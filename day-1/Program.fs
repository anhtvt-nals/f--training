// For more information see https://aka.ms/fsharp-console-apps
printfn "Hello from F#"

// define variable
let numbers = [1..5] // List number 1 to 5

printfn "Value of list: %A" numbers

// Function find even number with Option 
let evenNumber list=
    list
    |> List.tryFind (fun x -> x % 2 = 0)

let e = evenNumber numbers
match e with
| Some (e) -> printfn "Found: %d" e
| None -> printfn "NotFound"


// Find with default value
let evenNumber2 list =
    list
    |> List.tryFind (fun x -> x > 6)
    |> Option.defaultValue -1

printfn "event number 2: %d" (evenNumber2 numbers)

// Transform all element on array to multi with 2
let listAfterMultiWith2 = numbers |> List.map (fun x -> x * 2)
printfn "List after map: %A" listAfterMultiWith2

// Filter with get list even number, after that, sum it
let checkEvenNumber a = a % 2 = 0
let result2 =
    numbers
    |> List.filter(checkEvenNumber)
    |> List.fold (fun a b -> a + b) 0 

printfn "Total even number from list: %d" result2

// Define array
let arr = [|1..5|]

// Array map
let arrMultiWith2 list = 
    list
    |> Array.map (fun x -> x * 2)

printfn "Array after multi with 2: %A" (arrMultiWith2 arr)

// Array filter
let array2 = arr |> Array.filter (fun x -> x % 2 = 0)

printfn "Array after filter: %A" array2

// Define Seq 
let seq = seq { 1 .. 100 }  

// Seq map
let seq2 = seq |> Seq.map (fun x -> x * 2)
printfn "Seq after map: %A" seq2