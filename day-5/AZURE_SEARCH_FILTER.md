# Azure AI Search — OData Filter, Complex Types & Facets (F#)

> Tài liệu tham khảo về cách lọc dữ liệu chính xác bằng OData `$filter`, phân biệt với full-text search, làm việc với Collection và ComplexType, và tổng hợp facet cho filter sidebar.

---

## 1. OData `$filter` — Các Operators

`$filter` là ngôn ngữ OData dùng để lọc documents theo điều kiện **chính xác** trước khi tính score và trả về kết quả. Chỉ documents pass filter mới được search và rank.

### 1.1 Comparison Operators

```fsharp
// eq — equal (bằng)
options.Filter <- "Category eq 'Resort'"
options.Filter <- "Rating eq 4.5"
options.Filter <- "IsAvailable eq true"
options.Filter <- "DeletedAt eq null"           // kiểm tra null

// ne — not equal (khác)
options.Filter <- "Category ne 'Motel'"
options.Filter <- "Status ne 'closed'"

// gt — greater than (lớn hơn, không bao gồm)
options.Filter <- "Rating gt 4.0"              // Rating > 4.0
options.Filter <- "PricePerNight gt 100"

// ge — greater than or equal (lớn hơn hoặc bằng)
options.Filter <- "Rating ge 4.0"              // Rating >= 4.0

// lt — less than (nhỏ hơn, không bao gồm)
options.Filter <- "PricePerNight lt 500"

// le — less than or equal (nhỏ hơn hoặc bằng)
options.Filter <- "PricePerNight le 500"       // PricePerNight <= 500

// DateTimeOffset comparison
options.Filter <- "LastUpdated gt 2024-01-01T00:00:00Z"
options.Filter <- "CheckIn ge 2024-06-01T00:00:00.000Z"
```

### 1.2 Logical Operators

```fsharp
// and — tất cả điều kiện phải đúng
options.Filter <- "Rating gt 4.0 and Category eq 'Resort'"
options.Filter <- "Rating gt 3.5 and PricePerNight lt 300 and IsAvailable eq true"

// or — ít nhất một điều kiện đúng
options.Filter <- "Category eq 'Resort' or Category eq 'Boutique'"
options.Filter <- "City eq 'Hà Nội' or City eq 'Hồ Chí Minh'"

// not — đảo ngược điều kiện
options.Filter <- "not (Category eq 'Motel')"
options.Filter <- "not (Rating lt 3.0)"

// Kết hợp phức tạp — dùng ngoặc để rõ ràng thứ tự ưu tiên
options.Filter <- "(Category eq 'Resort' or Category eq 'Boutique') and Rating gt 4.0"
options.Filter <- "IsAvailable eq true and (PricePerNight lt 200 or Rating gt 4.8)"

// not với and/or
options.Filter <- "not (Category eq 'Motel' or Category eq 'Hostel') and Rating gt 3.5"
```

### 1.3 `search.in()` — IN Operator

`search.in()` là cách hiệu quả để filter theo danh sách giá trị — thay thế cho chuỗi `or` dài.

```fsharp
// Cú pháp: search.in(field, 'value1|value2|value3', 'delimiter')
// Delimiter mặc định là '|' hoặc ','

// Thay thế cho: Category eq 'Resort' or Category eq 'Boutique' or Category eq 'Spa'
options.Filter <- "search.in(Category, 'Resort|Boutique|Spa')"

// Dùng dấu phẩy làm delimiter
options.Filter <- "search.in(Category, 'Resort, Boutique, Spa', ',')"

// search.in với khoảng trắng trong giá trị — dùng delimiter khác
options.Filter <- "search.in(City, 'Hà Nội|Hồ Chí Minh|Đà Nẵng', '|')"

// Kết hợp với các operators khác
options.Filter <- "search.in(Category, 'Resort|Boutique') and Rating gt 4.0"
options.Filter <- "not search.in(Status, 'closed|suspended')"

// Build filter động từ F# list
let buildInFilter (fieldName: string) (values: string list) (delimiter: string) =
    let valueList = values |> String.concat delimiter
    $"search.in({fieldName}, '{valueList}', '{delimiter}')"

// Dùng:
let categoryFilter = buildInFilter "Category" ["Resort"; "Boutique"; "Spa Hotel"] "|"
// → "search.in(Category, 'Resort|Boutique|Spa Hotel', '|')"
```

### 1.4 Các hàm filter khác

```fsharp
// search.ismatch() — full-text search trong filter context
// Trả về true/false, không tính score
options.Filter <- "search.ismatch('hồ bơi', 'Description') and Rating gt 4.0"
// Tìm documents có "hồ bơi" trong Description VÀ Rating > 4.0

// geo.distance() — filter theo khoảng cách địa lý
// Cần field kiểu GeographyPoint
options.Filter <- "geo.distance(Location, geography'POINT(105.8342 21.0278)') lt 5.0"
// Chỉ lấy documents trong vòng 5km từ tọa độ (Hà Nội)

// geo.intersects() — point nằm trong polygon
options.Filter <- "geo.intersects(Location, geography'POLYGON((...coordinates...))')"
```

---

## 2. Filter khác Search — Phân biệt quan trọng

Đây là điểm hay gây nhầm lẫn nhất cho người mới dùng Azure Search.

### 2.1 Bảng so sánh

| | `$filter` | Full-text Search |
|---|---|---|
| **Mục đích** | Lọc chính xác (include/exclude) | Tìm kiếm ngữ nghĩa (rank by relevance) |
| **Qua analyzer?** | ❌ **Không** — raw value | ✅ **Có** — qua tokenizer + token filters |
| **So sánh** | Exact match với stored value | Token match với indexed tokens |
| **Ảnh hưởng score?** | ❌ Không tính vào score | ✅ Là cơ sở tính score |
| **Field yêu cầu** | `IsFilterable = true` | `IsSearchable = true` |
| **Case sensitive?** | ✅ Có (mặc định) | ❌ Không (analyzer lowercase) |
| **Partial match?** | ❌ Không | ✅ Có (tùy analyzer/query) |

### 2.2 Minh họa sự khác biệt

```fsharp
// Document được index:
// { Category: "Luxury Resort", Name: "Grand Metropolis" }

// FILTER — so sánh với giá trị raw, không qua analyzer
options.Filter <- "Category eq 'Luxury Resort'"    // ✅ Match (exact)
options.Filter <- "Category eq 'luxury resort'"    // ❌ No match (case sensitive!)
options.Filter <- "Category eq 'Resort'"           // ❌ No match (partial)
options.Filter <- "Category eq 'Luxury'"           // ❌ No match (partial)

// SEARCH — qua analyzer (standard: lowercase + tokenize)
// Index stores tokens: ["luxury", "resort"] cho field Name/Description
// Query "resort"     → ✅ Match
// Query "RESORT"     → ✅ Match (lowercase qua analyzer)
// Query "lux*"       → ✅ Match (wildcard)
// Query "Luxury"     → ✅ Match
```

### 2.3 Hệ quả thực tế

```fsharp
// ✅ Đúng: dùng filter cho categorical/exact values
options.Filter <- "Status eq 'active'"
options.Filter <- "search.in(Category, 'Resort|Boutique')"
options.Filter <- "Rating gt 4.0"

// ⚠️  Sai pattern: dùng filter để tìm text trong Description
// options.Filter <- "Description eq 'has swimming pool'"  ← KHÔNG BAO GIỜ match

// ✅ Đúng: dùng search cho text content
let query = "swimming pool"   // → qua analyzer → match "pool", "pools", "swimming"

// ✅ Kết hợp: search (relevance) + filter (constraint)
let options = SearchOptions()
options.Filter      <- "Category eq 'Resort' and Rating ge 4.0"  // Hard constraint
// query               ← tìm kiếm ngữ nghĩa trong kết quả đã filter


// ⚠️  Lưu ý về IsFilterable:
// Field phải khai báo IsFilterable=true TRƯỚC KHI index data
// Thêm IsFilterable sau không áp dụng cho documents cũ (cần reindex)
[<SimpleField(IsFilterable = true)>]   // Phải có attribute này
Category: string
```

---

## 3. Collection(String) Filter — Lambda Expressions

Khi field là `Collection(String)` (mảng chuỗi), `$filter` thông thường không hoạt động — phải dùng **lambda expression** với `any()` hoặc `all()`.

### 3.1 Cú pháp lambda

```
field/any(variable: condition)
field/all(variable: condition)
```

Trong đó:
- `field` — tên field kiểu Collection
- `variable` — tên biến đại diện cho mỗi phần tử (tự đặt, thường 1-2 ký tự)
- `condition` — điều kiện áp dụng cho từng phần tử

### 3.2 `any()` — Ít nhất một phần tử khớp

```fsharp
// Document:
// { Tags: ["F#", ".NET", "Azure", "Functional"] }
// { Skills: ["Python", "Django", "PostgreSQL"] }

// any() → true nếu ÍT NHẤT MỘT phần tử trong collection khớp điều kiện

// Tìm documents có tag "F#"
options.Filter <- "Tags/any(t: t eq 'F#')"
// → Khớp document 1 (có "F#"), không khớp document 2

// Tìm documents có ít nhất một skill trong danh sách
options.Filter <- "Skills/any(s: s eq 'Python' or s eq 'F#' or s eq 'Haskell')"

// Kết hợp với search.in — idiomatic hơn
options.Filter <- "Skills/any(s: search.in(s, 'Python|F#|Haskell', '|'))"

// Tìm documents có tag bắt đầu bằng "Azure"
// (Lưu ý: filter không hỗ trợ wildcard — dùng search.ismatch nếu cần)

// Kết hợp any() với điều kiện khác
options.Filter <- "Tags/any(t: t eq 'F#') and Rating gt 4.0"
options.Filter <- "Tags/any(t: t eq 'Azure') and Tags/any(t: t eq '.NET')"
// ↑ Document phải có CẢ "Azure" VÀ ".NET" trong Tags
```

### 3.3 `all()` — Tất cả phần tử đều khớp

```fsharp
// all() → true nếu TẤT CẢ phần tử trong collection khớp điều kiện
// Ít dùng hơn any() nhưng có use case riêng

// Tìm documents mà TẤT CẢ tags đều là approved tags
options.Filter <- "Tags/all(t: search.in(t, 'F#|.NET|Azure|Haskell', '|'))"

// Tìm documents mà TẤT CẢ scores đều >= 3.0
options.Filter <- "Scores/all(s: s ge 3.0)"

// Edge case: collection rỗng
// any() trên collection rỗng → false (không có phần tử nào khớp)
// all() trên collection rỗng → true  (vacuous truth — không có phần tử nào vi phạm)
```

### 3.4 `any()` không tham số — Collection không rỗng

```fsharp
// any() không tham số → true nếu collection có ÍT NHẤT MỘT phần tử
options.Filter <- "Tags/any()"      // Chỉ lấy documents có ít nhất 1 tag
options.Filter <- "not Tags/any()"  // Chỉ lấy documents không có tag nào
```

### 3.5 Ví dụ thực tế với F# schema

```fsharp
[<CLIMutable>]
type DeveloperDocument =
    {
        [<SimpleField(IsKey = true)>]
        Id: string

        [<SearchableField(IsSortable = true)>]
        Name: string

        // Collection — phải IsFilterable để dùng lambda trong filter
        [<SearchableField(IsFilterable = true, IsFacetable = true)>]
        Skills: string[]

        [<SearchableField(IsFilterable = true, IsFacetable = true)>]
        Certifications: string[]

        [<SimpleField(IsFilterable = true, IsSortable = true)>]
        YearsOfExperience: Nullable<int>
    }

// Queries thực tế
let findFSharpDevelopers (client: SearchClient) = task {
    let options = SearchOptions()
    // Có F# trong skills VÀ ít nhất 3 năm kinh nghiệm
    options.Filter <- "Skills/any(s: s eq 'F#') and YearsOfExperience ge 3"
    options.OrderBy.Add("YearsOfExperience desc")

    let! response = client.SearchAsync<DeveloperDocument>("*", options)
    return response.Value.GetResults() |> Seq.map (fun r -> r.Document) |> Seq.toList
}

let findFullStackFSharp (client: SearchClient) = task {
    let options = SearchOptions()
    // Có cả F# và Azure trong skills
    options.Filter <-
        "Skills/any(s: s eq 'F#') and Skills/any(s: search.in(s, 'Azure|AWS|GCP', '|'))"

    let! response = client.SearchAsync<DeveloperDocument>("*", options)
    return response.Value.GetResults() |> Seq.map (fun r -> r.Document) |> Seq.toList
}
```

---

## 4. ComplexType — Nested Object và Object Array

`ComplexType` cho phép một field chứa **nested object** (có nhiều sub-fields), thay vì chỉ là một giá trị scalar.

### 4.1 Hai dạng ComplexType

```
ComplexType đơn (single nested object):
  Document
  └── Address (ComplexType)
      ├── Street: string
      ├── City:   string
      └── ZipCode: string

Collection(ComplexType) — mảng nested objects:
  Document
  └── Rooms[] (Collection of ComplexType)
      ├── [0]: { Type: "Deluxe", Price: 250, Capacity: 2 }
      ├── [1]: { Type: "Suite",  Price: 500, Capacity: 4 }
      └── [2]: { Type: "Standard", Price: 120, Capacity: 2 }
```

### 4.2 Định nghĩa ComplexType trong F#

```fsharp
// Sub-type cho nested object
[<CLIMutable>]
type Address =
    {
        [<SearchableField>]
        Street: string

        [<SimpleField(IsFilterable = true, IsFacetable = true)>]
        City: string

        [<SimpleField(IsFilterable = true)>]
        Province: string

        [<SimpleField(IsFilterable = true)>]
        ZipCode: string
    }

// Sub-type cho collection of complex objects
[<CLIMutable>]
type Room =
    {
        [<SimpleField(IsFilterable = true, IsFacetable = true)>]
        Type: string              // "Deluxe", "Suite", "Standard"

        [<SimpleField(IsFilterable = true, IsSortable = false)>]
        BaseRate: Nullable<float> // IsSortable=false: không sort trong Collection

        [<SimpleField(IsFilterable = true, IsFacetable = true)>]
        Capacity: Nullable<int>

        [<SimpleField(IsFilterable = true)>]
        HasBalcony: Nullable<bool>

        [<SearchableField(IsFilterable = true, IsFacetable = true)>]
        Amenities: string[]       // Collection trong Collection ✅ được hỗ trợ
    }

// Parent document chứa cả hai dạng ComplexType
[<CLIMutable>]
type HotelDocument =
    {
        [<SimpleField(IsKey = true)>]
        Id: string

        [<SearchableField(IsSortable = true)>]
        Name: string

        [<SimpleField(IsFilterable = true, IsSortable = true)>]
        Rating: Nullable<float>

        // Single ComplexType — dùng [<FieldBuilderIgnore>] và thêm thủ công
        // hoặc SDK sẽ tự detect nested record
        Address: Address

        // Collection(ComplexType) — mảng rooms
        Rooms: Room[]
    }
```

### 4.3 Giới hạn và lưu ý quan trọng

```fsharp
// ⚠️  IsSortable KHÔNG được phép trong sub-fields của Collection(ComplexType)
// SDK sẽ throw khi tạo index nếu vi phạm

// ✅ Được phép trong ComplexType đơn (single object)
type Address =
    {
        [<SimpleField(IsFilterable = true, IsSortable = true)>]  // ✅ OK
        City: string
    }

// ❌ Không được trong Collection(ComplexType)
type Room =
    {
        [<SimpleField(IsFilterable = true, IsSortable = true)>]  // ❌ Error!
        BaseRate: Nullable<float>
        // Fix: IsSortable = false
    }

// ⚠️  Depth limit: Azure chỉ hỗ trợ 1 cấp nesting
// ComplexType trong ComplexType KHÔNG được hỗ trợ
// Room → SubRoom → ... ❌

// ✅ Nhưng Collection(String) trong ComplexType được
type Room =
    {
        Amenities: string[]  // ✅ Collection(String) trong ComplexType
    }
```

### 4.4 Filter trên ComplexType

```fsharp
// Filter trên single ComplexType — dùng dot notation
options.Filter <- "Address/City eq 'Hà Nội'"
options.Filter <- "Address/City eq 'Hồ Chí Minh' and Address/Province eq 'HCM'"

// Filter trên Collection(ComplexType) — kết hợp dot notation và lambda
// Tìm hotel có ít nhất 1 phòng loại Deluxe
options.Filter <- "Rooms/any(r: r/Type eq 'Deluxe')"

// Tìm hotel có ít nhất 1 phòng giá dưới 200 VÀ sức chứa >= 2
options.Filter <- "Rooms/any(r: r/BaseRate lt 200 and r/Capacity ge 2)"

// Tìm hotel có phòng Deluxe CÓ ban công
options.Filter <- "Rooms/any(r: r/Type eq 'Deluxe' and r/HasBalcony eq true)"

// Tìm hotel có phòng với amenity cụ thể (Collection trong Collection)
options.Filter <- "Rooms/any(r: r/Amenities/any(a: a eq 'Ocean View'))"

// Tìm hotel mà TẤT CẢ phòng đều có giá hợp lý
options.Filter <- "Rooms/all(r: r/BaseRate le 500)"

// Kết hợp filter trên parent và nested
options.Filter <- "Rating gt 4.0 and Address/City eq 'Đà Nẵng' and Rooms/any(r: r/BaseRate lt 300)"

// Filter trên single ComplexType kết hợp với OrderBy
options.Filter  <- "Address/City eq 'Hà Nội'"
options.OrderBy.Add("Rating desc")   // OrderBy trên parent field ✅
// options.OrderBy.Add("Rooms/BaseRate asc")  ❌ Không sort được trên Collection
```

### 4.5 Upload document có ComplexType

```fsharp
let sampleHotel : HotelDocument = {
    Id     = "hotel-001"
    Name   = "Grand Metropolis Da Nang"
    Rating = Nullable 4.7

    Address = {
        Street   = "123 Bạch Đằng"
        City     = "Đà Nẵng"
        Province = "Đà Nẵng"
        ZipCode  = "550000"
    }

    Rooms = [|
        { Type       = "Standard"
          BaseRate   = Nullable 1_200_000.0
          Capacity   = Nullable 2
          HasBalcony = Nullable false
          Amenities  = [| "WiFi"; "TV"; "Air Conditioning" |] }

        { Type       = "Deluxe"
          BaseRate   = Nullable 2_500_000.0
          Capacity   = Nullable 2
          HasBalcony = Nullable true
          Amenities  = [| "WiFi"; "TV"; "Ocean View"; "Mini Bar"; "Bathtub" |] }

        { Type       = "Suite"
          BaseRate   = Nullable 5_000_000.0
          Capacity   = Nullable 4
          HasBalcony = Nullable true
          Amenities  = [| "WiFi"; "TV"; "Ocean View"; "Butler Service"; "Private Pool" |] }
    |]
}

let upload (client: SearchClient) =
    let batch = IndexDocumentsBatch.MergeOrUpload([sampleHotel])
    client.IndexDocuments(batch) |> ignore
```

---

## 5. Facets — Aggregation cho Filter Sidebar

Facets trả về **thống kê phân nhóm** cùng với kết quả search, cho phép xây dựng filter sidebar kiểu e-commerce.

```
Search results: 142 hotels
│
├── Category facet:          ← string facet
│   ├── Resort (45)
│   ├── Boutique (38)
│   ├── Business (31)
│   └── Budget (28)
│
├── Rating facet:            ← numeric interval facet
│   ├── 4.0–5.0 (67)
│   ├── 3.0–4.0 (52)
│   └── 2.0–3.0 (23)
│
└── Skills/Tags facet:       ← collection facet
    ├── Pool (89)
    ├── Spa (61)
    └── Gym (44)
```

### 5.1 Yêu cầu

```fsharp
// Field phải có IsFacetable = true trong schema
[<SimpleField(IsFilterable = true, IsFacetable = true)>]
Category: string

[<SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)>]
Rating: Nullable<float>

// Collection field cũng hỗ trợ facet
[<SearchableField(IsFilterable = true, IsFacetable = true)>]
Tags: string[]
```

### 5.2 String Facet — Đếm theo giá trị

```fsharp
let options = SearchOptions()

// Facet cơ bản — đếm số documents theo từng giá trị Category
options.Facets.Add("Category")

// Giới hạn số buckets trả về (mặc định: 10)
options.Facets.Add("Category,count:5")          // Top 5 categories

// Sắp xếp buckets
options.Facets.Add("Category,count:10,sort:count")   // Sort theo số lượng giảm dần
options.Facets.Add("Category,count:10,sort:value")   // Sort theo value (alphabetical)
options.Facets.Add("Category,count:10,sort:-value")  // Sort value giảm dần

// Đọc kết quả facet
let! response = client.SearchAsync<HotelDocument>("*", options)
let facets = response.Value.Facets

if facets <> null && facets.ContainsKey("Category") then
    printfn "=== Category Facets ==="
    facets["Category"]
    |> Seq.iter (fun bucket ->
        // bucket.Value: giá trị của bucket ("Resort", "Boutique", ...)
        // bucket.Count: số documents trong bucket
        printfn "  %-20s: %d" (string bucket.Value) (bucket.Count.GetValueOrDefault()))

// Output:
// === Category Facets ===
//   Resort              : 45
//   Boutique            : 38
//   Business            : 31
//   Budget              : 28
//   Motel               : 10
```

### 5.3 Numeric Interval Facet — Histogram

```fsharp
// interval: chia numeric field thành các khoảng đều nhau

// Rating histogram với khoảng 1.0
options.Facets.Add("Rating,interval:1")
// Buckets: [1.0, 2.0), [2.0, 3.0), [3.0, 4.0), [4.0, 5.0)

// Price histogram với khoảng 500,000 VND
options.Facets.Add("PricePerNight,interval:500000")
// Buckets: [0, 500k), [500k, 1M), [1M, 1.5M), ...

// YearBuilt histogram với khoảng 10 năm
options.Facets.Add("YearBuilt,interval:10")
// Buckets: [1960, 1970), [1970, 1980), ..., [2020, 2030)

// Đọc numeric facet — cùng API nhưng Value là số
let! response = client.SearchAsync<HotelDocument>("*", options)

if response.Value.Facets.ContainsKey("Rating") then
    printfn "=== Rating Histogram ==="
    response.Value.Facets["Rating"]
    |> Seq.iter (fun bucket ->
        let rangeStart = bucket.Value  // Double: 1.0, 2.0, 3.0, 4.0
        let count      = bucket.Count.GetValueOrDefault()
        printfn "  %.1f★ – %.1f★ : %d hotels"
            (rangeStart :?> float) (rangeStart :?> float + 1.0) count)

// Output:
// === Rating Histogram ===
//   1.0★ – 2.0★ : 5 hotels
//   2.0★ – 3.0★ : 18 hotels
//   3.0★ – 4.0★ : 52 hotels
//   4.0★ – 5.0★ : 67 hotels
```

### 5.4 DateTimeOffset Interval Facet

```fsharp
// interval cho date: second, minute, hour, day, week, month, quarter, year
options.Facets.Add("LastUpdated,interval:month")
options.Facets.Add("CreatedAt,interval:year")
options.Facets.Add("CheckIn,interval:day")

// Đọc date facet
if response.Value.Facets.ContainsKey("LastUpdated") then
    response.Value.Facets["LastUpdated"]
    |> Seq.iter (fun bucket ->
        // Value là DateTimeOffset
        let date  = bucket.Value :?> DateTimeOffset
        let count = bucket.Count.GetValueOrDefault()
        printfn "  %s: %d" (date.ToString("yyyy-MM")) count)
```

### 5.5 Collection(String) Facet

Khi field là `Collection(String)`, Azure tự **flatten** mỗi giá trị trong mảng thành bucket riêng.

```fsharp
// Schema:
// Tags: string[]  →  ["Pool", "Spa", "WiFi"]
// Mỗi giá trị trong Tags được đếm như một bucket riêng

options.Facets.Add("Tags")
options.Facets.Add("Tags,count:10,sort:count")

// Document 1: Tags = ["Pool", "Spa", "WiFi"]
// Document 2: Tags = ["Pool", "Gym", "WiFi"]
// Document 3: Tags = ["Spa", "Gym", "Bar"]

// Facet result:
//   Pool  : 2   (có trong doc 1 và doc 2)
//   Spa   : 2   (có trong doc 1 và doc 3)
//   WiFi  : 2   (có trong doc 1 và doc 2)
//   Gym   : 2   (có trong doc 2 và doc 3)
//   Bar   : 1

// Đọc collection facet — cùng API với string facet
if response.Value.Facets.ContainsKey("Tags") then
    response.Value.Facets["Tags"]
    |> Seq.iter (fun bucket ->
        printfn "  %-15s: %d" (string bucket.Value) (bucket.Count.GetValueOrDefault()))
```

### 5.6 Nested Facet trên ComplexType

```fsharp
// Facet trên sub-field của ComplexType dùng slash notation
options.Facets.Add("Address/City")           // Facet trên single ComplexType
options.Facets.Add("Rooms/Type")             // Facet trên Collection(ComplexType)
options.Facets.Add("Rooms/BaseRate,interval:500000")

// Rooms/Type facet ví dụ:
//   Standard : 89 hotels (có ít nhất 1 phòng Standard)
//   Deluxe   : 76 hotels
//   Suite    : 45 hotels
```

### 5.7 Full pattern: Search + Filter + Facet

```fsharp
// Pattern hoàn chỉnh cho search page với filter sidebar
let buildSearchPage
    (client: SearchClient)
    (query: string)
    (categoryFilter: string list)
    (minRating: float)
    (page: int)
    (pageSize: int) = task {

    let options = SearchOptions()

    // Search mode
    options.SearchMode <- SearchMode.Any

    // Pagination
    options.Size <- pageSize
    options.Skip <- (page - 1) * pageSize
    options.IncludeTotalCount <- true

    // Select fields cần thiết (không lấy toàn bộ document)
    ["Id"; "Name"; "Rating"; "Category"; "Address/City"; "Rooms/BaseRate"]
    |> List.iter options.Select.Add

    // Build filter từ user selection
    let filters = [
        if categoryFilter.Length > 0 then
            let cats = categoryFilter |> String.concat "|"
            yield $"search.in(Category, '{cats}', '|')"
        if minRating > 0.0 then
            yield $"Rating ge {minRating}"
    ]
    if filters.Length > 0 then
        options.Filter <- filters |> String.concat " and "

    // Facets cho sidebar
    options.Facets.Add("Category,count:10,sort:count")
    options.Facets.Add("Rating,interval:1")
    options.Facets.Add("Address/City,count:10,sort:count")
    options.Facets.Add("Rooms/Type,count:5")

    // Highlight
    options.HighlightFields.Add("Name")
    options.HighlightPreTag  <- "<mark>"
    options.HighlightPostTag <- "</mark>"

    let! response = client.SearchAsync<HotelDocument>(query, options)
    let value = response.Value

    return {|
        TotalCount = value.TotalCount |> Option.ofNullable |> Option.defaultValue 0L
        Documents  =
            value.GetResults()
            |> Seq.map (fun r -> {| Score = r.Score; Doc = r.Document; Highlights = r.Highlights |})
            |> Seq.toList
        Facets = {|
            Categories = value.Facets.TryGetValue("Category") |> snd |> Option.ofObj
            Ratings    = value.Facets.TryGetValue("Rating")   |> snd |> Option.ofObj
            Cities     = value.Facets.TryGetValue("Address/City") |> snd |> Option.ofObj
        |}
    |}
}
```

---

## Tổng kết

```
OData $filter Operators:
  So sánh:  eq, ne, gt, ge, lt, le
  Logic:    and, or, not (dùng ngoặc để rõ thứ tự ưu tiên)
  List:     search.in(field, 'v1|v2|v3', '|')  ← thay cho chuỗi or

Filter vs Search:
  Filter  → không qua analyzer, exact match, cần IsFilterable=true
  Search  → qua analyzer, token match, ảnh hưởng score
  Filter không tính vào relevance score

Collection(String) Lambda:
  any(x: condition) → ít nhất 1 phần tử khớp  ← dùng phổ biến nhất
  all(x: condition) → tất cả phần tử khớp
  any()             → collection không rỗng
  any() trên empty collection → false
  all() trên empty collection → true (vacuous truth)

ComplexType:
  Single nested object  → filter bằng dot: Address/City eq 'Hà Nội'
  Collection of objects → filter bằng lambda: Rooms/any(r: r/Type eq 'Deluxe')
  Giới hạn: IsSortable=false bắt buộc trong Collection(ComplexType)
  Giới hạn: chỉ 1 cấp nesting (không có ComplexType trong ComplexType)
  Collection(String) trong ComplexType ✅ được hỗ trợ

Facets:
  String facet    → đếm per value: "Category,count:10,sort:count"
  Numeric facet   → histogram: "Rating,interval:1", "Price,interval:500000"
  Date facet      → histogram: "CreatedAt,interval:month"
  Collection facet → flatten mỗi item: "Tags,count:10"
  ComplexType facet → slash notation: "Address/City", "Rooms/Type,count:5"
  Field phải có IsFacetable=true và IsFilterable=true
```