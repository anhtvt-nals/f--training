module JsonAsync

open System
open System.IO
open System.Threading.Tasks
open Microsoft.FSharp.Control

let firstTask () = task {
    printfn "Test Doing..."
    do! Task.Delay(1000)
    printfn "Test Done!!!"
}

let firstTaskAsync () = async {
    printfn "Test Async Doing..."
    do! Async.Sleep(1000)
    printfn "Test Async Done!!!"
}

let getFileTask (path: string) = task {
    printfn "Task reading file %s ..." path
    do! Task.Delay(1500)
    let! content = File.ReadAllLinesAsync(path)
    printfn "Task read file %s done !" path
    return content
}

let getFileAsync (path: string) = async {
    printfn "Async reading file %s ..." path
    do! Async.Sleep(1500)
    let! content = File.ReadAllLinesAsync(path) |> Async.AwaitTask
    printfn "Async read file %s done !" path
    return content 
}