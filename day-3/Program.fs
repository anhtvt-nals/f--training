// For more information see https://aka.ms/fsharp-console-apps
open Number
open ListUtils
open JsonReader
open JsonAsync

// Test Number Module
printfn "1 is Odd: %b" (isOdd(1))
printfn "2 is Odd: %b" (isOdd(2))

printfn "1 is Even: %b" (isEven(1))
printfn "2 is Even: %b" (isEven(2))

printfn "Classify of -1 is: %s" (classify(-1))
printfn "Classify of 1 is: %s" (classify(1))
printfn "Classify of 0 is: %s" (classify(0))

// Text ListUtils Module
let numbers = [1..5]

printfn "Array Double: %A" (doubleAll numbers)
printfn "Array Filter Get Even: %A" (filterEven numbers)
printfn "Array Sum All: %d" (sumList numbers)


// Read file
let json = loadJson("users.json")
let getUsers path = 
    path
    |> loadJson
    |> parseUser

printfn "List users: %A" (getUsers "users.json" )

let getUsersLower20 (path: string): Result<User list, string> = 
    path
    |> loadJson
    |> parseUser
    |> ageLower20

let result20 = getUsersLower20("users.json");
match result20 with
    |Ok users -> printfn "List users age lower 20: %A" users
    |Error e -> printfn "Error: %s" e

// Async and Task function

// First function Task
firstTask().Wait()

// First function Assync
firstTaskAsync() |> Async.RunSynchronously

// Read file with task
let file1 = getFileTask("note.txt").Result
printfn "File content with task: %A" file1

// Read file with async
let file2 = getFileAsync("note.txt") |> Async.RunSynchronously
printfn "File content with async: %A" file2


// Run 2 async Parallel
let async1 = getFileAsync("note.txt")
let async2 = getFileAsync("note2.txt")
let runAsync =
    [async1; async2]
    |> Async.Parallel
let resultAsync = runAsync |> Async.RunSynchronously
printfn "Run Parallel Async Result: %A" resultAsync