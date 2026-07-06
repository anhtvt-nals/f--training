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

    // Search with count result
    member _.SearchWithCountAsync(searchText: string, ?top: int) =
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
            
            let totalCount = resp.Value.TotalCount |> Option.ofNullable |> Option.defaultValue 0L
            
            return (books, totalCount)
        } : System.Threading.Tasks.Task<Book list * int64>

    // Build filter query string
    member _.BuildFilterQuery(?categoryId: string, ?minYear: int, ?maxYear: int, ?author: string) =
        let filters = ResizeArray<string>()
        
        match categoryId with
        | Some cid when not (String.IsNullOrWhiteSpace(cid)) -> 
            filters.Add($"categoryId eq '{cid}'")
        | _ -> ()
        
        match minYear with
        | Some year when year > 0 -> 
            filters.Add($"publishedYear ge {year}")
        | _ -> ()
        
        match maxYear with
        | Some year when year > 0 -> 
            filters.Add($"publishedYear le {year}")
        | _ -> ()
        
        match author with
        | Some a when not (String.IsNullOrWhiteSpace(a)) -> 
            filters.Add($"author eq '{a}'")
        | _ -> ()
        
        if filters.Count > 0 then
            String.Join(" and ", filters)
        else
            null

    // Advanced search with filters and count
    member this.SearchWithFiltersAsync(searchText: string, ?categoryId: string, ?minYear: int, ?maxYear: int, ?author: string, ?top: int) =
        task {
            let options = SearchOptions()
            options.Size <- defaultArg top 50
            options.IncludeTotalCount <- true
            
            // Build filter expression
            let filterQuery = this.BuildFilterQuery(?categoryId = categoryId, ?minYear = minYear, ?maxYear = maxYear, ?author = author)
            if not (isNull filterQuery) then
                options.Filter <- filterQuery
            
            let! resp = searchClient.SearchAsync<Book>(searchText, options)
            let books = 
                resp.Value.GetResults()
                |> Seq.map (fun r -> r.Document)
                |> Seq.toList
            
            let totalCount = resp.Value.TotalCount |> Option.ofNullable |> Option.defaultValue 0L
            
            return (books, totalCount)
        } : System.Threading.Tasks.Task<Book list * int64>
