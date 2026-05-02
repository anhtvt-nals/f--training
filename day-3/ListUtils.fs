module ListUtils

let doubleAll list =
    list
    |> List.map (fun x -> x * 2)

let filterEven list = 
    list
    |> List.filter (fun x -> x % 2 = 0)

let sumList list = 
    list
    |> List.fold (fun a b -> a + b) 0