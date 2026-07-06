#!/usr/bin/env dotnet fsi
#r "nuget: Microsoft.Azure.Cosmos, 3.39.0"
#r "nuget: Azure.Search.Documents, 11.6.0"
#r "nuget: DotNetEnv, 3.0.0"

#load "../Models/Book.fs"

open System
open Microsoft.Azure.Cosmos
open Azure
open Azure.Search.Documents
open Azure.Search.Documents.Indexes
open Azure.Search.Documents.Indexes.Models
open Azure.Search.Documents.Models
open DotNetEnv
open Book

// ============ Helper Functions ============

let requireEnv name =
    match Environment.GetEnvironmentVariable(name) with
    | null | "" -> failwith ("Missing env: " + name)
    | v -> v

let defaultEnv name fallback =
    match Environment.GetEnvironmentVariable(name) with
    | null | "" -> fallback
    | v -> v

// ============ Batch Processing ============

let chunkList (size: int) (list: 'T list) : 'T list list =
    list
    |> List.indexed
    |> List.groupBy (fun (i, _) -> i / size)
    |> List.map (fun (_, group) -> group |> List.map snd)

let uploadBatch (searchClient: SearchClient) (batchNumber: int) (totalBatches: int) (batch: Book list) =
    try
        printfn "  Batch %d/%d: Uploading %d books..." batchNumber totalBatches batch.Length
        let uploadBatch = IndexDocumentsBatch.MergeOrUpload(batch |> List.toArray)
        let result = searchClient.IndexDocuments(uploadBatch)
        
        let failures = 
            result.Value.Results 
            |> Seq.filter (fun r -> not r.Succeeded)
            |> Seq.toList
        
        if failures.IsEmpty then
            printfn "  ✓ Batch %d: Success (%d documents)" batchNumber batch.Length
            Ok batch.Length
        else
            printfn "  ✗ Batch %d: Some documents failed" batchNumber
            failures |> List.iter (fun r -> 
                printfn "     - Document '%s': %s" r.Key r.ErrorMessage)
            Error batch.Length
    with ex ->
        printfn "  ✗ Batch %d: Failed - %s" batchNumber ex.Message
        Error batch.Length

let uploadBatchAsync (searchClient: SearchClient) (batchNumber: int) (totalBatches: int) (batch: Book list) =
    async {
        return uploadBatch searchClient batchNumber totalBatches batch
    }

let uploadAllBatchesParallel (searchClient: SearchClient) (batches: Book list list) (maxParallel: int) =
    printfn "  Using parallel processing (max %d batches at once)" maxParallel
    
    let asyncOperations = 
        batches 
        |> List.mapi (fun i batch -> 
            uploadBatchAsync searchClient (i + 1) batches.Length batch)
    
    let results = 
        asyncOperations
        |> Async.Parallel
        |> Async.RunSynchronously
    
    let successCount = 
        results 
        |> Array.choose (fun r -> match r with Ok count -> Some count | Error _ -> None)
        |> Array.sum
    
    let failureCount = 
        results 
        |> Array.choose (fun r -> match r with Error count -> Some count | Ok _ -> None)
        |> Array.sum
    
    (successCount, failureCount)


// ============ Index Management ============

let createIndexDefinition (indexName: string) =
    let index = SearchIndex(indexName)
    let fieldBuilder = FieldBuilder()
    index.Fields <- fieldBuilder.Build(typeof<Book>)
    index

let ensureIndex (indexClient: SearchIndexClient) (indexName: string) =
    try
        let index = createIndexDefinition indexName
        indexClient.CreateOrUpdateIndex(index) |> ignore
        printfn "  ✓ Index created"
        true
    with ex ->
        printfn "Force recreating index '%s'..." indexName
        try
            indexClient.DeleteIndex(indexName) |> ignore
            printfn "  ✓ Old index deleted"
        with _ ->
            printfn "  (Index did not exist)"
        let index = createIndexDefinition indexName
        indexClient.CreateOrUpdateIndex(index) |> ignore
        printfn "  ✓ Index created"
        true


// ============ MAIN ============

printfn "=========================================="
printfn "Azure Search Import Tool"
printfn "=========================================="
printfn ""

try Env.Load() |> ignore with _ -> ()

printfn "1. Reading configuration..."

let cosmosEndpoint = requireEnv "COSMOS_ENDPOINT_URL"
let cosmosKey = requireEnv "COSMOS_PRIMARY_KEY"
let databaseId = defaultEnv "COSMOS_DATABASE_ID" "LibraryDB"
let containerId = defaultEnv "COSMOS_CONTAINER_ID" "Books"

let searchEndpoint = requireEnv "SEARCH_ENDPOINT"
let searchKey = requireEnv "SEARCH_API_KEY"
let indexName = defaultEnv "SEARCH_INDEX_NAME" "books-index"

let batchSize = defaultEnv "BATCH_SIZE" "100" |> int
let maxParallel = defaultEnv "MAX_PARALLEL" "4" |> int

printfn "  Cosmos DB: %s/%s/%s" cosmosEndpoint databaseId containerId
printfn "  Search: %s/%s" searchEndpoint indexName
printfn "  Batch size: %d" batchSize
printfn "  Max parallel: %d" maxParallel
printfn ""

printfn "2. Connecting to services..."

let cosmosClient = new CosmosClient(cosmosEndpoint, cosmosKey)
let container = cosmosClient.GetDatabase(databaseId).GetContainer(containerId)

let credential = AzureKeyCredential(searchKey)
let searchClient = SearchClient(Uri(searchEndpoint), indexName, credential)
let indexClient = SearchIndexClient(Uri(searchEndpoint), credential)

printfn "  ✓ Connected"
printfn ""

printfn "3. Ensuring search index exists..."

if not (ensureIndex indexClient indexName) then
    printfn ""
    printfn "❌ Failed to create index. Exiting."
    exit 1

printfn ""

printfn "4. Fetching books from Cosmos DB..."

let query = QueryDefinition("SELECT * FROM c WHERE c.itemType = @type").WithParameter("@type", "book")
let iterator = container.GetItemQueryIterator<Book>(query)
let books = ResizeArray<Book>()
while iterator.HasMoreResults do
    let page = iterator.ReadNextAsync().GetAwaiter().GetResult()
    books.AddRange(page.Resource)

printfn "  Found %d books" books.Count

if books.Count = 0 then
    printfn "  No books to sync. Exiting."
    exit 0

printfn ""

printfn "5. Splitting into batches..."

let batches = chunkList batchSize (books |> List.ofSeq)
printfn "  Created %d batches" batches.Length
printfn ""

printfn "6. Uploading to Azure Search..."

let startTime = DateTime.UtcNow
let (successCount, failureCount) = uploadAllBatchesParallel searchClient batches maxParallel
let endTime = DateTime.UtcNow
let duration = endTime - startTime

printfn ""
printfn "=========================================="
printfn "COMPLETE"
printfn "=========================================="
printfn "  Total: %d books" books.Count
printfn "  Success: %d" successCount
printfn "  Failed: %d" failureCount
printfn "  Duration: %.2f seconds" duration.TotalSeconds
printfn ""

if failureCount > 0 then
    printfn "⚠️  Some documents failed to upload"
    exit 1
else
    printfn "✓ All documents uploaded successfully!"
    exit 0
