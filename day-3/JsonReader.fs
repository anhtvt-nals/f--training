module JsonReader

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization

let loadJson path =
    try
        File.ReadAllText(path)
    with
    | :? FileNotFoundException ->
        printfn "File not found!"
        ""

type User = {
    Id: int
    Name: string
    Age: int
}

let parseUser (json: string) : Result<User list,string> = 
    let options = JsonSerializerOptions()
    options.PropertyNameCaseInsensitive <- true
    try
        let data = JsonSerializer.Deserialize<User list> (json, options)
        Ok data
    with
    | :? JsonException -> 
        Error "Invalid JSON format"

let ageLower20 (users: Result<User list,string>) : Result<User list,string> = 
    match users with
    | Ok users -> 
        users
        |> List.filter (fun x -> x.Age <= 20)
        |> Ok
    | Error e ->
        Error e