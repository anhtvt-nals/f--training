module BookModelsValidationTests

open System
open Xunit
open Book
open Book.Validation

// ============ CreateBookRequest Validation Tests ============

[<Fact>]
let ``Valid CreateBookRequest should pass validation`` () =
    let req =
        { title = "Clean Code"
          author = "Robert C. Martin"
          categoryId = "cat_tech001"
          categoryName = "Technology"
          publishedYear = 2008
          totalCopies = 10 }
    
    match validateCreateBookRequest req with
    | Valid _ -> Assert.True(true)
    | Invalid errors -> Assert.True(false, $"Should be valid but got errors: {errors}")

[<Fact>]
let ``CreateBookRequest with empty title should fail`` () =
    let req =
        { title = ""
          author = "Robert C. Martin"
          categoryId = "cat_tech001"
          categoryName = "Technology"
          publishedYear = 2008
          totalCopies = 10 }
    
    match validateCreateBookRequest req with
    | Valid _ -> Assert.True(false, "Should have failed validation")
    | Invalid errors ->
        Assert.True(errors |> List.exists (fun e -> e.field = "title"))

[<Fact>]
let ``CreateBookRequest with empty author should fail`` () =
    let req =
        { title = "Clean Code"
          author = ""
          categoryId = "cat_tech001"
          categoryName = "Technology"
          publishedYear = 2008
          totalCopies = 10 }
    
    match validateCreateBookRequest req with
    | Invalid errors ->
        Assert.True(errors |> List.exists (fun e -> e.field = "author"))
    | Valid _ -> Assert.True(false, "Should have failed")

[<Fact>]
let ``CreateBookRequest with title too long should fail`` () =
    let longTitle = String.replicate 250 "a"
    let req =
        { title = longTitle
          author = "Author"
          categoryId = "cat_001"
          categoryName = "Category"
          publishedYear = 2020
          totalCopies = 5 }
    
    match validateCreateBookRequest req with
    | Invalid errors ->
        Assert.True(errors |> List.exists (fun e -> e.field = "title" && e.message.Contains("200")))
    | Valid _ -> Assert.True(false, "Should have failed")

[<Fact>]
let ``CreateBookRequest with invalid year should fail`` () =
    let req =
        { title = "Test"
          author = "Author"
          categoryId = "cat_001"
          categoryName = "Category"
          publishedYear = 3000
          totalCopies = 5 }
    
    match validateCreateBookRequest req with
    | Invalid errors ->
        Assert.True(errors |> List.exists (fun e -> e.field = "publishedYear"))
    | Valid _ -> Assert.True(false, "Should have failed")

[<Fact>]
let ``CreateBookRequest with negative totalCopies should fail`` () =
    let req =
        { title = "Test"
          author = "Author"
          categoryId = "cat_001"
          categoryName = "Category"
          publishedYear = 2020
          totalCopies = -5 }
    
    match validateCreateBookRequest req with
    | Invalid errors ->
        Assert.True(errors |> List.exists (fun e -> e.field = "totalCopies"))
    | Valid _ -> Assert.True(false, "Should have failed")

[<Fact>]
let ``CreateBookRequest with multiple errors should return all errors`` () =
    let req =
        { title = ""
          author = ""
          categoryId = ""
          categoryName = ""
          publishedYear = 3000
          totalCopies = -5 }
    
    match validateCreateBookRequest req with
    | Invalid errors ->
        Assert.True(errors.Length >= 4, $"Should have multiple errors, got {errors.Length}")
        Assert.True(errors |> List.exists (fun e -> e.field = "title"))
        Assert.True(errors |> List.exists (fun e -> e.field = "author"))
        Assert.True(errors |> List.exists (fun e -> e.field = "publishedYear"))
        Assert.True(errors |> List.exists (fun e -> e.field = "totalCopies"))
    | Valid _ -> Assert.True(false, "Should have failed")

// ============ UpdateBookRequest Validation Tests ============

[<Fact>]
let ``Valid UpdateBookRequest should pass validation`` () =
    let req =
        { title = Some "New Title"
          author = Some "New Author"
          categoryId = None
          categoryName = None
          publishedYear = Some 2020
          totalCopies = Some 15
          availableCopies = Some 10 }
    
    match validateUpdateBookRequest req with
    | Valid _ -> Assert.True(true)
    | Invalid errors -> Assert.True(false, $"Should be valid but got errors: {errors}")

[<Fact>]
let ``UpdateBookRequest with all None should pass`` () =
    let req =
        { title = None
          author = None
          categoryId = None
          categoryName = None
          publishedYear = None
          totalCopies = None
          availableCopies = None }
    
    match validateUpdateBookRequest req with
    | Valid _ -> Assert.True(true)
    | Invalid _ -> Assert.True(false, "Empty update should be valid")

[<Fact>]
let ``UpdateBookRequest with empty title should fail`` () =
    let req =
        { title = Some ""
          author = None
          categoryId = None
          categoryName = None
          publishedYear = None
          totalCopies = None
          availableCopies = None }
    
    match validateUpdateBookRequest req with
    | Invalid errors ->
        Assert.True(errors |> List.exists (fun e -> e.field = "title"))
    | Valid _ -> Assert.True(false, "Should have failed")

[<Fact>]
let ``UpdateBookRequest with negative availableCopies should fail`` () =
    let req =
        { title = None
          author = None
          categoryId = None
          categoryName = None
          publishedYear = None
          totalCopies = None
          availableCopies = Some -5 }
    
    match validateUpdateBookRequest req with
    | Invalid errors ->
        Assert.True(errors |> List.exists (fun e -> e.field = "availableCopies"))
    | Valid _ -> Assert.True(false, "Should have failed")

// ============ SearchRequest Validation Tests ============

[<Fact>]
let ``Valid SearchRequest should pass validation`` () =
    let req =
        { query = "clean code"
          top = Some 20
          categoryId = None
          minYear = None
          maxYear = None
          author = None }
    
    match validateSearchRequest req with
    | Valid _ -> Assert.True(true)
    | Invalid errors -> Assert.True(false, $"Should be valid but got errors: {errors}")

[<Fact>]
let ``SearchRequest with empty query should fail`` () =
    let req =
        { query = ""
          top = None
          categoryId = None
          minYear = None
          maxYear = None
          author = None }
    
    match validateSearchRequest req with
    | Invalid errors ->
        Assert.True(errors |> List.exists (fun e -> e.field = "query"))
    | Valid _ -> Assert.True(false, "Should have failed")

[<Fact>]
let ``SearchRequest with top out of range should fail`` () =
    let req1 =
        { query = "test"
          top = Some 0
          categoryId = None
          minYear = None
          maxYear = None
          author = None }
    
    match validateSearchRequest req1 with
    | Invalid errors ->
        Assert.True(errors |> List.exists (fun e -> e.field = "top"))
    | Valid _ -> Assert.True(false, "Should have failed")
    
    let req2 =
        { query = "test"
          top = Some 150
          categoryId = None
          minYear = None
          maxYear = None
          author = None }
    
    match validateSearchRequest req2 with
    | Invalid errors ->
        Assert.True(errors |> List.exists (fun e -> e.field = "top"))
    | Valid _ -> Assert.True(false, "Should have failed")

[<Fact>]
let ``SearchRequest with filters should pass validation`` () =
    let req =
        { query = "programming"
          top = Some 50
          categoryId = Some "cat_tech001"
          minYear = Some 2000
          maxYear = Some 2020
          author = Some "Martin Fowler" }
    
    match validateSearchRequest req with
    | Valid _ -> Assert.True(true)
    | Invalid errors -> Assert.True(false, $"Should be valid but got errors: {errors}")

[<Fact>]
let ``SearchRequest with invalid year range should fail`` () =
    let req =
        { query = "test"
          top = None
          categoryId = None
          minYear = Some 3000
          maxYear = None
          author = None }
    
    match validateSearchRequest req with
    | Invalid errors ->
        Assert.True(errors |> List.exists (fun e -> e.field = "minYear"))
    | Valid _ -> Assert.True(false, "Should have failed")

// ============ ValidationResult Tests ============

[<Fact>]
let ``ValidationResult Valid should contain value`` () =
    let result = Valid 42
    match result with
    | Valid v -> Assert.Equal(42, v)
    | Invalid _ -> Assert.True(false, "Should be Valid")

[<Fact>]
let ``ValidationResult Invalid should contain errors`` () =
    let errors = [{ field = "test"; message = "error" }]
    let result = Invalid errors
    match result with
    | Valid _ -> Assert.True(false, "Should be Invalid")
    | Invalid errs -> Assert.Equal(1, errs.Length)

[<Fact>]
let ``ValidationError should have field and message`` () =
    let error = { field = "title"; message = "Title is required" }
    Assert.Equal("title", error.field)
    Assert.Equal("Title is required", error.message)
