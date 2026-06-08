open System
open System.IO
open System.Threading.Tasks
open System.Text.Json
open System.Text.Json.Serialization

let options = JsonSerializerOptions()
options.PropertyNameCaseInsensitive <- true
options.WriteIndented <- true

type User = {
    Id: int
    Name: string
    Age: int
}

let readSources () = task {
    let! lines = File.ReadAllLinesAsync("users.txt")
    return lines
}

let readJson () = task {
    let! json = File.ReadAllTextAsync("users.json")
    return json
}

let updateAndInsert (id: int, name: string, age: int) = async {
    printfn "Updating and inserting: %d, %s, %d" id name age
    let! json = readJson() |> Async.AwaitTask
    let users = JsonSerializer.Deserialize<User list>(json, options)
    
    // users is a List, isNull only work with Ref or Object type so need user "box users" to convert it to object .NET to check if it's null
    let users =
        if isNull (box users) then [] else users
    let user = 
        users |> List.tryFind(fun u -> u.Id = id)

    let updatedUsers =
        match user with
        | Some u ->
            printfn "User found: %A" u
            users
                |> List.map (fun x -> if x.Id = id then { Id = id; Name = name; Age = age } else x)
        | None ->
            printfn "User not found"
            users @ [{ Id = id; Name = name; Age = age }]

    printfn "Current data: %A" updatedUsers

    let newJson = JsonSerializer.Serialize(updatedUsers, options)
    do!
        File.WriteAllTextAsync("users.json", newJson) |> Async.AwaitTask

    printfn "Done!"
}
    

let run () = 
    let! lines = readSources().Result
    printfn "Lines: %A" lines
    lines
    |> Array.map (fun x -> x.Split("|"))
    |> Array.map (fun x -> (int x.[0], x.[1], int x.[2]))
    |> Array.map (fun (id, name: string, age) -> updateAndInsert(id, name, age))
    |> Async.Parallel
    |> Async.RunSynchronously

run()
