module Users

open System.IO
open System.Text.Json
open System.Text.Json.Serialization

type User = {
    Id: int
    Name: string
    Age: int
    Email: string
}

type Repo = {
    Read: string -> User list
    Write: string -> User list -> unit
}

let options = JsonSerializerOptions()
options.PropertyNameCaseInsensitive <- true

let userRepo = 
    {
        Read = fun path -> 
            let json = File.ReadAllText(path)
            let users = JsonSerializer.Deserialize<User list>(json, options)
            users

        Write = fun path users ->
            let json = JsonSerializer.Serialize(users, options)
            File.WriteAllText(path, json)
    }

let path = "users.json"


let readUsers (repo: Repo) path =
    repo.Read path

let findUserById (userRepo: Repo) id = 
    let users = readUsers userRepo "users.json"
    users |> List.tryFind(fun x -> x.Id = id)

let addUser (userRepo: Repo) (user: User) = 
    let users: User list = readUsers userRepo "users.json"
    let updatedUsers = users @ [user]
    userRepo.Write "users.json" updatedUsers

let updateUser (userRepo: Repo) (user: User) : Result<bool, string>  = 
    let users: User list = readUsers userRepo "users.json"
    let existUser = 
        users
        |> List.tryFind(fun x -> x.Id = user.Id)
    match existUser with
    | None -> 
        printfn "User with ID %d not found" user.Id
        Error("User not found")
    | Some _ ->
        let updatedUsers = 
            users
            |> List.map (fun x -> if x.Id = user.Id then user else x)
        userRepo.Write "users.json" updatedUsers
        Ok(true)

let deleteUser (userRepo: Repo) id : Result<bool, string> =
    let users: User list = readUsers userRepo "users.json"
    let existUser =
        users
        |> List.tryFind(fun x -> x.Id = id)
    match existUser with
    | None -> 
        printfn "User with ID %d not found" id
        Error("User not found")
    | Some _ ->
        let updatedUsers = 
            users
            |> List.filter (fun x -> x.Id <> id)
        userRepo.Write "users.json" updatedUsers
        Ok(true)