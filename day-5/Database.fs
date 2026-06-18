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
    opts.ConnectionMode <- ConnectionMode.Gateway
    if isEmulator then
        opts.HttpClientFactory <- fun () ->
            let handler = new SocketsHttpHandler()
            handler.SslOptions.RemoteCertificateValidationCallback <-
                fun _ _ _ _ -> true
            new HttpClient(handler)
    new CosmosClient(cfg.EndpointUrl, cfg.PrimaryKey, opts) // CosmosClient constructor: (endpointUri, primaryKey, options)

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
// Partition Key : /category
// Throughput    : 400 RU/s (minimum — scale up sau khi có load thực)
let createBooksContainer (db: Database) (cfg: CosmosConfig) =
    task {
        printfn "Tạo container '%s' (Partition Key: /category)..." cfg.BooksId
        let props = ContainerProperties(cfg.BooksId, "/category") // ContainerProperties constructor: (id, partitionKeyPath)
        let! response = db.CreateContainerIfNotExistsAsync(props, throughput = 400)
        match response.StatusCode with
        | HttpStatusCode.OK      -> printfn "   ✓ Container 'books' đã tồn tại, dùng lại."
        | HttpStatusCode.Created -> printfn "   ✓ Container 'books' mới được tạo thành công."
        | code                   -> printfn "   ? StatusCode: %A" code
        return response.Container
    }

// ── Tạo Container: categories ─────────────────────────────────────────────
// Partition Key : /id  (slug = id, point read = 1 RU)
// Throughput    : 400 RU/s (shared minimum)
let createCategoriesContainer (db: Database) (cfg: CosmosConfig) =
    task {
        printfn "Tạo container '%s' (Partition Key: /id)..." cfg.CategoriesId
        let props = ContainerProperties(cfg.CategoriesId, "/id")
        let! response = db.CreateContainerIfNotExistsAsync(props, throughput = 400)
        match response.StatusCode with
        | HttpStatusCode.OK      -> printfn "   ✓ Container 'categories' đã tồn tại, dùng lại."
        | HttpStatusCode.Created -> printfn "   ✓ Container 'categories' mới được tạo thành công."
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
    Categories : Container
    Leases     : Container
}

let initLibraryDatabase (client: CosmosClient) (cfg: CosmosConfig) (isEmulator: bool) =
    task {
        printfn "Khởi tạo LibraryDB trên Cosmos DB..."
        printfn "Endpoint : %s" cfg.EndpointUrl
        printfn "Database : %s\n" cfg.DatabaseId

        let! db         = createDatabase           client cfg
        let! books      = createBooksContainer      db     cfg
        let! categories = createCategoriesContainer db     cfg
        let! leases = createLeaseContainer db cfg

        printfn "LibraryDatabase sẵn sàng."

        return { Books = books; Categories = categories; Leases = leases }
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