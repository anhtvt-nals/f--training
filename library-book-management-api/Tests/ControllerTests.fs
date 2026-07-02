module ControllerTests

open System
open Xunit
open Book
open BookRepository
open BookController
open CategoryController

// ============ Book Request Types ============

[<Fact>]
let ``CreateBookRequest should hold book data`` () =
    let req =
        { title = "Clean Code"
          author = "Robert C. Martin"
          genre = "Technology"
          publishedYear = 2008
          totalCopies = 5 }
    Assert.Equal("Clean Code", req.title)
    Assert.Equal(5, req.totalCopies)

[<Fact>]
let ``UpdateBookRequest fields default to None`` () =
    let req =
        { title = Some "New Title"
          author = None
          genre = None
          publishedYear = None
          totalCopies = None
          availableCopies = None }
    Assert.Equal(Some "New Title", req.title)
    Assert.Equal(None, req.author)
    Assert.Equal(None, req.availableCopies)

[<Fact>]
let ``Option.defaultValue works correctly for update`` () =
    let existing = "old"
    let updated = Some "new" |> Option.defaultValue existing
    Assert.Equal("new", updated)

[<Fact>]
let ``Option.defaultValue keeps original when None`` () =
    let existing = "old"
    let updated = None |> Option.defaultValue existing
    Assert.Equal("old", updated)

// ============ Category Request Types ============

[<Fact>]
let ``CreateCategoryRequest should hold category data`` () =
    let req =
        { name = "Fiction"
          description = "Imaginative works" }
    Assert.Equal("Fiction", req.name)
    Assert.Equal("Imaginative works", req.description)

[<Fact>]
let ``UpdateCategoryRequest fields default to None`` () =
    let req =
        { name = Some "New Name"
          description = None }
    Assert.Equal(Some "New Name", req.name)
    Assert.Equal(None, req.description)

// ============ Search Request Type ============

[<Fact>]
let ``SearchRequest should hold query and top`` () =
    let req =
        { query = "clean code"
          top = Some 10 }
    Assert.Equal("clean code", req.query)
    Assert.Equal(Some 10, req.top)

[<Fact>]
let ``SearchRequest top defaults to None`` () =
    let req =
        { query = "test"
          top = None }
    Assert.Equal(None, req.top)
