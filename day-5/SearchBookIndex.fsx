// QuickTest.fsx - Test nhanh SearchClient.fs
// Chạy: dotnet fsi QuickTest.fsx

#r "nuget: Azure.Search.Documents"
#r "nuget: Microsoft.Azure.Cosmos"

#load "Config.fs"
#load "Types.fs"
#load "SearchIndexClient.fs"
#load "Database.fs"
#load "Category.fs"
#load "Book.fs"
#load "SearchClient.fs"

open System
open SearchClient

printfn "SearchClient.fs\n"

// STEP 1: Upload sample books
printfn "Push init data lên Azure Search Index"
try
    let results = uploadBooks()
    let succeeded = results |> Seq.filter (fun r -> r.Succeeded) |> Seq.length
    printfn "Tải lên %d sách thành công" succeeded
    System.Threading.Thread.Sleep(2000)
with ex ->
    printfn "Tải lên thất bại: %s" ex.Message

printfn "Tìm tất cả sách..."
try
    let results = searchAllBooks()
    printfn "Tìm thấy %d sách" (results |> List.length)
    
    results
    |> List.take (min 2 (results |> List.length))
    |> List.iter (fun result ->
        printfn "   • %s by %s" result.Document.title result.Document.author
    )
with ex ->
    printfn "Tìm kiếm thất bại: %s" ex.Message

printfn "Tìm kiếm với từ khóa 'code'..."
try
    let results = searchByKeyword "code"
    
    results
    |> Seq.take (min 2 (results |> Seq.length))
    |> Seq.iter (fun result ->
        let score = result.Score.GetValueOrDefault(0.0)
        printfn "   • %s (score: %.4f)" result.Document.title score
    )
with ex ->
    printfn "Tìm kiếm thất bại: %s" ex.Message

printfn "Tìm sách còn hàng..."
try
    getAvailabelBooks()
with ex ->
    printfn "Lọc thất bại: %s" ex.Message

printfn "Tìm kiếm gợi ý cho 'cle'..."
try
    let suggestions = autoCompleteSearch "cle"
    
    if suggestions.IsEmpty then
        printfn "Không có gợi ý"
    else
        suggestions |> List.iter (fun s -> printfn "   • %s" s)
with ex ->
    printfn "Tìm kiếm gợi ý thất bại: %s" ex.Message

printfn "Phần Trang (page 1, size 3)..."
try
    let page1 = searchWithPagination "*" 1 3
    printfn "Tổng số: %d sách, %d trang" page1.TotalCount page1.TotalPages
    
    page1.Items
    |> List.iteri (fun i book ->
        printfn "   %d. %s" (i + 1) book.title
    )
with ex ->
    printfn "Phân trang thất bại: %s" ex.Message

printfn "Done!"
