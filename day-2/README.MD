# 📘 Học F# — Điều Kiện & Pattern Matching

Bài tập này khám phá các cách kiểm tra và phân nhánh logic trong F#, từ `if/elif/else` cơ bản đến **Pattern Matching** nâng cao với **Active Patterns**.

---

## 🗂️ Nội Dung Bài Tập

### 1. `if / elif / else` — Rẽ nhánh cơ bản

Cú pháp quen thuộc để phân loại theo điều kiện. Trong F#, `if` là **biểu thức** (expression) trả về giá trị, không phải câu lệnh (statement).

```fsharp
let classify n = 
    if n < 0 then "Negative"
    elif n = 0 then "Zero"
    else "Positive"
```

---

### 2. `match ... with` — Pattern Matching cơ bản

Thay thế `switch/case` nhưng mạnh hơn nhiều. `_` là wildcard — khớp với mọi giá trị còn lại.

```fsharp
let classify2 n =
    match n with
    | 1 -> "One"
    | 2 -> "Two"
    | _ -> "Other"
```

---

### 3. Match với điều kiện (`when` guard)

Kết hợp pattern matching với điều kiện bổ sung bằng từ khóa `when`.

```fsharp
let classifyWithCondition n = 
    match n with
    | s when s >= 4 -> "High"
    | s when s >= 2 -> "Medium"
    | _ -> "Low"
```

---

### 4. Match với List

F# cho phép match trực tiếp trên cấu trúc danh sách — rỗng, một phần tử, hoặc nhiều phần tử.

```fsharp
let checkList list =
    match list with
    | []  -> "Empty"
    | [_] -> "One item"
    | _   -> "Many"
```

| Input | Kết quả |
|---|---|
| `[]` | `"Empty"` |
| `[1]` | `"One item"` |
| `[1..5]` | `"Many"` |

---

### 5. Match với Tuple

Match đồng thời nhiều giá trị bằng cách gom vào tuple.

```fsharp
let checkPoint (x, y) =
    match (x, y) with
    | (0, 0) -> "Origin"
    | (x, 0) -> "X axis"
    | (0, y) -> "Y axis"
    | _       -> "Other"
```

---

### 6. Match với `Option`

Kết hợp `List.tryFind` trả về `Option` và dùng `match` để xử lý an toàn, tránh lỗi null.

```fsharp
let findNum (n, list) =
    list |> List.tryFind (fun x -> x = n)

let show opt = 
    match opt with
    | Some v -> sprintf "Value %d" v
    | None   -> "Invalid"
```

---

### 7. Active Patterns — Pattern tự định nghĩa

Active Patterns cho phép **đặt tên cho các điều kiện** và dùng chúng như pattern thông thường trong `match`. Đây là tính năng đặc trưng và mạnh mẽ của F#.

#### Partial Active Pattern (2 nhánh)

```fsharp
let (|Even|Odd|) n = 
    if n % 2 = 0 then Even else Odd

let testEven n = 
    match n with
    | Even -> "EVEN NUMBER"
    | Odd  -> "ODD NUMBER"
```

#### Multi-case Active Pattern (3 nhánh)

```fsharp
let (|Small|Medium|Large|) n = 
    if n >= 10 then Large
    elif n >= 5 then Medium
    else Small

let testNumber n =
    match n with
    | Small  -> "Number Small"
    | Medium -> "Number Medium"
    | Large  -> "Number Large"
```

---

## 🔑 Tổng Kết Các Khái Niệm

| Khái niệm | Mô tả |
|---|---|
| `if / elif / else` | Rẽ nhánh cơ bản, là expression trả về giá trị |
| `match ... with` | Pattern matching, thay thế switch/case linh hoạt hơn |
| `when` guard | Thêm điều kiện vào từng nhánh match |
| List pattern `[]`, `[_]` | Match theo cấu trúc danh sách |
| Tuple pattern `(x, y)` | Match nhiều giá trị cùng lúc |
| `Option` match | Xử lý `Some` / `None` an toàn, không null |
| Active Pattern `(\|A\|B\|)` | Định nghĩa pattern có tên để tái sử dụng |

---

## ▶️ Chạy Chương Trình

```bash
dotnet run
```

**Kết quả mẫu:**

```
Classify of -1: Negative
Classify of 0: Zero
Classify of 2: Positive
Classify match 1: One
Classify match 2: Two
Classify match 3: Other
Classify match conditon 4: High
Classify match conditon 2: Medium
Classify match conditon 1: Low
List with match []: Empty
List with match [1]: One item
List with match [1..5]: Many
Checkpoint match 0,0: Origin
Checkpoin match 1,0: X axis
Checkpoin match 0,1: Y axis
Checkpoin match 1,1: Other
Found Num 1: Value 1
Found Num 6: Invalid
test active pattern 2: EVEN NUMBER
test active pattern 7: ODD NUMBER
test active pattern 11: Number Large
test active pattern 6: Number Medium
test active pattern 2: Number Small
```

---

## 📚 Tài Liệu Tham Khảo

- [Pattern Matching in F#](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/pattern-matching)
- [Active Patterns](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/active-patterns)
- [F# for Fun and Profit — Match Expressions](https://fsharpforfunandprofit.com/posts/match-expression/)