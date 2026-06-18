module Book

open System
open System.Net
open System.Collections.Generic
open Microsoft.Azure.Cosmos
open Database
open Category
open Config
open Types


// ── Tạo Book ─────────────────────────────────────────────────────────────
// Lưu ý: book.Category phải trùng với partition key của container (books)
// Nếu không, Cosmos DB sẽ trả về lỗi 400 Bad Request
// Throughput: 1 RU (point read) —> scale up sau khi có load thực tế
let createBook (container: Container) (book: Book) =
    task {
        try
            let! response = container.CreateItemAsync(book, new PartitionKey(book.category))
            match response.StatusCode with
            | HttpStatusCode.Created -> printfn "   ✓ Book '%s' được tạo thành công." book.title
            | (code: HttpStatusCode)                   -> printfn "   ? StatusCode: %A" code
            return response.Resource
        with
        | :? CosmosException as (ex: CosmosException) when ex.StatusCode = HttpStatusCode.Conflict ->
            printfn "   ? Book with id '%s' already exists." book.id
            return book
        | ex ->
            printfn "   ! Error creating book: %s" ex.Message
            return book
    }


let allBooks (container: Container) =
    task {
        let query = "SELECT * FROM books"
        let! results = queryCosmosAsyncSeq<Book> container query
        return results
    }

let getBookById (container: Container) (id: string) (category: string) =
    task {
        try
            let! response = container.ReadItemAsync<Book>(id, new PartitionKey(category))
            return Some response.Resource
        with
        | :? CosmosException as ex when ex.StatusCode = HttpStatusCode.NotFound ->
            printfn "   ? Book with id '%s' not found." id
            return None
        | ex ->
            return None
    }

let updateBook (container: Container) (book: Book) =
    task {
        let! response = container.ReplaceItemAsync(book, book.id, new PartitionKey(book.category))
        match response.StatusCode with
        | HttpStatusCode.OK -> printfn "   ✓ Book '%s' updated successfully." book.title
        | code              -> printfn "   ? StatusCode: %A" code
        return response.Resource
    }

let deleteBook (container: Container) (id: string) (category: string) =
    task {
        try
            let! response = container.DeleteItemAsync<Book>(id, new PartitionKey(category))
            match response.StatusCode with
            | HttpStatusCode.NoContent -> printfn "   ✓ Book with id '%s' deleted successfully." id
            | code                     -> printfn "   ? StatusCode: %A" code
            return Ok(true)
        with
        | :? CosmosException as ex when ex.StatusCode = HttpStatusCode.NotFound ->
            printfn "   ? Book with id '%s' not found for deletion." id
            return Error(sprintf "Book with id '%s' not found." id)
        | ex ->
            return Error ex.Message
    }

let handleBookChanges
    (bookContainer: Container)
    (categoryContainer: Container)
    (changes: IReadOnlyCollection<Book>) =
    task {

        for book in changes do
            if book.isDeleted then
                printfn "Book changed: %s (%s)" book.title book.category

                let! result = updateCategoryCount categoryContainer bookContainer book.category
                match result with
                | Some updatedCategory ->
                    printfn "   ✓ Updated category '%s' bookCount to %d" updatedCategory.name updatedCategory.bookCount
                | None ->
                    printfn "   ? Failed to update category '%s' bookCount" book.category

        return ()
    }