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
    let cfg = azureConfig
    let client = createClient cfg false

    task {
        let! containers = initLibraryDatabase client cfg false

        let newCategory = {
            id = "van-hoc"
            name = "Văn học"
            description = "Các tác phẩm văn học Việt Nam và thế giới"
            itemType = "category"
            bookCount = 0
            isActive = true
            sortOrder = 1
            createdAt = DateTime.UtcNow
        }

        let newCategory2 = {
            id = "van-hoc"
            name = "Văn học - Cập nhật 2"
            description = "Các tác phẩm văn học Việt Nam và thế giới"
            itemType = "category"
            bookCount = 0
            isActive = true
            sortOrder = 1
            createdAt = DateTime.UtcNow
        }

        let newCategory3 = {
            id = "van-hoc-3"
            name = "Văn học - New"
            description = "Các tác phẩm văn học Việt Nam và thế giới"
            itemType = "category"
            bookCount = 0
            isActive = true
            sortOrder = 1
            createdAt = DateTime.UtcNow
        }

        let newBook = {
            id = "1"
            title = "Truyện Kiều"
            author = "Nguyễn Du"
            itemType = "book"

            category = newCategory.id
            categoryName = newCategory.name

            description = "Tác phẩm văn học kinh điển của Việt Nam"
            isAvailable = true
            createdAt = DateTime.UtcNow
            isDeleted = false
        }

        let newBook2 = {
            id = "1"
            title = "Truyện Kiều - Bản cập nhật 2"
            author = "Nguyễn Du"
            itemType = "book"

            category = newCategory.id
            categoryName = newCategory.name

            description = "Tác phẩm văn học kinh điển của Việt Nam"
            isAvailable = true
            createdAt = DateTime.UtcNow
            isDeleted = false
        }

        let newBook3 = {
            id = "2"
            title = "Truyện Kiều - Bản cập nhật 3"
            author = "Nguyễn Du"
            itemType = "book"

            category = newCategory.id
            categoryName = newCategory.name

            description = "Tác phẩm văn học kinh điển của Việt Nam"
            isAvailable = true
            createdAt = DateTime.UtcNow
            isDeleted = false
        }

        // Tạo category mới
        let! createdCategory = createCategory containers.Books newCategory
        printfn "Category created: %s" createdCategory.name

        // Tạo category mới (cập nhật) - upsert
        let! createdCategory2 = createOrUpdate containers.Books newCategory2
        printfn "Category created or updated: %s" createdCategory2.name

        // Tạo category mới (upsert) - tạo mới
        let! createdCategory3 = createOrUpdate containers.Books newCategory3
        printfn "Category created or updated: %s" createdCategory3.name

        // Tạo book mới thuộc category vừa tạo
        let! createdBook = createBook containers.Books newBook
        printfn "Book created: %s (Category: %s)" createdBook.title createdBook.categoryName

        // Tạo book mới (cập nhật) - upsert
        let! createdBook2 = createBook containers.Books newBook2
        printfn "Book created: %s (Category: %s)" createdBook2.title createdBook2.categoryName

        // Tạo book mới (upsert) - tạo mới
        let! createdBook3 = createBook containers.Books newBook3
        printfn "Book created: %s (Category: %s)" createdBook3.title createdBook3.categoryName

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
        let! bookOpt = getBookById (containers.Books) newBook.id
        match bookOpt with
        | Some book -> printfn "Book retrieved: %s (Category: %s)" book.title book.categoryName
        | None -> printfn "Book with id '%s' not found." newBook.id

        // Xóa book
        let! deleteResult = deleteBook containers.Books newBook.id
        match deleteResult with
        | Ok true -> printfn "Book with id '%s' deleted successfully." newBook.id
        | Ok false -> printfn "Book with id '%s' was not found for deletion." newBook.id
        | Error msg -> printfn "Error deleting book: %s" msg

        printfn "\n--- CATEGORY DATA ---\n"

        // Lấy tất cả category
        let! categories = allCategories containers.Books
        printfn "All categories in library:"
        categories
            |> List.iter (fun c -> printfn " - %s: %s" c.name c.description)
        
        // Cập nhật category (thay đổi description)
        let updatedCategory = { newCategory with description = "Tác phẩm văn học Việt Nam và thế giới - cập nhật" }
        let! _ = updateCategory containers.Books updatedCategory

        // Lấy category theo id
        let! categoryOpt = getCategoryById containers.Books newCategory.id
        match categoryOpt with
        | Some cat -> printfn "Category retrieved: %s - %s" cat.name cat.description
        | None -> printfn "Category with id '%s' not found." newCategory.id

        // Xóa category
        let! deleteResult = deleteCategory containers.Books newCategory.id
        match deleteResult with
        | Ok true -> printfn "Category with id '%s' deleted successfully." newCategory.id
        | Ok false -> printfn "Category with id '%s' was not found for deletion." newCategory.id
        | Error msg -> printfn "Error deleting category: %s" msg

        return 0 
    } |> Async.AwaitTask
      |> Async.RunSynchronously