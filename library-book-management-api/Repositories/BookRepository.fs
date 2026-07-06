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
    member _.SearchAsync(?searchText: string, ?title: string, ?author: string, ?categoryId: string, ?minYear: int, ?maxYear: int) =
        task {
            let conditions = ResizeArray<string>()
            conditions.Add("c.itemType = @type")
            
            let parameters = ResizeArray<struct(string * obj)>()
            parameters.Add(struct("@type", ItemType.book :> obj))
            
            // Build WHERE conditions
            match searchText with
            | Some text when not (String.IsNullOrWhiteSpace(text)) ->
                conditions.Add("(CONTAINS(LOWER(c.title), LOWER(@searchText)) OR CONTAINS(LOWER(c.author), LOWER(@searchText)) OR CONTAINS(LOWER(c.categoryName), LOWER(@searchText)))")
                parameters.Add(struct("@searchText", text :> obj))
            | _ -> ()
            
            match title with
            | Some t when not (String.IsNullOrWhiteSpace(t)) ->
                conditions.Add("CONTAINS(LOWER(c.title), LOWER(@title))")
                parameters.Add(struct("@title", t :> obj))
            | _ -> ()
            
            match author with
            | Some a when not (String.IsNullOrWhiteSpace(a)) ->
                conditions.Add("CONTAINS(LOWER(c.author), LOWER(@author))")
                parameters.Add(struct("@author", a :> obj))
            | _ -> ()
            
            match categoryId with
            | Some cid ->
                conditions.Add("c.categoryId = @categoryId")
                parameters.Add(struct("@categoryId", cid :> obj))
            | _ -> ()
            
            match minYear with
            | Some y ->
                conditions.Add("c.publishedYear >= @minYear")
                parameters.Add(struct("@minYear", y :> obj))
            | _ -> ()
            
            match maxYear with
            | Some y ->
                conditions.Add("c.publishedYear <= @maxYear")
                parameters.Add(struct("@maxYear", y :> obj))
            | _ -> ()
            
            let whereClause = String.Join(" AND ", conditions)
            let sql = sprintf "SELECT * FROM c WHERE %s ORDER BY c.addedDate DESC" whereClause
            
            let mutable queryDef = QueryDefinition(sql)
            for struct(name, value) in parameters do
                queryDef <- queryDef.WithParameter(name, value)
            
            return! this.QueryCosmosAsync<Book> queryDef
        }

    member _.Client = client
    member _.DatabaseId = config.DatabaseId
    member _.ContainerId = config.ContainerId
