#!/usr/bin/env dotnet fsi

#r "nuget: Microsoft.Azure.Cosmos, 3.39.0"
#r "nuget: Azure.Search.Documents, 11.6.0"
#r "nuget: DotNetEnv, 3.0.0"

open System
open Microsoft.Azure.Cosmos
open Azure
open Azure.Search.Documents
open Azure.Search.Documents.Indexes
open Azure.Search.Documents.Indexes.Models
open Azure.Search.Documents.Models
open DotNetEnv

// ============ Types ============

type Book =
    { id: string
      bookId: string
      title: string
      author: string
      categoryId: string
      categoryName: string
      publishedYear: int
      totalCopies: int
      availableCopies: int
      itemType: string
      addedDate: DateTime }

// ============ Helpers ============

let requireEnv name =
    match Environment.GetEnvironmentVariable(name) with
    | null | "" -> failwith ("Missing env: " + name)
    | v -> v

let defaultEnv name fallback =
    match Environment.GetEnvironmentVariable(name) with
    | null | "" -> fallback
    | v -> v

// ============ Main ============

try Env.Load() |> ignore with _ -> ()

// Cosmos config
let cosmosEndpoint = requireEnv "COSMOS_ENDPOINT_URL"
let cosmosKey = requireEnv "COSMOS_PRIMARY_KEY"
let databaseId = defaultEnv "COSMOS_DATABASE_ID" "LibraryDB"
let containerId = defaultEnv "COSMOS_CONTAINER_ID" "Books"

// Search config
let searchEndpoint = requireEnv "SEARCH_ENDPOINT"
let searchKey = requireEnv "SEARCH_API_KEY"
let indexName = defaultEnv "SEARCH_INDEX_NAME" "books-index"

// Connect to Cosmos
let cosmosClient = new CosmosClient(cosmosEndpoint, cosmosKey)
let container = cosmosClient.GetDatabase(databaseId).GetContainer(containerId)

// Connect to Azure Search
let credential = AzureKeyCredential(searchKey)
let searchClient = SearchClient(Uri(searchEndpoint), indexName, credential)
let indexClient = SearchIndexClient(Uri(searchEndpoint), credential)

// ------- Ensure index exists -------
printfn "Ensuring index '%s' exists ..." indexName
let index = SearchIndex(indexName)

let idField = SearchField("id", SearchFieldDataType.String)
idField.IsKey <- true
idField.IsFilterable <- true

let bookIdField = SearchField("bookId", SearchFieldDataType.String)
bookIdField.IsFilterable <- true

let titleField = SearchField("title", SearchFieldDataType.String)
titleField.IsSearchable <- true
titleField.IsFilterable <- true

let authorField = SearchField("author", SearchFieldDataType.String)
authorField.IsSearchable <- true
authorField.IsFilterable <- true

let categoryIdField = SearchField("categoryId", SearchFieldDataType.String)
categoryIdField.IsFilterable <- true
categoryIdField.IsFacetable <- true

let categoryNameField = SearchField("categoryName", SearchFieldDataType.String)
categoryNameField.IsSearchable <- true
categoryNameField.IsFilterable <- true
categoryNameField.IsFacetable <- true

let publishedYearField = SearchField("publishedYear", SearchFieldDataType.Int32)
publishedYearField.IsFilterable <- true
publishedYearField.IsSortable <- true
publishedYearField.IsFacetable <- true

let totalCopiesField = SearchField("totalCopies", SearchFieldDataType.Int32)
totalCopiesField.IsFilterable <- true
totalCopiesField.IsSortable <- true

let availableCopiesField = SearchField("availableCopies", SearchFieldDataType.Int32)
availableCopiesField.IsFilterable <- true
availableCopiesField.IsSortable <- true

let itemTypeField = SearchField("itemType", SearchFieldDataType.String)
itemTypeField.IsFilterable <- true
itemTypeField.IsFacetable <- true

let addedDateField = SearchField("addedDate", SearchFieldDataType.DateTimeOffset)
addedDateField.IsFilterable <- true
addedDateField.IsSortable <- true

index.Fields.Add(idField)
index.Fields.Add(bookIdField)
index.Fields.Add(titleField)
index.Fields.Add(authorField)
index.Fields.Add(categoryIdField)
index.Fields.Add(categoryNameField)
index.Fields.Add(publishedYearField)
index.Fields.Add(totalCopiesField)
index.Fields.Add(availableCopiesField)
index.Fields.Add(itemTypeField)
index.Fields.Add(addedDateField)

indexClient.CreateOrUpdateIndexAsync(index).GetAwaiter().GetResult() |> ignore
printfn "  Index ready."

// ------- Fetch all books from Cosmos -------
printfn "\nFetching books from CosmosDB ..."
let query = QueryDefinition("SELECT * FROM c WHERE c.itemType = @type").WithParameter("@type", "book")
let iterator = container.GetItemQueryIterator<Book>(query)
let books = ResizeArray<Book>()
while iterator.HasMoreResults do
    let page = iterator.ReadNextAsync().GetAwaiter().GetResult()
    books.AddRange(page.Resource)
printfn "  Found %d books." books.Count

// ------- Index each book in Azure Search -------
printfn "\nIndexing books to Azure Search ..."

// Upload in batches of 100 (Azure Search limit is 1000 per batch)
let batchSize = 100
let mutable uploaded = 0

for i in 0 .. batchSize .. books.Count - 1 do
    let batchEnd = min (i + batchSize) books.Count
    let batch = books.GetRange(i, batchEnd - i) |> Seq.toArray
    
    let uploadBatch = IndexDocumentsBatch.MergeOrUpload(batch)
    let result = searchClient.IndexDocuments(uploadBatch)
    
    uploaded <- uploaded + batch.Length
    printfn "  Indexed %d/%d books ..." uploaded books.Count
    
    // Check for failures
    let failures = 
        result.Value.Results 
        |> Seq.filter (fun r -> not r.Succeeded)
        |> Seq.toList
    
    if not failures.IsEmpty then
        printfn "  ⚠️  Some documents failed:"
        failures |> List.iter (fun r ->
            printfn "     ❌ Document '%s': Status %d - %s" r.Key r.Status r.ErrorMessage)

printfn "\nDone. Synced %d books to Azure Search index '%s'." uploaded indexName
