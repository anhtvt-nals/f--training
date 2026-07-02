module CategoryTests

open System
open Xunit
open Book

// ============ Category Record Tests ============

[<Fact>]
let ``Category record should hold all fields`` () =
    let cat =
        { id = "cat_abc12345"
          categoryId = "cat_abc12345"
          name = "Fiction"
          description = "Imaginative works"
          itemType = ItemType.category }
    Assert.Equal("cat_abc12345", cat.id)
    Assert.Equal("Fiction", cat.name)
    Assert.Equal("Imaginative works", cat.description)
    Assert.Equal(ItemType.category, cat.itemType)

[<Fact>]
let ``Category update with "with" should preserve other fields`` () =
    let cat =
        { id = "cat_001"
          categoryId = "cat_001"
          name = "Old Name"
          description = "Old desc"
          itemType = ItemType.category }
    let updated = { cat with name = "New Name" }
    Assert.Equal("New Name", updated.name)
    Assert.Equal("Old desc", updated.description)
    Assert.Equal("cat_001", updated.id)

[<Fact>]
let ``CLIMutable Category should allow property mutation`` () =
    let cat =
        { id = "cat_001"
          categoryId = "cat_001"
          name = "Test"
          description = "Desc"
          itemType = ItemType.category }
    cat.name <- "Changed"
    Assert.Equal("Changed", cat.name)

// ============ Shared Container Concept ============

[<Fact>]
let ``Book and Category use different itemType values`` () =
    let book =
        { id = "book_001"; bookId = "book_001"; title = "T"; author = "A"
          genre = "G"; publishedYear = 2020; totalCopies = 1
          availableCopies = 1; itemType = ItemType.book; addedDate = DateTime.UtcNow }
    let cat =
        { id = "cat_001"; categoryId = "cat_001"; name = "N"
          description = "D"; itemType = ItemType.category }
    Assert.Equal(ItemType.book, book.itemType)
    Assert.Equal(ItemType.category, cat.itemType)
    Assert.NotEqual(book.itemType, cat.itemType)
