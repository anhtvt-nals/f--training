module Database

open System
open System.Net
open System.Net.Http
open Microsoft.Azure.Cosmos
open Config
open Types
open FSharp.Control

// ── Tạo CosmosClient ──────────────────────────────────────────────────────
// isEmulator = true  → bỏ qua SSL (self-signed cert của emulator)
// isEmulator = false → dùng cho Azure production
let createClient (cfg: CosmosConfig) (isEmulator: bool) =
    let opts = CosmosClientOptions()
    new CosmosClient(cfg.EndpointUrl, cfg.PrimaryKey, opts)

// ── Tạo Database ──────────────────────────────────────────────────────────

let createDatabase (client: CosmosClient) (cfg: CosmosConfig) =
    task {
        printfn "Tạo database '%s'..." cfg.DatabaseId
        let! response = client.CreateDatabaseIfNotExistsAsync(cfg.DatabaseId)
        let db = response.Database
        match response.StatusCode with
        | HttpStatusCode.OK      -> printfn "   ✓ Database đã tồn tại, dùng lại."
        | HttpStatusCode.Created -> printfn "   ✓ Database mới được tạo."
        | code                   -> printfn "   ? StatusCode: %A" code

        return db
    }

// ── Tạo Container: books ──────────────────────────────────────────────────
// Partition Key : /itemType
// Throughput    : 400 RU/s (minimum — scale up sau khi có load thực)
let createBooksContainer (db: Database) (cfg: CosmosConfig) =
    task {
        printfn "Tạo container '%s' (Partition Key: /itemType)..." cfg.BooksId
        let props = ContainerProperties(cfg.BooksId, "/itemType") // ContainerProperties constructor: (id, partitionKeyPath)
        let! response = db.CreateContainerIfNotExistsAsync(props, throughput = 400)
        match response.StatusCode with
        | HttpStatusCode.OK      -> printfn "   ✓ Container 'books' đã tồn tại, dùng lại."
        | HttpStatusCode.Created -> printfn "   ✓ Container 'books' mới được tạo thành công."
        | code                   -> printfn "   ? StatusCode: %A" code
        return response.Container
    }

let createLeaseContainer (db: Database) (cfg: CosmosConfig) =
    task {
        printfn "Tạo container '%s' (Partition Key: /id)..." cfg.LeasesId
        let props = ContainerProperties(cfg.LeasesId, "/id")
        let! response = db.CreateContainerIfNotExistsAsync(props, throughput = 400)
        match response.StatusCode with
        | HttpStatusCode.OK      -> printfn "   ✓ Container 'leases' đã tồn tại, dùng lại."
        | HttpStatusCode.Created -> printfn "   ✓ Container 'leases' mới được tạo thành công."
        | code                   -> printfn "   ? StatusCode: %A" code
        return response.Container
    }

// ── Init tất cả (gọi 1 lần khi khởi động app) ───────────────────────────

type LibraryContainers = {
    Books      : Container
    Leases     : Container
}

let initLibraryDatabase (client: CosmosClient) (cfg: CosmosConfig) (isEmulator: bool) =
    task {
        printfn "Khởi tạo LibraryDB trên Cosmos DB..."
        printfn "Endpoint : %s" cfg.EndpointUrl
        printfn "Database : %s\n" cfg.DatabaseId

        let! db         = createDatabase           client cfg
        let! books      = createBooksContainer      db     cfg
        let! leases = createLeaseContainer db cfg

        printfn "LibraryDatabase sẵn sàng."

        return { Books = books; Leases = leases }
    }

let queryCosmosAsyncSeq<'T> (container: Container) (query: string) =
    task {
        let queryDef = QueryDefinition(query)
        let iterator = container.GetItemQueryIterator<'T>(queryDef)
        let results: ResizeArray<'T> = ResizeArray<'T>()
        while iterator.HasMoreResults do
            let! response = iterator.ReadNextAsync() |> Async.AwaitTask
            results.AddRange(response.Resource)

        return List.ofSeq results
    }