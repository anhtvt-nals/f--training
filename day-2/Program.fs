// if / elif / else
let classify n = 
    if n < 0 then "Negative"
    elif n = 0 then "Zero"
    else "Positive"

printfn "Classify of -1: %s" (classify -1)
printfn "Classify of 0: %s" (classify 0)
printfn "Classify of 2: %s" (classify 2)


// Math -> with
let classify2 n =
    match n with
    | 1 -> "One"
    | 2 -> "Two"
    | _ -> "Other"

printfn "Classify match 1: %s" (classify2 1)
printfn "Classify match 2: %s" (classify2 2)
printfn "Classify match 3: %s" (classify2 3)

// Math -> with + condition
let classifyWithCondition n = 
    match n with
    | s when s >= 4 -> "High"
    | s when s >= 2 -> "Medium"
    | _ -> "Low"

printfn "Classify match conditon 4: %s" (classifyWithCondition 4)
printfn "Classify match conditon 2: %s" (classifyWithCondition 2)
printfn "Classify match conditon 1: %s" (classifyWithCondition 1)

// Math with list
let numbers = [1..5]
let checkList list =
    match list with
    | [] -> "Empty"
    | [_] -> "One item"
    | _ -> "Many"
printfn "List with match []: %s" (checkList [])
printfn "List with match [1]: %s" (checkList [1])
printfn "List with match [1..5]: %s" (checkList [1..5])

// Match with tupe
let checkPoint (x, y) =
    match (x, y) with
    | (0, 0) -> "Origin"
    | (x, 0) -> "X axis"
    | (0, y) -> "Y axis"
    | _ -> "Other"

printfn "Checkpoint match 0,0: %s" (checkPoint(0, 0))
printfn "Checkpoin match 1,0: %s" (checkPoint(1, 0))
printfn "Checkpoin match 0,1: %s" (checkPoint(0, 1))
printfn "Checkpoin match 1,1: %s" (checkPoint(1, 1))

// Math with option
let findNum (n, list) =
 list
 |> List.tryFind (fun x -> x = n)

let show opt = 
    match opt with
    | Some opt -> sprintf "Value %d" opt
    | None -> "Invalid"

let num1 = findNum (1, numbers)
let num2 = findNum (6, numbers)
printfn "Found Num 1: %s" (show num1)
printfn "Found Num 6: %s" (show num2)

// Active Pattern
let (|Even|Odd|) n = 
    if n % 2 = 0 then Even else Odd

let testEven n = 
    match n with
    | Even -> "EVEN NUMBER"
    | Odd -> "ODD NUMBER"
printfn "test active pattern 2: %s" (testEven 2)
printfn "test active pattern 7: %s" (testEven 7)

let (|Small|Medium|Large|) n = 
    if n >= 10 then Large
    elif n >= 5 then Medium
    else Small

let testNumber n =
    match n with
    | Small -> "Number Small"
    | Medium -> "Number Medium"
    | Large -> "Number Large"

printfn "test active pattern 11: %s" (testNumber 11)
printfn "test active pattern 6: %s" (testNumber 6)
printfn "test active pattern 2: %s" (testNumber 2)
