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
          categoryId = "cat_tech001"
          categoryName = "Technology"
          publishedYear = 2008
          totalCopies = 5 }
    Assert.Equal("Clean Code", req.title)
    Assert.Equal("cat_tech001", req.categoryId)
    Assert.Equal("Technology", req.categoryName)
    Assert.Equal(5, req.totalCopies)

[<Fact>]
let ``UpdateBookRequest fields default to None`` () =
    let req =
        { title = Some "New Title"
          author = None
          categoryId = None
          categoryName = None
          publishedYear = None
          totalCopies = None
          availableCopies = None }
    Assert.Equal(Some "New Title", req.title)
    Assert.Equal(None, req.author)
    Assert.Equal(None, req.categoryId)
    Assert.Equal(None, req.categoryName)
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
    let req: CreateCategoryRequest =
        { name = "Fiction"
          description = "Imaginative works" }
    Assert.Equal("Fiction", req.name)
    Assert.Equal("Imaginative works", req.description)

[<Fact>]
let ``UpdateCategoryRequest fields default to None`` () =
    let req =
        { name = Some "New Name"
          description = None }
    Assert.Equal("New Name", req.name.Value)
    Assert.True(req.description.IsNone)

// ============ Search Request Type ============

[<Fact>]
let ``SearchRequest should hold query and top`` () =
    let req =
        { query = "clean code"
          top = Some 10
          categoryId = None
          minYear = None
          maxYear = None
          author = None }
    Assert.Equal("clean code", req.query)
    Assert.Equal(Some 10, req.top)

[<Fact>]
let ``SearchRequest top defaults to None`` () =
    let req =
        { query = "test"
          top = None
          categoryId = None
          minYear = None
          maxYear = None
          author = None }
    Assert.Equal(None, req.top)
