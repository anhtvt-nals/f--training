module BookRepositoryIntegrationTests

open System
open System.Threading.Tasks
open Xunit
open Book
open BookRepository

// ============ Test Configuration ============

// NOTE: These tests require a running Cosmos DB Emulator or Azure Cosmos DB instance
// Set these environment variables before running:
// - COSMOS_ENDPOINT_URL
// - COSMOS_PRIMARY_KEY
// - COSMOS_DATABASE_ID (default: LibraryDB_Test)
// - COSMOS_CONTAINER_ID (default: Books_Test)

let getTestConfig () =
    let endpoint = 
        Environment.GetEnvironmentVariable("COSMOS_ENDPOINT_URL")
        |> Option.ofObj
        |> Option.defaultValue "https://localhost:8081"
    
    let key = 
        Environment.GetEnvironmentVariable("COSMOS_PRIMARY_KEY")
        |> Option.ofObj
        |> Option.defaultValue "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
    
    let databaseId = 
        Environment.GetEnvironmentVariable("COSMOS_DATABASE_ID")
        |> Option.ofObj
        |> Option.defaultValue "LibraryDB_Test"
    
    let containerId = 
        Environment.GetEnvironmentVariable("COSMOS_CONTAINER_ID")
        |> Option.ofObj
        |> Option.defaultValue "Books_Test"
    
    { EndpointUrl = endpoint
      PrimaryKey = key
      DatabaseId = databaseId
      ContainerId = containerId }

let createTestBook (id: string) =
    { id = id
      bookId = id
      title = "Test Book " + id
      author = "Test Author"
      categoryId = "cat_test001"
      categoryName = "Test Category"
      publishedYear = 2024
      totalCopies = 10
      availableCopies = 10
      itemType = ItemType.book
      addedDate = DateTimeOffset.UtcNow }

// ============ Test Fixtures ============

type BookRepositoryFixture() =
    let config = getTestConfig()
    let repo = BookRepository(config)
    let testBookIds = System.Collections.Concurrent.ConcurrentBag<string>()
    
    member _.Repository = repo
    
    member _.RegisterTestBook(bookId: string) =
        testBookIds.Add(bookId)
    
    member _.CleanupAsync() =
        task {
            let mutable errors = []
            for bookId in testBookIds do
                let! deleteResult = 
                    task {
                        try
                            let! _ = repo.DeleteAsync(bookId)
                            printfn "Cleaned up test book: %s" bookId
                            return Ok ()
                        with ex ->
                            return Error (bookId, ex.Message)
                    }
                match deleteResult with
                | Ok _ -> ()
                | Error (id, msg) -> errors <- (id, msg) :: errors
            
            if not errors.IsEmpty then
                printfn "Failed to clean up some test books:"
                errors |> List.iter (fun (id, err) -> printfn "  - %s: %s" id err)
            return ()
        } :> Task
    
    interface IDisposable with
        member this.Dispose() =
            this.CleanupAsync().GetAwaiter().GetResult()

// ============ Integration Tests ============

[<Collection("BookRepository")>]
type BookRepositoryIntegrationTests(fixture: BookRepositoryFixture) =
    
    [<Fact>]
    let ``CreateAsync should create a book in Cosmos DB`` () =
        task {
            // Arrange
            let bookId = IdGen.generateBookId()
            fixture.RegisterTestBook(bookId)
            let book = createTestBook bookId
            
            // Act
            let! created = fixture.Repository.CreateAsync(book)
            
            // Assert
            Assert.Equal(bookId, created.id)
            Assert.Equal("Test Book " + bookId, created.title)
            Assert.Equal(ItemType.book, created.itemType)
        }
    
    [<Fact>]
    let ``GetByIdAsync should retrieve created book`` () =
        task {
            // Arrange
            let bookId = IdGen.generateBookId()
            fixture.RegisterTestBook(bookId)
            let book = createTestBook bookId
            let! _ = fixture.Repository.CreateAsync(book)
            
            // Act
            let! retrieved = fixture.Repository.GetByIdAsync(bookId)
            
            // Assert
            Assert.Equal(bookId, retrieved.id)
            Assert.Equal(book.title, retrieved.title)
            Assert.Equal(book.author, retrieved.author)
        }
    
    [<Fact>]
    let ``UpdateAsync should update book fields`` () =
        task {
            // Arrange
            let bookId = IdGen.generateBookId()
            fixture.RegisterTestBook(bookId)
            let book = createTestBook bookId
            let! created = fixture.Repository.CreateAsync(book)
            
            // Act
            let updated = { created with title = "Updated Title"; availableCopies = 5 }
            let! saved = fixture.Repository.UpdateAsync(updated)
            
            // Assert
            Assert.Equal("Updated Title", saved.title)
            Assert.Equal(5, saved.availableCopies)
            
            // Verify it's persisted
            let! retrieved = fixture.Repository.GetByIdAsync(bookId)
            Assert.Equal("Updated Title", retrieved.title)
            Assert.Equal(5, retrieved.availableCopies)
        }
    
    [<Fact>]
    let ``DeleteAsync should remove book from Cosmos DB`` () =
        task {
            // Arrange
            let bookId = IdGen.generateBookId()
            // Don't register for cleanup since test deletes it
            let book = createTestBook bookId
            let! _ = fixture.Repository.CreateAsync(book)
            
            // Act
            let! _ = fixture.Repository.DeleteAsync(bookId)
            
            // Assert - should throw when trying to retrieve deleted book
            let! ex = Assert.ThrowsAsync<exn>(fun () -> 
                fixture.Repository.GetByIdAsync(bookId) :> Task)
            
            // Verify exception occurred (just checking it's not null is enough)
            Assert.True(ex <> null)
        }
    
    [<Fact>]
    let ``GetAllAsync should return all books`` () =
        task {
            // Arrange - create multiple test books
            let bookIds = [1..3] |> List.map (fun i -> IdGen.generateBookId())
            bookIds |> List.iter fixture.RegisterTestBook
            
            for bookId in bookIds do
                let book = createTestBook bookId
                let! _ = fixture.Repository.CreateAsync(book)
                ()
            
            // Act
            let! allBooks = fixture.Repository.GetAllAsync()
            
            // Assert
            let createdBooks = allBooks |> List.filter (fun b -> bookIds |> List.contains b.id)
            Assert.True(createdBooks.Length >= 3, "Should have at least the 3 created books")
        }
    
    [<Fact>]
    let ``SearchAsync should find books by title`` () =
        task {
            // Arrange
            let bookId = IdGen.generateBookId()
            fixture.RegisterTestBook(bookId)
            let book = { createTestBook bookId with title = "Unique Search Term 12345" }
            let! _ = fixture.Repository.CreateAsync(book)
            
            // Wait a bit for indexing
            do! Task.Delay(1000)
            
            // Act
            let! results = fixture.Repository.SearchAsync("Unique Search Term")
            
            // Assert
            let found = results |> List.tryFind (fun b -> b.id = bookId)
            Assert.True(found.IsSome, "Should find the created book")
        }
    
    [<Fact>]
    let ``Multiple operations in sequence should work correctly`` () =
        task {
            // Arrange
            let bookId = IdGen.generateBookId()
            fixture.RegisterTestBook(bookId)
            let book = createTestBook bookId
            
            // Act & Assert - Create
            let! created = fixture.Repository.CreateAsync(book)
            Assert.Equal(bookId, created.id)
            
            // Act & Assert - Read
            let! retrieved = fixture.Repository.GetByIdAsync(bookId)
            Assert.Equal(created.title, retrieved.title)
            
            // Act & Assert - Update
            let updated = { retrieved with availableCopies = 7 }
            let! saved = fixture.Repository.UpdateAsync(updated)
            Assert.Equal(7, saved.availableCopies)
            
            // Act & Assert - Read again
            let! retrievedAgain = fixture.Repository.GetByIdAsync(bookId)
            Assert.Equal(7, retrievedAgain.availableCopies)
        }

// ============ Validation Tests with DB ============

[<Collection("BookRepository")>]
type BookValidationIntegrationTests(fixture: BookRepositoryFixture) =
    
    [<Fact>]
    let ``CreateAsync with duplicate ID should fail`` () =
        task {
            // Arrange
            let bookId = IdGen.generateBookId()
            fixture.RegisterTestBook(bookId)
            let book1 = createTestBook bookId
            let book2 = createTestBook bookId
            
            // Act - Create first book
            let! _ = fixture.Repository.CreateAsync(book1)
            
            // Assert - Creating second book with same ID should fail
            let! ex = Assert.ThrowsAsync<exn>(fun () ->
                fixture.Repository.CreateAsync(book2) :> Task)
            
            Assert.NotNull(ex)
        }
    
    [<Fact>]
    let ``GetByIdAsync with non-existent ID should fail`` () =
        task {
            // Arrange
            let nonExistentId = "book_notexist"
            
            // Act & Assert
            let! ex = Assert.ThrowsAsync<exn>(fun () ->
                fixture.Repository.GetByIdAsync(nonExistentId) :> Task)
            
            Assert.NotNull(ex)
        }
    
    [<Fact>]
    let ``UpdateAsync with non-existent ID should fail`` () =
        task {
            // Arrange
            let nonExistentBook = createTestBook "book_notexist"
            
            // Act & Assert
            let! ex = Assert.ThrowsAsync<exn>(fun () ->
                fixture.Repository.UpdateAsync(nonExistentBook) :> Task)
            
            Assert.NotNull(ex)
        }

[<CollectionDefinition("BookRepository")>]
type BookRepositoryCollection() =
    interface ICollectionFixture<BookRepositoryFixture>
