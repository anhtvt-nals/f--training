module BulkImportUser.Tests

open System
open System.IO
open Xunit
open FsUnit.Xunit
open BulkImportUser

let createTempFile (content:string) =
    let path = Path.GetTempFileName()
    File.WriteAllText(path, content)
    path

[<Fact>]
let ``importUsers imports valid users`` () =

    let txtPath =
        createTempFile
            """1|Alice|alice@example.com|30
2|Bob|bob@example.com|25"""

    let jsonPath = Path.GetTempFileName()
    let errorPath = Path.GetTempFileName()

    let result =
        importUsers 5 txtPath jsonPath errorPath
        |> Async.RunSynchronously

    result.SuccessCount |> should equal 2
    result.FailedCount |> should equal 0

    File.Exists(jsonPath) |> should equal true

[<Fact>]
let ``importUsers logs invalid rows`` () =

    let txtPath =
        createTempFile
            """1|Alice|alice@example.com|30
bad-id|Bob|bob@example.com|20
3|Charlie"""

    let jsonPath = Path.GetTempFileName()
    let errorPath = Path.GetTempFileName()

    let result =
        importUsers 5 txtPath jsonPath errorPath
        |> Async.RunSynchronously

    result.SuccessCount |> should equal 1
    result.FailedCount |> should equal 2

    let errors =
        File.ReadAllLines(errorPath)

    errors.Length |> should equal 2

[<Fact>]
let ``importUsers creates json with valid users only`` () =

    let txtPath =
        createTempFile
            """1|Alice|alice@example.com|30
invalid
2|Bob|bob@example.com|25"""

    let jsonPath = Path.GetTempFileName()
    let errorPath = Path.GetTempFileName()

    importUsers 5 txtPath jsonPath errorPath
    |> Async.RunSynchronously
    |> ignore

    let json =
        File.ReadAllText(jsonPath)

    json.Contains("Alice") |> should equal true
    json.Contains("Bob") |> should equal true

[<Fact>]
let ``importUsers logs invalid age rows`` () =

    let txtPath =
        createTempFile
            """1|Alice|alice@example.com|30
2|Bob|bob@example.com|x
3|Charlie|charlie@example.com|28"""

    let jsonPath = Path.GetTempFileName()
    let errorPath = Path.GetTempFileName()

    let result =
        importUsers 5 txtPath jsonPath errorPath
        |> Async.RunSynchronously

    result.SuccessCount |> should equal 2
    result.FailedCount |> should equal 1

    let errors =
        File.ReadAllLines(errorPath)

    errors.Length |> should equal 1
    errors.[0].Contains("Invalid age") |> should equal true

[<Fact>]
let ``importUsers handles empty source file`` () =

    let txtPath =
        createTempFile ""

    let jsonPath = Path.GetTempFileName()
    let errorPath = Path.GetTempFileName()

    let result =
        importUsers 5 txtPath jsonPath errorPath
        |> Async.RunSynchronously

    result.SuccessCount |> should equal 0
    result.FailedCount |> should equal 0

    File.ReadAllText(jsonPath).Contains("[]") |> should equal true
    File.ReadAllText(errorPath) |> should equal ""