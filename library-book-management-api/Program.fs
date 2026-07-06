module Program

open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Hosting
open Giraffe
open Microsoft.Azure.Cosmos
open Azure.Search.Documents
open DotNetEnv

open Book
open BookRepository
open BookSearchRepository
open BookController
open CategoryRepository
open CategoryController
open Microsoft.AspNetCore.Http

let requireEnv name =
    match System.Environment.GetEnvironmentVariable(name) with
    | null | "" -> failwith ("Missing env: " + name)
    | v -> v

let defaultEnv name fallback =
    match System.Environment.GetEnvironmentVariable(name) with
    | null | "" -> fallback
    | v -> v

let bindVModel<'T> (handler: 'T -> HttpHandler) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! model = ctx.BindModelAsync<'T>()
            return! handler model next ctx
        }

let webApp =
    choose [
        GET >=> route "/health" >=> healthHandler

        // Books
        GET >=> route "/api/books" >=> getBooksHandler
        GET >=> routef "/api/books/%s" getBookHandler
        POST >=> route "/api/books" >=> createBookHandler
        PUT >=> routef "/api/books/%s" updateBookHandler
        DELETE >=> routef "/api/books/%s" deleteBookHandler

        // Categories
        GET >=> route "/api/categories" >=> getCategoriesHandler
        GET >=> routef "/api/categories/%s" getCategoryHandler
        POST >=> route "/api/categories" >=> createCategoryHandler
        PUT >=> routef "/api/categories/%s" updateCategoryHandler
        DELETE >=> routef "/api/categories/%s" deleteCategoryHandler

        // Search (Azure Search - hỗ trợ cả GET với query params và POST với JSON body)
        route "/api/search" >=> choose [
            GET >=> bindVModel<SearchRequest> searchBooksHandler     // GET /api/search?query=fiction&categoryId=cat_123
            POST >=> bindVModel<SearchRequest> searchBooksHandler    // POST /api/search (JSON body)
        ]
        
        // Search (Cosmos DB - đơn giản)
        GET >=> route "/api/search/cosmos" >=> searchCosmosHandler  // Support GET with query params
        GET >=> routef "/api/books/category/%s" getBooksByCategoryHandler

        setStatusCode 404 >=> json {| error = "Not Found" |}
    ]

[<EntryPoint>]
let main args =
    try Env.Load() |> ignore with _ -> ()

    printfn "Library Book Management API (F# + Giraffe)"
    printfn "=============================================="
    printfn "Books:       GET/POST /api/books, GET/PUT/DELETE /api/books/{id}"
    printfn "Categories:  GET/POST /api/categories, GET/PUT/DELETE /api/categories/{id}"
    printfn ""
    printfn "Search (Azure):  GET/POST /api/search"
    printfn "                 - GET with query params: ?query=text&categoryId=id&top=10"
    printfn "                 - POST with JSON body: {query, categoryId, top, ...}"
    printfn "Search (Cosmos): GET /api/search/cosmos"
    printfn "                 GET /api/books/category/{categoryId}"
    printfn ""
    printfn "Health:      GET /health"

    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(fun wh ->
            wh.Configure(fun (app: Microsoft.AspNetCore.Builder.IApplicationBuilder) ->
                app.UseGiraffe(webApp))
              .ConfigureServices(fun (services: Microsoft.Extensions.DependencyInjection.IServiceCollection) ->
                services.AddGiraffe() |> ignore

                let cosmosCfg =
                    { EndpointUrl = requireEnv "COSMOS_ENDPOINT_URL"
                      PrimaryKey = requireEnv "COSMOS_PRIMARY_KEY"
                      DatabaseId = defaultEnv "COSMOS_DATABASE_ID" "LibraryDB"
                      ContainerId = defaultEnv "COSMOS_CONTAINER_ID" "Books" }
                let bookRepo = new BookRepository(cosmosCfg)
                services.AddSingleton<BookRepository>(bookRepo) |> ignore

                // CategoryRepository reuses the SAME CosmosClient + container
                let catRepo = new CategoryRepository(bookRepo.Client, bookRepo.DatabaseId, bookRepo.ContainerId)
                services.AddSingleton<CategoryRepository>(catRepo) |> ignore

                let searchCfg =
                    { Endpoint = requireEnv "SEARCH_ENDPOINT"
                      ApiKey = requireEnv "SEARCH_API_KEY"
                      IndexName = defaultEnv "SEARCH_INDEX_NAME" "books-index" }
                services.AddSingleton<BookSearchRepository>(new BookSearchRepository(searchCfg)) |> ignore
              ) |> ignore)
        .Build().Run()
    0
