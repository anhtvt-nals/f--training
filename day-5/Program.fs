module Program

open System
open Microsoft.Azure.Cosmos
open Config
open Database
open Book
open Category
open Types
open ChangeFeed
open System.Threading.Tasks

[<EntryPoint>]
let main argv =
    let cfg = emulatorConfig
    let client = createClient cfg true

    task {
        let! containers = initLibraryDatabase client cfg true

        let newCategory = {
            id = "van-hoc"
            name = "Văn học"
            description = "Các tác phẩm văn học Việt Nam và thế giới"
            bookCount = 0
            isActive = true
            sortOrder = 1
            createdAt = DateTime.UtcNow
        }
        let newBook = {
            id = "1"
            title = "Truyện Kiều"
            author = "Nguyễn Du"
            category = newCategory.id
            categoryName = newCategory.name
            description = "Tác phẩm văn học kinh điển của Việt Nam"
            isAvailable = true
            createdAt = DateTime.UtcNow
            isDeleted = false
        }

        // Tạo category mới
        let! createdCategory = createCategory containers.Categories newCategory
        printfn "Category created: %s" createdCategory.name

        // Tạo book mới thuộc category vừa tạo
        let! createdBook = createBook containers.Books newBook
        printfn "Book created: %s (Category: %s)" createdBook.title createdBook.categoryName

        printfn "\n--- BOOK DATA ---\n"

        // Lấy tất cả book
        let! books = allBooks containers.Books
        printfn "All books in library:"
        books
            |> List.iter (fun b -> printfn " - %s (Category: %s)" b.title b.categoryName)

        // Cập nhật tên book
        let updatedBook = { createdBook with title = "Truyện Kiều - Bản cập nhật" }
        let! _ = updateBook containers.Books updatedBook
        printfn "Book updated: %s" updatedBook.title

        // Lấy book theo id
        let! bookOpt = getBookById (containers.Books) newBook.id newBook.category
        match bookOpt with
        | Some book -> printfn "Book retrieved: %s (Category: %s)" book.title book.categoryName
        | None -> printfn "Book with id '%s' not found." newBook.id

        // Xóa book
        let! deleteResult = deleteBook containers.Books newBook.id newBook.category
        match deleteResult with
        | Ok true -> printfn "Book with id '%s' deleted successfully." newBook.id
        | Ok false -> printfn "Book with id '%s' was not found for deletion." newBook.id
        | Error msg -> printfn "Error deleting book: %s" msg

        printfn "\n--- CATEGORY DATA ---\n"

        // Lấy tất cả category
        let! categories = allCategories containers.Categories
        printfn "All categories in library:"
        categories
            |> List.iter (fun c -> printfn " - %s: %s" c.name c.description)
        
        // Cập nhật category (thay đổi description)
        let updatedCategory = { newCategory with description = "Tác phẩm văn học Việt Nam và thế giới - cập nhật" }
        let! _ = updateCategory containers.Categories updatedCategory

        // Lấy category theo id
        let! categoryOpt = getCategoryById containers.Categories newCategory.id
        match categoryOpt with
        | Some cat -> printfn "Category retrieved: %s - %s" cat.name cat.description
        | None -> printfn "Category with id '%s' not found." newCategory.id

        // Xóa category
        let! deleteResult = deleteCategory containers.Categories newCategory.id
        match deleteResult with
        | Ok true -> printfn "Category with id '%s' deleted successfully." newCategory.id
        | Ok false -> printfn "Category with id '%s' was not found for deletion." newCategory.id
        | Error msg -> printfn "Error deleting category: %s" msg

        // let processor = createBookChangeFeedProcessor containers.Books containers.Categories containers.Leases
        // do! processor.StartAsync()

        // printfn "Change feed processor started. Listening for changes..."

        // // Giữ app chạy để processor hoạt động
        // do! Task.Delay(-1)

        return 0 
    } |> Async.AwaitTask
      |> Async.RunSynchronously