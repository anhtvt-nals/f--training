module Category

open System.Net
open Microsoft.Azure.Cosmos
open Database
open Config
open Types


let allCategories (container: Container) =
    task {
        let query = "SELECT * FROM books WHERE books.itemType = 'category'"
        let! results = queryCosmosAsyncSeq<Category> container query
        return results
    }

let updateCategory (container: Container) (category: Category) =
    task {
        let! response =   container.ReplaceItemAsync(category, category.id, PartitionKey(category.itemType))
        match response.StatusCode with
        | HttpStatusCode.OK -> printfn "   ✓ Category '%s' updated successfully." category.name
        | (code: HttpStatusCode)              -> printfn "   ? StatusCode: %A" code
        return response.Resource
    }

let createOrUpdate (container: Container) (category: Category) =
    task {
        try
            let! response = container.UpsertItemAsync(category, PartitionKey(category.itemType))
            return response.Resource
        with
        | :? CosmosException as ex ->
            printfn "Error upserting category: %s" ex.Message
            return category
    }

let increaseBookCount (container: Container) (categoryId: string, partitionKey: string) (delta: int) =
    task {
        let! response = container.PatchItemAsync<Category>(
            categoryId,
            PartitionKey(partitionKey),
            [
                PatchOperation.Increment("/bookCount", int64 delta)
            ]
        )
        match response.StatusCode with
        | HttpStatusCode.OK -> printfn "   ✓ Category '%s' bookCount updated successfully." categoryId
        | (code: HttpStatusCode)              -> printfn "   ? StatusCode: %A" code
        return response.Resource
    }

let deleteCategory (container: Container) (id: string) =
    task {
        try
            let! response = container.DeleteItemAsync<Category>(id, new PartitionKey("category"))
            match response.StatusCode with
            | HttpStatusCode.NoContent -> printfn "   ✓ Category with id '%s' deleted successfully." id
            | (code: HttpStatusCode)                     -> printfn "   ? StatusCode: %A" code
            return Ok(true)
        with
        | :? CosmosException as ex when ex.StatusCode = HttpStatusCode.NotFound ->
            printfn "   ? Category with id '%s' not found." id
            return Ok(false)
        | ex -> 
            return Error ex.Message
    }


let getCategoryById (container: Container) (id: string) =
    task {
        try
            let! response = container.ReadItemAsync<Category>(id, new PartitionKey("category"))
            return Some response.Resource
        with
        | :? CosmosException as ex when ex.StatusCode = HttpStatusCode.NotFound ->
            printfn "   ? Category with id '%s' not found." id
            return None
        | ex ->
            return None
    }
let createCategory (container: Container) (category: Category) =
    task {
        try
            let! response = container.CreateItemAsync(category, new PartitionKey(category.itemType))
            match response.StatusCode with
            | HttpStatusCode.Created -> printfn "   ✓ Category '%s' được tạo thành công." category.name
            | (code: HttpStatusCode)                   -> printfn "   ? StatusCode: %A" code
            return response.Resource
        with
        | :? CosmosException as (ex: CosmosException) when ex.StatusCode = HttpStatusCode.Conflict ->
            printfn "   ? Category with id '%s' already exists." category.id
            return category
        | ex ->
            printfn "   ! Error creating category: %s" ex.Message
            return category
    }

let updateCategoryCount (bookContainer: Container) (categoryId: string) =
    task {
        let! categoryOpt = getCategoryById bookContainer (categoryId)
        match categoryOpt with
        | Some category ->
            try
                let! booksInCategory = queryCosmosAsyncSeq<Book> bookContainer (sprintf "SELECT * FROM books books WHERE books.category = '%s' and books.isDeleted = false and books.itemType = 'book'" categoryId)
                let newCount = List.length booksInCategory
                let! response = bookContainer.PatchItemAsync<Category>(
                    categoryId,
                    PartitionKey(category.itemType),
                    [
                        PatchOperation.Replace("/bookCount", int64 newCount)
                    ]
                )

                match response.StatusCode with
                | HttpStatusCode.OK -> printfn "   ✓ Category '%s' bookCount updated successfully to %d." categoryId newCount
                | code              -> printfn "   ? StatusCode: %A" code

                return Some response.Resource
            with
            | :? CosmosException as ex when ex.StatusCode = HttpStatusCode.NotFound ->
                printfn "   ? Category with id '%s' not found." categoryId
                return None
            | ex ->
                return None
        | None -> 
            printfn "   ? Category with id '%s' not found." categoryId
            return None
    }
