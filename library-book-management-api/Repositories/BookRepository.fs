module BookRepository

open System
open Microsoft.Azure.Cosmos
open Book

type CosmosConfig =
    { EndpointUrl: string
      PrimaryKey: string
      DatabaseId: string
      ContainerId: string }

type BookRepository(config: CosmosConfig) as this =

    let client = new CosmosClient(config.EndpointUrl, config.PrimaryKey)
    let database = client.GetDatabase(config.DatabaseId)
    let container = database.GetContainer(config.ContainerId)

    let pk = PartitionKey(ItemType.book)

    // Helper: Query Cosmos DB và trả về List<'T>
    member private _.QueryCosmosAsync<'T>(queryDef: QueryDefinition) =
        task {
            let iterator = container.GetItemQueryIterator<'T>(queryDef)
            let results = ResizeArray<'T>()
            while iterator.HasMoreResults do
                let! page = iterator.ReadNextAsync()
                results.AddRange(page.Resource)
            return results |> List.ofSeq
        }

    // CREATE
    member _.CreateAsync(book: Book) =
        task {
            let b = { book with itemType = ItemType.book }
            let! _ = container.CreateItemAsync(b, pk)
            return b
        }

    // READ by id
    member _.GetByIdAsync(id: string) =
        task {
            let! resp = container.ReadItemAsync<Book>(id, pk)
            return resp.Resource
        }

    // LIST all books (filter by itemType)
    member _.GetAllAsync() =
        task {
            let query = QueryDefinition("SELECT * FROM c WHERE c.itemType = @type ORDER BY c.addedDate DESC")
                                    .WithParameter("@type", ItemType.book)
            return! this.QueryCosmosAsync<Book> query
        }

    // UPDATE
    member _.UpdateAsync(book: Book) =
        task {
            let! _ = container.UpsertItemAsync(book, pk)
            return book
        }

    // DELETE
    member _.DeleteAsync(id: string) =
        task {
            let! _ = container.DeleteItemAsync<Book>(id, pk)
            return true
        }

    // ============ SEARCH với Cosmos DB ============
    
    // Search by title (LIKE query)
    member _.SearchByTitleAsync(searchText: string) =
        task {
            let query = 
                QueryDefinition("SELECT * FROM c WHERE c.itemType = @type AND CONTAINS(LOWER(c.title), LOWER(@searchText))")
                    .WithParameter("@type", ItemType.book)
                    .WithParameter("@searchText", searchText)
            return! this.QueryCosmosAsync<Book> query
        }

    // Search by author
    member _.SearchByAuthorAsync(searchText: string) =
        task {
            let query = 
                QueryDefinition("SELECT * FROM c WHERE c.itemType = @type AND CONTAINS(LOWER(c.author), LOWER(@searchText))")
                    .WithParameter("@type", ItemType.book)
                    .WithParameter("@searchText", searchText)
            return! this.QueryCosmosAsync<Book> query
        }

    // Search by category
    member _.SearchByCategoryAsync(categoryId: string) =
        task {
            let query = 
                QueryDefinition("SELECT * FROM c WHERE c.itemType = @type AND c.categoryId = @categoryId")
                    .WithParameter("@type", ItemType.book)
                    .WithParameter("@categoryId", categoryId)
            return! this.QueryCosmosAsync<Book> query
        }

    // General search (title OR author OR category)
    member _.SearchAsync(searchText: string) =
        task {
            let query = 
                QueryDefinition("""SELECT * FROM c 
                   WHERE c.itemType = @type 
                   AND (CONTAINS(LOWER(c.title), LOWER(@searchText)) 
                        OR CONTAINS(LOWER(c.author), LOWER(@searchText))
                        OR CONTAINS(LOWER(c.categoryName), LOWER(@searchText)))
                   ORDER BY c.addedDate DESC""")
                    .WithParameter("@type", ItemType.book)
                    .WithParameter("@searchText", searchText)
            return! this.QueryCosmosAsync<Book> query
        }

    member _.Client = client
    member _.DatabaseId = config.DatabaseId
    member _.ContainerId = config.ContainerId
