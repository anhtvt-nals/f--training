module SearchClient

open System
open System.Collections.Generic
open System.Threading.Tasks
open Azure
open Azure.Search.Documents
open Azure.Search.Documents.Models
open Microsoft.Azure.Cosmos
open Config
open Types
open SearchIndexClient
open Book


let searchBookClient = new SearchClient(Uri(azureSearchConfig.EndpointUrl), azureSearchConfig.BookIndex, AzureKeyCredential(azureSearchConfig.ReadKey))

let createSampleBooks () : Book list = [
    {
        id = "book-001"
        category = "cat-literature-vn"
        categoryName = "Văn học Việt Nam"
        itemType = "book"
        title = "Số Đỏ"
        author = "Vũ Trọng Phụng"
        description = "Tác phẩm nổi tiếng về xã hội Hà Nội đầu thế kỷ 20, phê phán tệ nạn xã hội và sự giả dối"
        isAvailable = true
        isDeleted = false
        createdAt = DateTime(2023, 1, 15)
    }
    {
        id = "book-002"
        category = "cat-literature-vn"
        categoryName = "Văn học Việt Nam"
        itemType = "book"
        title = "Chí Phèo"
        author = "Nam Cao"
        description = "Truyện ngắn kinh điển về con người bị xã hội đẩy đưa, tâm lý nhân vật sâu sắc"
        isAvailable = true
        isDeleted = false
        createdAt = DateTime(2023, 2, 10)
    }
    {
        id = "book-003"
        category = "cat-literature-vn"
        categoryName = "Văn học Việt Nam"
        itemType = "book"
        title = "Tắt Đèn"
        author = "Ngô Tất Tố"
        description = "Tiểu thuyết về cuộc sống nông dân nghèo khổ dưới ách thống trị thực dân"
        isAvailable = false  // Đang được mượn
        isDeleted = false
        createdAt = DateTime(2023, 3, 5)
    }
    
    {
        id = "book-004"
        category = "cat-literature-world"
        categoryName = "Văn học nước ngoài"
        itemType = "book"
        title = "1984"
        author = "George Orwell"
        description = "Dystopian novel về xã hội toàn trị, giám sát tuyệt đối và kiểm soát tư tưởng"
        isAvailable = true
        isDeleted = false
        createdAt = DateTime(2023, 4, 20)
    }
    {
        id = "book-005"
        category = "cat-literature-world"
        categoryName = "Văn học nước ngoài"
        itemType = "book"
        title = "The Great Gatsby"
        author = "F. Scott Fitzgerald"
        description = "Classic American novel về giấc mơ Mỹ, tình yêu và sự ảo tưởng trong thập niên 1920"
        isAvailable = true
        isDeleted = false
        createdAt = DateTime(2023, 5, 12)
    }
    {
        id = "book-006"
        category = "cat-literature-world"
        categoryName = "Văn học nước ngoài"
        itemType = "book"
        title = "One Hundred Years of Solitude"
        author = "Gabriel García Márquez"
        description = "Kiệt tác magical realism về gia tộc Buendía qua bảy thế hệ"
        isAvailable = true
        isDeleted = false
        createdAt = DateTime(2023, 6, 8)
    }
    
    {
        id = "book-007"
        category = "cat-tech"
        categoryName = "Khoa học - Công nghệ"
        itemType = "book"
        title = "Clean Code"
        author = "Robert C. Martin"
        description = "Hướng dẫn viết code sạch, maintainable và professional cho software engineers"
        isAvailable = true
        isDeleted = false
        createdAt = DateTime(2024, 1, 10)
    }
    {
        id = "book-008"
        category = "cat-tech"
        categoryName = "Khoa học - Công nghệ"
        itemType = "book"
        title = "Domain-Driven Design"
        author = "Eric Evans"
        description = "Thiết kế phần mềm dựa trên domain model, tactical và strategic patterns"
        isAvailable = true
        isDeleted = false
        createdAt = DateTime(2024, 2, 15)
    }
    {
        id = "book-009"
        category = "cat-tech"
        categoryName = "Khoa học - Công nghệ"
        itemType = "book"
        title = "The Pragmatic Programmer"
        author = "Andrew Hunt, David Thomas"
        description = "Best practices và mindset của lập trình viên chuyên nghiệp"
        isAvailable = false  // Đang được mượn
        isDeleted = false
        createdAt = DateTime(2024, 3, 20)
    }

    {
        id = "book-010"
        category = "cat-self-help"
        categoryName = "Tự lực - Kỹ năng sống"
        itemType = "book"
        title = "Atomic Habits"
        author = "James Clear"
        description = "Xây dựng thói quen tốt và phá bỏ thói quen xấu bằng các chiến lược khoa học"
        isAvailable = true
        isDeleted = false
        createdAt = DateTime(2024, 5, 1)
    }
    {
        id = "book-011"
        category = "cat-self-help"
        categoryName = "Tự lực - Kỹ năng sống"
        itemType = "book"
        title = "Deep Work"
        author = "Cal Newport"
        description = "Làm việc tập trung sâu trong thời đại phân tâm và tối đa hóa năng suất"
        isAvailable = true
        isDeleted = false
        createdAt = DateTime(2024, 6, 10)
    }
    
    {
        id = "book-012"
        category = "cat-history"
        categoryName = "Lịch sử"
        itemType = "book"
        title = "Sapiens: A Brief History of Humankind"
        author = "Yuval Noah Harari"
        description = "Lịch sử loài người từ thời kỳ đồ đá đến cách mạng nhận thức và khoa học"
        isAvailable = true
        isDeleted = false
        createdAt = DateTime(2024, 7, 5)
    }
    {
        id = "book-013"
        category = "cat-history"
        categoryName = "Lịch sử"
        itemType = "book"
        title = "Guns, Germs, and Steel"
        author = "Jared Diamond"
        description = "Tại sao một số nền văn minh phát triển nhanh hơn — phân tích địa lý, sinh học"
        isAvailable = true
        isDeleted = false
        createdAt = DateTime(2024, 8, 12)
    }
    
    {
        id = "book-014"
        category = "cat-literature-vn"
        categoryName = "Văn học Việt Nam"
        itemType = "book"
        title = "Book cũ không còn dùng"
        author = "Unknown"
        description = "Sách này đã bị xóa khỏi thư viện"
        isAvailable = false
        isDeleted = true  // Đã xóa
        createdAt = DateTime(2022, 1, 1)
    }
]

let uploadBooks () =
    let books = createSampleBooks ()
    let batch = IndexDocumentsBatch.MergeOrUpload(books)
    let result = searchBookClient.IndexDocuments(batch)
    
    printfn "Upload thành công %d sách" books.Length
    
    // Check for any failures
    let failures = 
        result.Value.Results 
        |> Seq.filter (fun r -> not r.Succeeded)
        |> Seq.toList
    
    if not failures.IsEmpty then
        printfn "⚠️  Một số documents bị lỗi:"
        failures |> List.iter (fun r ->
            printfn "   ❌ Document '%s': Status %d - %s" r.Key r.Status r.ErrorMessage
        )
    
    result.Value.Results


let searchAllBooks () = 
    let response = searchBookClient.Search<Book>("*")
    response.Value.GetResults() |> Seq.toList


let searchByKeyword (keyword: string) =
    let response = searchBookClient.Search<Book>(keyword)    
    let totalCount = response.Value.TotalCount.GetValueOrDefault(0L)
    printfn "Tìm thấy %d sách với từ khóa '%s'" (int totalCount) keyword
    response.Value.GetResults()
    |> Seq.iter (fun result ->
        let book = result.Document
        let score = result.Score.GetValueOrDefault(0.0)
        printfn "ID: %s, Title: %s, Author: %s, Available: %b, Score: %f" book.id book.title book.author book.isAvailable score
    )
    
    response.Value.GetResults() |> Seq.toList


let getAvailabelBooks () =
    let options = SearchOptions()
    options.Filter <- "isAvailable eq true and isDeleted eq false"
    options.OrderBy.Add("createdAt desc")
    let response = searchBookClient.Search<Book>("*", options)
    response.Value.GetResults()
    |> Seq.iter (fun result ->
        let book = result.Document
        printfn "ID: %s, Title: %s, Author: %s, Available: %b" book.id book.title book.author book.isAvailable
    )


// Search với điều kiện nằm trong 1 mảng
let getLiteratureBooks () = 
    let option = SearchOptions()
    option.Filter <- "search.in(category, 'cat-literature-vn|cat-literature-world', '|') and isDeleted eq false"
    let response = searchBookClient.Search<Book>("*", option)
    response.Value.GetResults()
    |> Seq.iter (fun result ->
        let book = result.Document
        printfn "ID: %s, Title: %s, Author: %s, Category: %s" book.id book.title book.author book.category
    )


let searchWithFacets () =
    let options = SearchOptions()

    options.Filter <- "isDeleted eq false"

    options.Facets.Add("category,count:5")
    options.Facets.Add("isAvailable")

    let response = searchBookClient.Search<Book>("*", options)

    if response.Value.Facets.ContainsKey("category") then
        printfn "Facets cho category:"
        for facet in response.Value.Facets.["category"] do
            let value = facet.["value"].ToString()
            let count = int (facet.["count"] :?> int64)
            printfn "  Giá trị: %s, Count: %d" value count

    if response.Value.Facets.ContainsKey("isAvailable") then
        printfn "Facets cho isAvailable:"
        for facet in response.Value.Facets.["isAvailable"] do
            let value = facet.["value"] :?> bool
            let count = int (facet.["count"] :?> int64)
            printfn "  Giá trị: %b, Count: %d" value count
        
    
    response.Value.GetResults() |> Seq.toList

let autoCompleteSearch (prefix: string) =
    if prefix.Length < 2 then
        printfn "Prefix quá ngắn, cần ít nhất 2 ký tự"
        []
    else
        let options = AutocompleteOptions()
        options.Mode <- AutocompleteMode.OneTermWithContext
        options.Size <- 5

        let response = searchBookClient.Autocomplete(prefix, "sg", options)

        let suggestions =
            response.Value.Results
            |> Seq.map (fun r -> r.Text)
            |> Seq.toList

        suggestions


let searchWithPagination (query: string) (pageNumber: int) (pageSize: int) : PaginationResult<Book> =
    let options = SearchOptions()
    options.Filter <- "isDeleted eq false"
    options.Skip <- (pageNumber - 1) * pageSize
    options.Size <- pageSize
    options.IncludeTotalCount <- true
    let response = searchBookClient.Search<Book>(query, options)
    let totalCount = response.Value.TotalCount.GetValueOrDefault(0L)
    let totalPages = int (Math.Ceiling(float totalCount / float pageSize))
    {
        Items = response.Value.GetResults() |> Seq.map (fun r -> r.Document) |> Seq.toList
        TotalCount = totalCount
        PageNumber = pageNumber
        PageSize = pageSize
        TotalPages = totalPages
    }


let syncBooksFromCosmos (container: Container) =
    task {
        let! books = Book.allBooks container
        let booksFromCosmos = books |> Seq.toList
        
        let batch = IndexDocumentsBatch.MergeOrUpload(booksFromCosmos)
        let! result = searchBookClient.IndexDocumentsAsync(batch)
        
        printfn "Upload thành công %d sách" booksFromCosmos.Length
        
        // Check for any failures
        let failures = 
            result.Value.Results 
            |> Seq.filter (fun r -> not r.Succeeded)
            |> Seq.toList
        
        if not failures.IsEmpty then
            printfn "⚠️  Một số documents bị lỗi:"
            failures |> List.iter (fun r ->
                printfn "   ❌ Sách '%s': Status %d - %s" r.Key r.Status r.ErrorMessage
            )
        
        return result.Value.Results
    }