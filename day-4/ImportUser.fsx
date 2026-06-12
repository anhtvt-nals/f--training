#r "nuget:FSharp.Control.AsyncSeq,4.15.0"
#load "BulkImportUser.fs"

open System
open System.IO
open BulkImportUser

let run () =
    let sourcePath = "users.txt"
    let jsonPath = "users.json"
    let errorPath = "errors.txt"

    importUsers 5 sourcePath jsonPath errorPath
    |> Async.RunSynchronously
    |> printfn "Import result: %A"

run()