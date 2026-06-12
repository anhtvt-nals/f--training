module BulkImportUser

open System
open System.IO
open System.Text.Json
open FSharp.Control

type User =
    {
        Id : int
        Name : string
        Email : string
        Age : int
    }

type ImportResult =
    {
        SuccessCount : int
        FailedCount : int
    }

let parseLine (line:string) =
    async {
        do! Async.Sleep 50

        let parts = line.Split('|')

        if parts.Length <> 4 then
            return Error $"Invalid format: {line}"
        else
            match Int32.TryParse(parts.[0]) with
            | false, _ ->
                return Error $"Invalid id: {line}"
            | true, id ->
                match Int32.TryParse(parts.[3].Trim()) with
                | false, _ ->
                    return Error $"Invalid age: {line}"
                | true, age ->
                    return
                        Ok {
                            Id = id
                            Name = parts.[1].Trim()
                            Email = parts.[2].Trim()
                            Age = age
                        }
    }

let readLines path =
    File.ReadAllLinesAsync(path)
    |> Async.AwaitTask

let writeJson path users =
    async {
        let options =
            JsonSerializerOptions(WriteIndented = true)

        let json =
            JsonSerializer.Serialize(users, options)

        do!
            File.WriteAllTextAsync(path, json)
            |> Async.AwaitTask
    }

let writeErrors path errors =
    async {
        if not (List.isEmpty errors) then
            do!
                File.WriteAllLinesAsync(path, errors)
                |> Async.AwaitTask
    }

let importUsers
    (concurency:int)
    (sourcePath:string)
    (jsonPath:string)
    (errorPath:string) =

    async {

        let! lines = readLines sourcePath
        let users = ResizeArray<User>() // ResizeArray cho phép thêm phần tử một cách hiệu quả mà không cần phải tạo lại danh sách mới mỗi lần
        let errors = ResizeArray<string>() // ResizeArray cho phép thêm phần tử một cách hiệu quả mà không cần phải tạo lại danh sách mới mỗi lần

        do!
            lines
            |> AsyncSeq.ofArray
            |> AsyncSeq.iterAsyncParallelThrottled concurency (fun line ->
                async {
                    let! result = parseLine line

                    match result with
                    | Ok user ->
                        // lock: Tránh xung đột khi thêm người dùng vào danh sách trên nhiều luồng
                        lock users (fun () -> 
                            users.Add user)

                    | Error err ->
                        // lock: Tránh xung đột khi thêm lỗi vào danh sách trên nhiều luồng
                        lock errors (fun () ->
                            errors.Add err)
                })

        let users = List.ofSeq users // Chuyển đổi ResizeArray sang List để dễ dàng sử dụng sau này
        let errors = List.ofSeq errors // Chuyển đổi ResizeArray sang List để dễ dàng sử dụng sau này

        do! writeJson jsonPath users
        do! writeErrors errorPath errors

        return {
            SuccessCount = users.Length
            FailedCount = errors.Length
        }
    }