module BookSearchRepository

open System
open Azure
open Azure.Search.Documents
open Azure.Search.Documents.Indexes
open Azure.Search.Documents.Indexes.Models
open Azure.Search.Documents.Models
open Book

type SearchConfig =
    { Endpoint: string
      ApiKey: string
      IndexName: string }

type BookSearchRepository(config: SearchConfig) =

    let credential = AzureKeyCredential(config.ApiKey)
    let searchClient = SearchClient(Uri(config.Endpoint), config.IndexName, credential)
    let indexClient = SearchIndexClient(Uri(config.Endpoint), credential)

    member _.EnsureIndexAsync() =
        task {
            let index = SearchIndex(config.IndexName)
            let fieldBuilder = FieldBuilder()
            index.Fields <- fieldBuilder.Build(typeof<Book>)
            let! _ = indexClient.CreateOrUpdateIndexAsync(index)
            return "Index ready"
        } : System.Threading.Tasks.Task<string>

    member _.IndexAsync(book: Book) =
        task {
            let doc = SearchDocument()
            doc.Add("id", book.id :> obj)
            doc.Add("title", book.title :> obj)
            doc.Add("author", book.author :> obj)
            doc.Add("categoryId", book.categoryId :> obj)
            doc.Add("categoryName", book.categoryName :> obj)
            let batch = IndexDocumentsBatch.Upload([| doc |])
            let! _ = searchClient.IndexDocumentsAsync(batch)
            return "Indexed"
        } : System.Threading.Tasks.Task<string>

    member _.DeleteFromIndexAsync(id: string) =
        task {
            let batch = IndexDocumentsBatch.Delete("id", [| id |])
            let! _ = searchClient.IndexDocumentsAsync(batch)
            return "Removed"
        } : System.Threading.Tasks.Task<string>

    member _.SearchAsync(searchText: string, ?top: int) =
        task {
            let options = SearchOptions()
            options.Size <- defaultArg top 20
            options.IncludeTotalCount <- true
            let! resp = searchClient.SearchAsync<Book>(searchText, options)
            
            // Parse results to Book list
            let books = 
                resp.Value.GetResults()
                |> Seq.map (fun r -> r.Document)
                |> Seq.toList
            
            return books
        } : System.Threading.Tasks.Task<Book list>

    // Advanced search with filters
    member _.SearchWithFiltersAsync(searchText: string, ?categoryId: string, ?minYear: int, ?maxYear: int) =
        task {
            let options = SearchOptions()
            options.Size <- 50
            
            // Build filter expression
            let filters = ResizeArray<string>()
            match categoryId with
            | Some cid -> filters.Add($"categoryId eq '{cid}'")
            | None -> ()
            match minYear with
            | Some year -> filters.Add($"publishedYear ge {year}")
            | None -> ()
            match maxYear with
            | Some year -> filters.Add($"publishedYear le {year}")
            | None -> ()
            
            if filters.Count > 0 then
                options.Filter <- String.Join(" and ", filters)
            
            let! resp = searchClient.SearchAsync<Book>(searchText, options)
            let books = 
                resp.Value.GetResults()
                |> Seq.map (fun r -> r.Document)
                |> Seq.toList
            return books
        } : System.Threading.Tasks.Task<Book list>
