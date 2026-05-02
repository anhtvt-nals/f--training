# 📘 Học F# — Day 3

Bài tập này tập trung vào các kỹ thuật xử lý dữ liệu, đọc file JSON và lập trình bất đồng bộ trong F#.

---

## 🗂️ Nội Dung Bài Tập

### 1. `Program.fs`

File chính chạy các ví dụ sau:
- kiểm tra `Number.fs`
- thao tác danh sách với `ListUtils.fs`
- đọc và parse JSON từ `users.json`
- lọc người dùng theo tuổi
- chạy tác vụ `Task` và `Async`
- đọc file `note.txt` và `note2.txt`
- chạy song song nhiều tác vụ `Async`

```fsharp
let getUsers path =
    path
    |> loadJson
    |> parseUser

let getUsersLower20 (path: string): Result<User list, string> =
    path
    |> loadJson
    |> parseUser
    |> ageLower20

let async1 = getFileAsync("note.txt")
let async2 = getFileAsync("note2.txt")
let runAsync =
    [async1; async2]
    |> Async.Parallel
let resultAsync = runAsync |> Async.RunSynchronously
```

---

### 2. `Number.fs`

Các hàm xử lý số cơ bản:
- `isEven` / `isOdd` kiểm tra số chẵn/lẻ
- `classify` phân loại số thành `Negative`, `Zero` hoặc `Positive`

```fsharp
let isEven x = x % 2 = 0
let isOdd x = x % 2 <> 0

let classify x =
    if x < 0 then "Negative"
    elif x = 0 then "Zero"
    else "Positive"
```

---

### 3. `ListUtils.fs`

Các hàm thao tác danh sách:
- `doubleAll` nhân đôi tất cả phần tử
- `filterEven` lọc số chẵn
- `sumList` tính tổng danh sách

```fsharp
let doubleAll list =
    list
    |> List.map (fun x -> x * 2)

let filterEven list =
    list
    |> List.filter (fun x -> x % 2 = 0)

let sumList list =
    list
    |> List.fold (fun a b -> a + b) 0
```

---

### 4. `JsonReader.fs`

Đọc JSON từ file và parse sang kiểu dữ liệu `User`:
- `loadJson` đọc nội dung file JSON
- `parseUser` deserialize JSON sang `User list`
- `ageLower20` lọc người dùng có tuổi <= 20

```fsharp
type User = {
    Id: int
    Name: string
    Age: int
}

let loadJson path =
    try
        File.ReadAllText(path)
    with
    | :? FileNotFoundException ->
        printfn "File not found!"
        ""

let parseUser (json: string) : Result<User list, string> =
    let options = JsonSerializerOptions()
    options.PropertyNameCaseInsensitive <- true
    try
        let data = JsonSerializer.Deserialize<User list> (json, options)
        Ok data
    with
    | :? JsonException ->
        Error "Invalid JSON format"

let ageLower20 (users: Result<User list, string>) : Result<User list, string> =
    match users with
    | Ok users ->
        users
        |> List.filter (fun x -> x.Age <= 20)
        |> Ok
    | Error e ->
        Error e
```

---

### 5. `JsonAsync.fs`

Thực hành lập trình bất đồng bộ bằng `Task` và `Async`:
- `firstTask` sử dụng `task`
- `firstTaskAsync` sử dụng `async`
- `getFileTask` đọc file bằng `Task`
- `getFileAsync` đọc file bằng `Async`

```fsharp
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
```

---

### 6. `users.json`

Dữ liệu mẫu chứa danh sách người dùng dùng để kiểm thử parse JSON và lọc theo tuổi.

---

## ▶️ Kết quả khi chạy chương trình

```text
1 is Odd: true
2 is Odd: false
1 is Even: false
2 is Even: true
Classify of -1 is: Negative
Classify of 1 is: Positive
Classify of 0 is: Zero
Array Double: [2; 4; 6; 8; 10]
Array Filter Get Even: [2; 4]
Array Sum All: 15
List users: Ok [{ Id = 1
      Name = "User 1"
      Age = 20 }; { Id = 2
                    Name = "User 2"
                    Age = 18 }; { Id = 3
                                  Name = "User 3"
                                  Age = 45 }; { Id = 4
                                                Name = "User 4"
                                                Age = 30 }]
List users age lower 20: [{ Id = 1
   Name = "User 1"
   Age = 20 }; { Id = 2
                 Name = "User 2"
                 Age = 18 }]
Test Doing...
Test Done!!!
Test Async Doing...
Test Async Done!!!
Task reading file note.txt ...
Task read file note.txt done !
File content with task: [|"dev 1"; "dev 2"; "dev 3"; "dev 4"; "dev 5"; "dev 6"; "dev 7"|]
Async reading file note.txt ...
Async read file note.txt done !
File content with async: [|"dev 1"; "dev 2"; "dev 3"; "dev 4"; "dev 5"; "dev 6"; "dev 7"|]
Async reading file note.txt ...
Async reading file note2.txt ...
Async read file note2.txt done !
Async read file note.txt done !
Run Parallel Async Result: [|[|"dev 1"; "dev 2"; "dev 3"; "dev 4"; "dev 5"; "dev 6"; "dev 7"|];
  [|"argent 1"; "argent 2"; "argent 3"; "argent 4"; "argent 5"; "argent 6";
    "argent 7"|]|]
```

---

## ▶️ Chạy Chương Trình

```bash
cd day-3
dotnet run
```

---

## 📚 Tài Liệu Tham Khảo

- [F# asynchronous programming](https://learn.microsoft.com/en-us/dotnet/fsharp/tutorials/asynchronous-and-parallel-programming)
- [JSON programming in F#](https://learn.microsoft.com/en-us/dotnet/fsharp/tutorials/using-json)
- [F# modules and namespaces](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/namespaces-modules)
