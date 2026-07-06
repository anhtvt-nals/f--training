module BookTests

open System
open Xunit
open Book

// ============ ItemType Tests ============

[<Fact>]
let ``ItemType.book should be "book"`` () =
    Assert.Equal("book", ItemType.book)

[<Fact>]
let ``ItemType.category should be "category"`` () =
    Assert.Equal("category", ItemType.category)

[<Fact>]
let ``ItemType.book and category should be different`` () =
    Assert.NotEqual<string>(ItemType.book, ItemType.category)

// ============ IdGen Tests ============

[<Fact>]
let ``generateBookId should start with "book_"`` () =
    let id = IdGen.generateBookId()
    Assert.StartsWith("book_", id)

[<Fact>]
let ``generateBookId should have 13 characters total`` () =
    let id = IdGen.generateBookId()
    Assert.Equal(13, id.Length)

[<Fact>]
let ``generateCategoryId should start with "cat_"`` () =
    let id = IdGen.generateCategoryId()
    Assert.StartsWith("cat_", id)

[<Fact>]
let ``generateCategoryId should have 12 characters total`` () =
    let id = IdGen.generateCategoryId()
    Assert.Equal(12, id.Length)

[<Fact>]
let ``generateBookId should produce unique ids`` () =
    let id1 = IdGen.generateBookId()
    let id2 = IdGen.generateBookId()
    Assert.NotEqual<string>(id1, id2)

[<Fact>]
let ``generateCategoryId should produce unique ids`` () =
    let id1 = IdGen.generateCategoryId()
    let id2 = IdGen.generateCategoryId()
    Assert.NotEqual<string>(id1, id2)

// ============ Book Record Tests ============

[<Fact>]
let ``Book record should hold all fields`` () =
    let now = DateTimeOffset.UtcNow
    let book =
        { id = "book_abc12345"
          bookId = "book_abc12345"
          title = "Clean Code"
          author = "Robert C. Martin"
          categoryId = "cat_tech001"
          categoryName = "Technology"
          publishedYear = 2008
          totalCopies = 5
          availableCopies = 3
          itemType = ItemType.book
          addedDate = now }
    Assert.Equal("book_abc12345", book.id)
    Assert.Equal("Clean Code", book.title)
    Assert.Equal("Robert C. Martin", book.author)
    Assert.Equal("cat_tech001", book.categoryId)
    Assert.Equal("Technology", book.categoryName)
    Assert.Equal(2008, book.publishedYear)
    Assert.Equal(5, book.totalCopies)
    Assert.Equal(3, book.availableCopies)
    Assert.Equal(ItemType.book, book.itemType)
    Assert.Equal(now, book.addedDate)

[<Fact>]
let ``Book update with "with" should preserve other fields`` () =
    let book =
        { id = "book_001"
          bookId = "book_001"
          title = "Old Title"
          author = "Author"
          categoryId = "cat_001"
          categoryName = "Fiction"
          publishedYear = 2020
          totalCopies = 10
          availableCopies = 8
          itemType = ItemType.book
          addedDate = DateTimeOffset.UtcNow }
    let updated = { book with title = "New Title"; availableCopies = 5 }
    Assert.Equal("New Title", updated.title)
    Assert.Equal(5, updated.availableCopies)
    // unchanged fields
    Assert.Equal("book_001", updated.id)
    Assert.Equal("Author", updated.author)
    Assert.Equal("cat_001", updated.categoryId)
    Assert.Equal("Fiction", updated.categoryName)
    Assert.Equal(10, updated.totalCopies)
