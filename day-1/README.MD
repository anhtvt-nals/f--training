# 📘 Học F# Cơ Bản — Collections & Option Type

Bài tập này giới thiệu các khái niệm cơ bản trong F# thông qua việc làm việc với **List**, **Array**, **Seq** và **Option type**.

---

## 🗂️ Nội Dung Bài Tập

### 1. List (Danh sách)

F# cung cấp kiểu `List` là immutable (bất biến), hỗ trợ nhiều hàm xử lý dữ liệu theo phong cách lập trình hàm.

```fsharp
let numbers = [1..5]
```

#### Tìm phần tử với `Option`

Sử dụng `List.tryFind` để tìm phần tử đầu tiên thỏa điều kiện. Kết quả trả về kiểu `Option` (`Some` hoặc `None`), tránh lỗi khi không tìm thấy.

```fsharp
let evenNumber list =
    list |> List.tryFind (fun x -> x % 2 = 0)

match evenNumber numbers with
| Some e -> printfn "Found: %d" e
| None   -> printfn "NotFound"
```

#### Giá trị mặc định với `Option.defaultValue`

Nếu không tìm thấy phần tử, trả về giá trị mặc định thay vì `None`.

```fsharp
let evenNumber2 list =
    list
    |> List.tryFind (fun x -> x > 6)
    |> Option.defaultValue -1
```

#### Map — Biến đổi từng phần tử

```fsharp
let listAfterMultiWith2 = numbers |> List.map (fun x -> x * 2)
// Kết quả: [2; 4; 6; 8; 10]
```

#### Filter + Fold — Lọc và tính tổng

```fsharp
let checkEvenNumber a = a % 2 = 0

let result2 =
    numbers
    |> List.filter checkEvenNumber
    |> List.fold (fun acc b -> acc + b) 0
// Lọc số chẵn [2; 4] rồi cộng lại → 6
```

---

### 2. Array (Mảng)

`Array` trong F# có cú pháp `[| ... |]` và hỗ trợ các hàm tương tự `List` nhưng có thể thay đổi giá trị (mutable).

```fsharp
let arr = [|1..5|]
```

#### Array.map

```fsharp
let arrMultiWith2 list =
    list |> Array.map (fun x -> x * 2)
// Kết quả: [|2; 4; 6; 8; 10|]
```

#### Array.filter

```fsharp
let array2 = arr |> Array.filter (fun x -> x % 2 = 0)
// Kết quả: [|2; 4|]
```

---

### 3. Seq (Chuỗi / Sequence)

`Seq` là **lazy collection** — dữ liệu chỉ được tính toán khi cần. Phù hợp cho tập dữ liệu lớn hoặc vô hạn.

```fsharp
let mySeq = seq { 1 .. 100 }

let seq2 = mySeq |> Seq.map (fun x -> x * 2)
```

---

## 🔑 Các Khái Niệm Chính

| Khái niệm | Mô tả |
|---|---|
| `Option` | Kiểu an toàn thay thế `null`, có hai trường hợp: `Some value` và `None` |
| `\|>` (pipe) | Toán tử truyền dữ liệu qua chuỗi hàm, giúp code dễ đọc |
| `List.map` | Biến đổi từng phần tử trong danh sách |
| `List.filter` | Lọc phần tử theo điều kiện |
| `List.fold` | Gộp danh sách thành một giá trị (tương tự `reduce`) |
| `List` vs `Array` | List là immutable, Array là mutable và truy cập theo index nhanh hơn |
| `Seq` | Lazy evaluation, chỉ tính khi cần — hiệu quả với dữ liệu lớn |

---

## ▶️ Chạy Chương Trình

```bash
dotnet run
```

**Kết quả mẫu:**

```
Hello from F#
Value of list: [1; 2; 3; 4; 5]
Found: 2
event number 2: -1
List after map: [2; 4; 6; 8; 10]
Total even number from list: 6
Array after multi with 2: [|2; 4; 6; 8; 10|]
Array after filter: [|2; 4|]
Seq after map: seq [2; 4; 6; 8; ...]
```

---

## 📚 Tài Liệu Tham Khảo

- [F# Official Docs](https://learn.microsoft.com/en-us/dotnet/fsharp/)
- [F# for Fun and Profit](https://fsharpforfunandprofit.com/)
- [Tour of F#](https://learn.microsoft.com/en-us/dotnet/fsharp/tour)