module CategoryRepository

open System
open Microsoft.Azure.Cosmos
open Book

type CategoryRepository(cosmosClient: CosmosClient, databaseId: string, containerId: string) as this =
    let container = cosmosClient.GetDatabase(databaseId).GetContainer(containerId)
    let pk = PartitionKey(ItemType.category)

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

    member _.CreateAsync(cat: Category) =
        task {
            let c = { cat with itemType = ItemType.category }
            let! _ = container.CreateItemAsync(c, pk)
            return c
        }

    member _.GetByIdAsync(id: string) =
        task {
            let! resp = container.ReadItemAsync<Category>(id, pk)
            return resp.Resource
        }

    member _.GetAllAsync() =
        task {
            let query = QueryDefinition("SELECT * FROM c WHERE c.itemType = @type").WithParameter("@type", ItemType.category)
            return! this.QueryCosmosAsync<Category> query
        }

    member _.UpdateAsync(cat: Category) =
        task {
            let! _ = container.UpsertItemAsync(cat, pk)
            return cat
        }

    member _.DeleteAsync(id: string) =
        task {
            let! _ = container.DeleteItemAsync<Category>(id, pk)
            return true
        }
