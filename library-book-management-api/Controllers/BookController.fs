module BookController

open System
open Giraffe
open Book
open BookRepository
open BookSearchRepository

[<CLIMutable>]
type CreateBookRequest =
    { title: string
      author: string
      categoryId: string
      categoryName: string
      publishedYear: int
      totalCopies: int }

[<CLIMutable>]
type UpdateBookRequest =
    { title: string option
      author: string option
      categoryId: string option
      categoryName: string option
      publishedYear: int option
      totalCopies: int option
      availableCopies: int option }

[<CLIMutable>]
type SearchRequest =
    { query: string
      top: int option }

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
            let bookId = IdGen.generateBookId()
            let book: Book =
                { id = bookId
                  bookId = bookId
                  title = req.title
                  author = req.author
                  categoryId = req.categoryId
                  categoryName = req.categoryName
                  publishedYear = req.publishedYear
                  totalCopies = req.totalCopies
                  availableCopies = req.totalCopies
                  itemType = ItemType.book
                  addedDate = DateTime.UtcNow }
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
            let repo = ctx.GetService<BookRepository>()
            try
                let! existing = repo.GetByIdAsync(bookId)
                let updated: Book =
                    { existing with
                        title = req.title |> Option.defaultValue existing.title
                        author = req.author |> Option.defaultValue existing.author
                        categoryId = req.categoryId |> Option.defaultValue existing.categoryId
                        categoryName = req.categoryName |> Option.defaultValue existing.categoryName
                        publishedYear = req.publishedYear |> Option.defaultValue existing.publishedYear
                        totalCopies = req.totalCopies |> Option.defaultValue existing.totalCopies
                        availableCopies = req.availableCopies |> Option.defaultValue existing.availableCopies }
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

// POST /api/search (Azure Search - phức tạp, nhanh, full-text search)
let searchBooksHandler: HttpHandler =
    fun next ctx ->
        task {
            let! req = ctx.BindJsonAsync<SearchRequest>()
            let searchRepo = ctx.GetService<BookSearchRepository>()
            let! results = searchRepo.SearchAsync(req.query, defaultArg req.top 20)
            return! json {| source = "Azure Search"; count = results.Length; results = results |} next ctx
        }

// POST /api/search/cosmos (Cosmos DB search - đơn giản, filter-based)
let searchCosmosHandler: HttpHandler =
    fun next ctx ->
        task {
            let! req = ctx.BindJsonAsync<SearchRequest>()
            let repo = ctx.GetService<BookRepository>()
            let! results = repo.SearchAsync(req.query)
            return! json {| source = "Cosmos DB"; count = results.Length; results = results |} next ctx
        }

// GET /api/books/search/title?q=clean
let searchByTitleHandler: HttpHandler =
    fun next ctx ->
        task {
            let query = ctx.GetQueryStringValue("q") |> Result.defaultValue ""
            let repo = ctx.GetService<BookRepository>()
            let! results = repo.SearchByTitleAsync(query)
            return! json results next ctx
        }

// GET /api/books/search/author?q=martin
let searchByAuthorHandler: HttpHandler =
    fun next ctx ->
        task {
            let query = ctx.GetQueryStringValue("q") |> Result.defaultValue ""
            let repo = ctx.GetService<BookRepository>()
            let! results = repo.SearchByAuthorAsync(query)
            return! json results next ctx
        }

// GET /api/books/category/{categoryId}
let getBooksByCategoryHandler (categoryId: string) : HttpHandler =
    fun next ctx ->
        task {
            let repo = ctx.GetService<BookRepository>()
            let! results = repo.SearchByCategoryAsync(categoryId)
            return! json results next ctx
        }
