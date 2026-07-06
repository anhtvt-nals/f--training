module BookController

open System
open Giraffe
open Book
open BookRepository
open BookSearchRepository
open Book.Validation

// GET /health
let healthHandler: HttpHandler =
    fun next ctx -> json {| status = "healthy" |} next ctx

// GET /api/books
let getBooksHandler: HttpHandler =
    fun next ctx ->
        task {
            let repo = ctx.GetService<BookRepository>()
            let! books = repo.GetAllAsync()
            return! json books next ctx
        }

// GET /api/books/{id}
let getBookHandler (bookId: string) : HttpHandler =
    fun next ctx ->
        task {
            let repo = ctx.GetService<BookRepository>()
            try
                let! book = repo.GetByIdAsync(bookId)
                return! json book next ctx
            with _ ->
                return! (setStatusCode 404 >=> json {| error = "Book not found" |}) next ctx
        }

// POST /api/books
let createBookHandler: HttpHandler =
    fun next ctx ->
        task {
            let! req = ctx.BindJsonAsync<CreateBookRequest>()
            
            // Validate request
            match validateCreateBookRequest req with
            | Invalid errors ->
                return! (setStatusCode 400 >=> json {| error = "Validation failed"; details = errors |}) next ctx
            | Valid validReq ->
                let bookId = IdGen.generateBookId()
                let book: Book =
                    { id = bookId
                      bookId = bookId
                      title = validReq.title
                      author = validReq.author
                      categoryId = validReq.categoryId
                      categoryName = validReq.categoryName
                      publishedYear = validReq.publishedYear
                      totalCopies = validReq.totalCopies
                      availableCopies = validReq.totalCopies
                      itemType = ItemType.book
                      addedDate = DateTimeOffset.UtcNow }
                let repo = ctx.GetService<BookRepository>()
                let! created = repo.CreateAsync(book)
                let searchRepo = ctx.GetService<BookSearchRepository>()
                let _ = searchRepo.IndexAsync(created).Result
                return! (setStatusCode 201 >=> json created) next ctx
        }

// PUT /api/books/{id}
let updateBookHandler (bookId: string) : HttpHandler =
    fun next ctx ->
        task {
            let! req = ctx.BindJsonAsync<UpdateBookRequest>()
            
            // Validate request
            match validateUpdateBookRequest req with
            | Invalid errors ->
                return! (setStatusCode 400 >=> json {| error = "Validation failed"; details = errors |}) next ctx
            | Valid validReq ->
                let repo = ctx.GetService<BookRepository>()
                try
                    let! existing = repo.GetByIdAsync(bookId)
                    let updated: Book =
                        { existing with
                            title = validReq.title |> Option.defaultValue existing.title
                            author = validReq.author |> Option.defaultValue existing.author
                            categoryId = validReq.categoryId |> Option.defaultValue existing.categoryId
                            categoryName = validReq.categoryName |> Option.defaultValue existing.categoryName
                            publishedYear = validReq.publishedYear |> Option.defaultValue existing.publishedYear
                            totalCopies = validReq.totalCopies |> Option.defaultValue existing.totalCopies
                            availableCopies = validReq.availableCopies |> Option.defaultValue existing.availableCopies }
                    let! saved = repo.UpdateAsync(updated)
                    let searchRepo = ctx.GetService<BookSearchRepository>()
                    let _ = searchRepo.IndexAsync(saved).Result
                    return! json saved next ctx
                with _ ->
                    return! (setStatusCode 404 >=> json {| error = "Book not found" |}) next ctx
        }

// DELETE /api/books/{id}
let deleteBookHandler (bookId: string) : HttpHandler =
    fun next ctx ->
        task {
            let repo = ctx.GetService<BookRepository>()
            let searchRepo = ctx.GetService<BookSearchRepository>()
            try
                let _ = repo.DeleteAsync(bookId).Result
                let _ = searchRepo.DeleteFromIndexAsync(bookId).Result
                return! (setStatusCode 204) next ctx
            with _ ->
                return! (setStatusCode 404 >=> json {| error = "Book not found" |}) next ctx
        }

// ============ SEARCH ENDPOINTS ============

// Implementation: Azure Search với validation
let searchBooksHandler (req: SearchRequest) : HttpHandler =
    fun next ctx ->
        task {
            // Validate request
            match validateSearchRequest req with
            | Invalid errors ->
                return! (setStatusCode 400 >=> json {| error = "Validation failed"; details = errors |}) next ctx
            | Valid validReq ->
                let searchRepo = ctx.GetService<BookSearchRepository>()
                let! (results, totalCount) = searchRepo.SearchWithFiltersAsync(validReq)
                let response: SearchResponse = 
                    { source = "Azure Search"
                      count = results.Length
                      totalCount = totalCount
                      results = results }
                return! json response next ctx
        }


// POST/GET /api/search/cosmos (Cosmos DB search - đơn giản, filter-based)
let searchCosmosHandler: HttpHandler =
    fun next ctx ->
        task {
            let repo = ctx.GetService<BookRepository>()
            
            // Support both POST (JSON body) and GET (query params)
            let! searchText, categoryId, author, minYear, maxYear =
                task {
                    if ctx.Request.Method = "POST" then
                        let! req = ctx.BindJsonAsync<SearchRequest>()
                        return 
                            (if String.IsNullOrWhiteSpace(req.query) then None else Some req.query),
                            req.categoryId,
                            req.author,
                            req.minYear,
                            req.maxYear
                    else
                        // GET: read from query string
                        let q = ctx.TryGetQueryStringValue("searchText") |> Option.orElse (ctx.TryGetQueryStringValue("q"))
                        let cat = ctx.TryGetQueryStringValue("categoryId")
                        let auth = ctx.TryGetQueryStringValue("author")
                        let minY = ctx.TryGetQueryStringValue("minYear") |> Option.bind (fun s -> match Int32.TryParse(s) with true, v -> Some v | _ -> None)
                        let maxY = ctx.TryGetQueryStringValue("maxYear") |> Option.bind (fun s -> match Int32.TryParse(s) with true, v -> Some v | _ -> None)
                        return q, cat, auth, minY, maxY
                }
            
            let! results = repo.SearchAsync(
                ?searchText = searchText,
                ?categoryId = categoryId,
                ?author = author,
                ?minYear = minYear,
                ?maxYear = maxYear
            )
            return! json {| source = "Cosmos DB"; count = results.Length; results = results |} next ctx
        }

// GET /api/books/category/{categoryId}
let getBooksByCategoryHandler (categoryId: string) : HttpHandler =
    fun next ctx ->
        task {
            let repo = ctx.GetService<BookRepository>()
            let! results = repo.SearchAsync(categoryId = categoryId)
            return! json results next ctx
        }
