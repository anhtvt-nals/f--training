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

    // Build full-text query targeting multiple fields (not OData filter)
    member _.BuildSearchTextQuery(searchText: string) =
        if String.IsNullOrWhiteSpace(searchText) then
            "*"
        else
            let q = searchText.Trim().Replace("\"", "\\\"")
            $"title:(\"{q}\") OR author:(\"{q}\") OR categoryName:(\"{q}\")"

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

    member this.SearchAsync(searchText: string, ?top: int) =
        task {
            let options = SearchOptions()
            options.Size <- defaultArg top 20
            options.IncludeTotalCount <- true
            options.QueryType <- SearchQueryType.Full
            let query = this.BuildSearchTextQuery(searchText)
            let! resp = searchClient.SearchAsync<Book>(query, options)
            
            // Parse results to Book list
            let books = 
                resp.Value.GetResults()
                |> Seq.map (fun r -> r.Document)
                |> Seq.toList
            
            return books
        } : System.Threading.Tasks.Task<Book list>

    // Search with count result
    member this.SearchWithCountAsync(searchText: string, ?top: int) =
        task {
            let options = SearchOptions()
            options.Size <- defaultArg top 20
            options.IncludeTotalCount <- true
            options.QueryType <- SearchQueryType.Full
            let query = this.BuildSearchTextQuery(searchText)
            let! resp = searchClient.SearchAsync<Book>(query, options)
            
            // Parse results to Book list
            let books = 
                resp.Value.GetResults()
                |> Seq.map (fun r -> r.Document)
                |> Seq.toList
            
            let totalCount = resp.Value.TotalCount |> Option.ofNullable |> Option.defaultValue 0L
            
            return (books, totalCount)
        } : System.Threading.Tasks.Task<Book list * int64>

    // Build filter query string from SearchRequest model
    member _.BuildFilterQuery(req: SearchRequest) =
        let filters = ResizeArray<string>()
        
        match req.categoryId with
        | Some cid when not (String.IsNullOrWhiteSpace(cid)) -> 
            filters.Add($"categoryId eq '{cid}'")
        | _ -> ()
        
        match req.minYear with
        | Some year when year > 0 -> 
            filters.Add($"publishedYear ge {year}")
        | _ -> ()
        
        match req.maxYear with
        | Some year when year > 0 -> 
            filters.Add($"publishedYear le {year}")
        | _ -> ()
        
        match req.author with
        | Some a when not (String.IsNullOrWhiteSpace(a)) -> 
            filters.Add($"author eq '{a}'")
        | _ -> ()
        
        if filters.Count > 0 then
            String.Join(" and ", filters)
        else
            null

    // Advanced search with filters and count - using SearchRequest model
    member this.SearchWithFiltersAsync(req: SearchRequest) =
        task {
            let options = SearchOptions()
            options.Size <- defaultArg req.top 50
            options.IncludeTotalCount <- true
            options.QueryType <- SearchQueryType.Full

            let query = this.BuildSearchTextQuery(req.query)
            // Build filter expression from model
            let filterQuery = this.BuildFilterQuery(req)
            if not (isNull filterQuery) then
                options.Filter <- filterQuery
            
            let! resp = searchClient.SearchAsync<Book>(query, options)
            let books = 
                resp.Value.GetResults()
                |> Seq.map (fun r -> r.Document)
                |> Seq.toList
            
            let totalCount = resp.Value.TotalCount |> Option.ofNullable |> Option.defaultValue 0L
            
            return (books, totalCount)
        } : System.Threading.Tasks.Task<Book list * int64>
