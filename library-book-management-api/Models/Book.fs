module Book

open System
open Azure.Search.Documents.Indexes
open Azure.Search.Documents.Indexes.Models

// ============ Item Types ============
// Both Book and Category live in the SAME Cosmos container.
// We use "itemType" to distinguish them AND as the partition key.

module ItemType =
    let book = "book"
    let category = "category"

// ============ ID Generation ============

module IdGen =
    let generateBookId () =
        let newGuidString = System.Guid.NewGuid().ToString("N")
        "book_" + newGuidString.[..7]
    let generateCategoryId () =
        let newGuidString = System.Guid.NewGuid().ToString("N")
        "cat_" + newGuidString.[..7]

// ============ Book ============

[<CLIMutable>]
type Book =
    { [<SimpleField(IsKey = true, IsFilterable = true)>]
      id: string
      
      [<SimpleField(IsFilterable = true)>]
      bookId: string
      
      [<SearchableField(IsFilterable = true, IsSortable = true)>]
      title: string
      
      [<SearchableField(IsFilterable = true, IsSortable = true)>]
      author: string
      
      [<SimpleField(IsFilterable = true, IsFacetable = true)>]
      categoryId: string
      
      [<SearchableField(IsFilterable = true, IsFacetable = true)>]
      categoryName: string
      
      [<SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)>]
      publishedYear: int
      
      [<SimpleField(IsFilterable = true, IsSortable = true)>]
      totalCopies: int
      
      [<SimpleField(IsFilterable = true, IsSortable = true)>]
      availableCopies: int
      
      [<SimpleField(IsFilterable = true, IsFacetable = true)>]
      itemType: string
      
      [<SimpleField(IsFilterable = true, IsSortable = true)>]
      addedDate: DateTimeOffset }

// ============ Category ============

[<CLIMutable>]
type Category =
    { id: string
      categoryId: string
      name: string
      description: string
      itemType: string }

// ============ Index Configuration ============

module IndexConfiguration =
    
    let createIndexDefinition (indexName: string) =
        let index = SearchIndex(indexName)
        let fieldBuilder = FieldBuilder()
        index.Fields <- fieldBuilder.Build(typeof<Book>)
        index
    
    let createFieldsManually (indexName: string) =
        let index = SearchIndex(indexName)
        
        let idField = SearchField("id", SearchFieldDataType.String)
        idField.IsKey <- true
        idField.IsFilterable <- true
        
        let bookIdField = SearchField("bookId", SearchFieldDataType.String)
        bookIdField.IsFilterable <- true
        
        let titleField = SearchField("title", SearchFieldDataType.String)
        titleField.IsSearchable <- true
        titleField.IsFilterable <- true
        titleField.IsSortable <- true
        
        let authorField = SearchField("author", SearchFieldDataType.String)
        authorField.IsSearchable <- true
        authorField.IsFilterable <- true
        authorField.IsSortable <- true
        
        let categoryIdField = SearchField("categoryId", SearchFieldDataType.String)
        categoryIdField.IsFilterable <- true
        categoryIdField.IsFacetable <- true
        
        let categoryNameField = SearchField("categoryName", SearchFieldDataType.String)
        categoryNameField.IsSearchable <- true
        categoryNameField.IsFilterable <- true
        categoryNameField.IsFacetable <- true
        
        let publishedYearField = SearchField("publishedYear", SearchFieldDataType.Int32)
        publishedYearField.IsFilterable <- true
        publishedYearField.IsSortable <- true
        publishedYearField.IsFacetable <- true
        
        let totalCopiesField = SearchField("totalCopies", SearchFieldDataType.Int32)
        totalCopiesField.IsFilterable <- true
        totalCopiesField.IsSortable <- true
        
        let availableCopiesField = SearchField("availableCopies", SearchFieldDataType.Int32)
        availableCopiesField.IsFilterable <- true
        availableCopiesField.IsSortable <- true
        
        let itemTypeField = SearchField("itemType", SearchFieldDataType.String)
        itemTypeField.IsFilterable <- true
        itemTypeField.IsFacetable <- true
        
        let addedDateField = SearchField("addedDate", SearchFieldDataType.DateTimeOffset)
        addedDateField.IsFilterable <- true
        addedDateField.IsSortable <- true
        
        index.Fields.Add(idField)
        index.Fields.Add(bookIdField)
        index.Fields.Add(titleField)
        index.Fields.Add(authorField)
        index.Fields.Add(categoryIdField)
        index.Fields.Add(categoryNameField)
        index.Fields.Add(publishedYearField)
        index.Fields.Add(totalCopiesField)
        index.Fields.Add(availableCopiesField)
        index.Fields.Add(itemTypeField)
        index.Fields.Add(addedDateField)
        
        index

// ============ Query Builders ============

module QueryBuilder =
    
    open Azure.Search.Documents
    
    let buildSearchOptions (size, filter, orderBy, select, includeTotalCount, searchFields, facets) =
        
        let options = SearchOptions()
        
        size |> Option.iter (fun s -> options.Size <- s)
        filter |> Option.iter (fun f -> options.Filter <- f)
        orderBy |> Option.iter (fun ob -> ob |> List.iter (options.OrderBy.Add))
        select |> Option.iter (fun sel -> sel |> List.iter (options.Select.Add))
        includeTotalCount |> Option.iter (fun inc -> options.IncludeTotalCount <- inc)
        searchFields |> Option.iter (fun sf -> sf |> List.iter (options.SearchFields.Add))
        facets |> Option.iter (fun f -> f |> List.iter (options.Facets.Add))
        
        options
    
    let buildFilterExpression (filters: (string * obj) list) =
        filters
        |> List.map (fun (field, value) ->
            match value with
            | :? string as s -> $"{field} eq '{s}'"
            | :? int as i -> $"{field} eq {i}"
            | :? bool as b -> $"{field} eq {b}"
            | _ -> $"{field} eq {value}"
        )
        |> fun parts -> String.Join(" and ", parts)
    
    let buildRangeFilter (field: string) (min: 'T option) (max: 'T option) =
        match min, max with
        | Some mn, Some mx -> Some $"{field} ge {mn} and {field} le {mx}"
        | Some mn, None -> Some $"{field} ge {mn}"
        | None, Some mx -> Some $"{field} le {mx}"
        | None, None -> None

// ============ Request Models with Validation ============

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
      top: int option
      categoryId: string option
      minYear: int option
      maxYear: int option
      author: string option }

[<CLIMutable>]
type SearchResponse =
    { source: string
      count: int
      totalCount: int64
      results: Book list }

// ============ Validation Module ============

module Validation =
    
    type ValidationError = { field: string; message: string }
    
    type ValidationResult<'T> =
        | Valid of 'T
        | Invalid of ValidationError list
    
    let private isNullOrWhiteSpace (s: string) =
        String.IsNullOrWhiteSpace(s)
    
    let validateCreateBookRequest (req: CreateBookRequest) =
        let errors = ResizeArray<ValidationError>()
        
        if isNullOrWhiteSpace req.title then
            errors.Add({ field = "title"; message = "Title is required" })
        
        if req.title <> null && req.title.Length > 200 then
            errors.Add({ field = "title"; message = "Title must be less than 200 characters" })
        
        if isNullOrWhiteSpace req.author then
            errors.Add({ field = "author"; message = "Author is required" })
        
        if req.author <> null && req.author.Length > 100 then
            errors.Add({ field = "author"; message = "Author must be less than 100 characters" })
        
        if isNullOrWhiteSpace req.categoryId then
            errors.Add({ field = "categoryId"; message = "Category ID is required" })
        
        if isNullOrWhiteSpace req.categoryName then
            errors.Add({ field = "categoryName"; message = "Category name is required" })
        
        if req.publishedYear < 1000 || req.publishedYear > DateTime.UtcNow.Year + 1 then
            errors.Add({ field = "publishedYear"; message = $"Published year must be between 1000 and {DateTime.UtcNow.Year + 1}" })
        
        if req.totalCopies < 0 then
            errors.Add({ field = "totalCopies"; message = "Total copies must be non-negative" })
        
        if req.totalCopies > 10000 then
            errors.Add({ field = "totalCopies"; message = "Total copies must be less than 10000" })
        
        if errors.Count > 0 then
            Invalid (errors |> List.ofSeq)
        else
            Valid req
    
    let validateUpdateBookRequest (req: UpdateBookRequest) =
        let errors = ResizeArray<ValidationError>()
        
        match req.title with
        | Some t when isNullOrWhiteSpace t ->
            errors.Add({ field = "title"; message = "Title cannot be empty" })
        | Some t when t.Length > 200 ->
            errors.Add({ field = "title"; message = "Title must be less than 200 characters" })
        | _ -> ()
        
        match req.author with
        | Some a when isNullOrWhiteSpace a ->
            errors.Add({ field = "author"; message = "Author cannot be empty" })
        | Some a when a.Length > 100 ->
            errors.Add({ field = "author"; message = "Author must be less than 100 characters" })
        | _ -> ()
        
        match req.publishedYear with
        | Some y when y < 1000 || y > DateTime.UtcNow.Year + 1 ->
            errors.Add({ field = "publishedYear"; message = $"Published year must be between 1000 and {DateTime.UtcNow.Year + 1}" })
        | _ -> ()
        
        match req.totalCopies with
        | Some tc when tc < 0 ->
            errors.Add({ field = "totalCopies"; message = "Total copies must be non-negative" })
        | Some tc when tc > 10000 ->
            errors.Add({ field = "totalCopies"; message = "Total copies must be less than 10000" })
        | _ -> ()
        
        match req.availableCopies with
        | Some ac when ac < 0 ->
            errors.Add({ field = "availableCopies"; message = "Available copies must be non-negative" })
        | _ -> ()
        
        if errors.Count > 0 then
            Invalid (errors |> List.ofSeq)
        else
            Valid req
    
    let validateSearchRequest (req: SearchRequest) =
        let errors = ResizeArray<ValidationError>()
        
        if isNullOrWhiteSpace req.query then
            errors.Add({ field = "query"; message = "Search query is required" })
        
        match req.top with
        | Some t when t < 1 || t > 100 ->
            errors.Add({ field = "top"; message = "Top must be between 1 and 100" })
        | _ -> ()
        
        match req.minYear with
        | Some y when y < 1000 || y > DateTime.UtcNow.Year + 1 ->
            errors.Add({ field = "minYear"; message = $"Min year must be between 1000 and {DateTime.UtcNow.Year + 1}" })
        | _ -> ()
        
        match req.maxYear with
        | Some y when y < 1000 || y > DateTime.UtcNow.Year + 1 ->
            errors.Add({ field = "maxYear"; message = $"Max year must be between 1000 and {DateTime.UtcNow.Year + 1}" })
        | _ -> ()
        
        if errors.Count > 0 then
            Invalid (errors |> List.ofSeq)
        else
            Valid req
