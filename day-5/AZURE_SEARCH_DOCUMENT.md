# 🔍 Azure Cognitive Search với F# — Hướng dẫn toàn diện

## 📋 Tổng quan

Tài liệu này hướng dẫn sử dụng **Azure Cognitive Search** (Azure Search) trong **F#** từ cơ bản đến nâng cao, bao gồm tạo index, import dữ liệu, tìm kiếm và các best practices.

> **Stack:** F# · .NET 8 · Azure.Search.Documents · Azure Cognitive Search

---

## 🎯 Azure Cognitive Search là gì?

**Azure Cognitive Search** là dịch vụ tìm kiếm đám mây (search-as-a-service) của Microsoft Azure, cung cấp:

- ✅ **Full-text search** với scoring và ranking thông minh
- ✅ **Faceted navigation** (lọc theo nhiều tiêu chí)
- ✅ **Autocomplete & Suggestions** (gợi ý tìm kiếm)
- ✅ **AI-powered enrichment** (phân tích ảnh, văn bản)
- ✅ **Geo-spatial search** (tìm kiếm theo vị trí địa lý)
- ✅ **Multi-language support** (hỗ trợ nhiều ngôn ngữ)

### So sánh với các giải pháp khác

| Tính năng                | Azure Search      | Elasticsearch     | SQL Full-Text    |
|--------------------------|-------------------|-------------------|------------------|
| **Quản lý**              | Fully managed     | Self-hosted       | Built-in DB      |
| **AI Integration**       | ✅ Native         | ❌ Manual         | ❌               |
| **Scalability**          | ✅ Auto           | ⚠️ Manual         | ⚠️ Limited       |
| **Faceted Search**       | ✅                | ✅                | ⚠️ Limited       |
| **Pricing**              | Pay-per-use       | Infrastructure    | Included         |

---

## 1. Hierarchy: Service → Index → Documents → Fields

Azure AI Search tổ chức dữ liệu theo 4 tầng lồng nhau:

```
Azure AI Search Service
└── Index  (ví dụ: "hotels-index", "products-index")
    └── Document  (một record — tương đương một row trong SQL)
        └── Field  (một thuộc tính của document — tương đương một column)
```

| Tầng | Tương đương | Mô tả |
|---|---|---|
| **Service** | Database server | Endpoint duy nhất, chứa nhiều indexes |
| **Index** | Table / Collection | Schema cố định, đơn vị search độc lập |
| **Document** | Row / Record | Một thực thể dữ liệu (khách sạn, sản phẩm…) |
| **Field** | Column | Thuộc tính có kiểu dữ liệu và behavior rõ ràng |

**Ví dụ thực tế:**

```
Service: https://my-company.search.windows.net
├── Index: "hotels-index"
│   ├── Document { Id: "h001", Name: "Metropolis Hotel", Rating: 4.8 }
│   │   ├── Field: Id       (String, IsKey)
│   │   ├── Field: Name     (String, IsSearchable, IsSortable)
│   │   └── Field: Rating   (Double, IsFilterable, IsSortable)
│   └── Document { Id: "h002", Name: "Grand Palace", Rating: 4.2 }
└── Index: "products-index"
    └── Document { Id: "p001", Name: "Laptop", Price: 1299.99 }
```

> **Lưu ý:** Mỗi Index có schema riêng biệt và hoàn toàn độc lập. Không có JOIN giữa các indexes.

---

## 2. Định nghĩa SearchField và các Attributes

Mỗi Field trong Index được định nghĩa bằng tổ hợp các attributes kiểm soát behavior của field đó.

### 2.1 Bảng tóm tắt attributes

| Attribute | Kiểu | Mô tả | Dùng khi nào |
|---|---|---|---|
| `IsKey` | `bool` | Định danh duy nhất của document | Bắt buộc có đúng 1 field |
| `IsSearchable` | `bool` | Field được đưa vào full-text index | Cần tìm kiếm bằng text |
| `IsFilterable` | `bool` | Dùng được trong `$filter` OData | Cần lọc chính xác (category, status) |
| `IsSortable` | `bool` | Dùng được trong `$orderby` | Cần sắp xếp kết quả |
| `IsFacetable` | `bool` | Tổng hợp thống kê theo nhóm | Cần sidebar lọc (faceted navigation) |

### 2.2 Chi tiết từng attribute

#### `IsKey = true`

- Mỗi Index bắt buộc có **đúng một** field làm key.
- Kiểu dữ liệu phải là `String` (Azure sẽ encode nội bộ).
- Dùng để retrieve, delete, hoặc merge document cụ thể.
- Không thể là `IsSearchable` (key không tham gia full-text search).

```fsharp
[<SimpleField(IsKey = true)>]
Id: string
```

#### `IsSearchable = true`

- Field được **phân tích (analyzed)** và đưa vào inverted index.
- Cho phép tìm kiếm từ khóa, phrase match, fuzzy search.
- Chỉ áp dụng cho kiểu `String` và `Collection(String)`.
- Có thể chỉ định `AnalyzerName` để xử lý ngôn ngữ (vi, en, ja…).

```fsharp
[<SearchableField(AnalyzerName = "vi.lucene")>]
Description: string

[<SearchableField>]
Tags: string[]  // Collection(String)
```

> **Lưu ý:** Khi field là `IsSearchable`, dữ liệu gốc vẫn được lưu, nhưng ngoài ra còn có inverted index riêng. Storage tăng lên.

#### `IsFilterable = true`

- Field có thể dùng trong OData `$filter` expression.
- Dữ liệu được lưu nguyên vẹn (không phân tích).
- Phù hợp cho: giá, trạng thái, category, boolean flags.

```fsharp
[<SimpleField(IsFilterable = true)>]
Category: string

[<SimpleField(IsFilterable = true)>]
IsAvailable: bool

// Query: $filter=Category eq 'Hotel' and Rating gt 4.0
```

#### `IsSortable = true`

- Field có thể dùng trong `$orderby`.
- Yêu cầu lưu thêm cấu trúc dữ liệu để sort nhanh.
- **Không thể** `IsSortable = true` khi `IsSearchable = true` cùng lúc trên `Collection(String)`.

```fsharp
[<SimpleField(IsSortable = true)>]
Rating: double

[<SimpleField(IsSortable = true)>]
CreatedAt: DateTimeOffset

// Query: $orderby=Rating desc, CreatedAt asc
```

#### `IsFacetable = true`

- Field xuất hiện trong kết quả **facet** (thống kê phân nhóm).
- Dùng cho sidebar lọc kiểu "Danh mục (45) | Địa điểm (23)".
- Thường kết hợp với `IsFilterable`.
- Không phù hợp cho field có cardinality cao (ID, email, URL).

```fsharp
[<SimpleField(IsFilterable = true, IsFacetable = true)>]
Category: string

// Response sẽ có:
// facets: { "Category": [{ value: "Resort", count: 15 }, { value: "Boutique", count: 8 }] }
```

### 2.3 Ma trận compatibility

| Attribute | String | Int32 | Double | DateTimeOffset | Collection(String) |
|---|:---:|:---:|:---:|:---:|:---:|
| IsKey | ✅ | ❌ | ❌ | ❌ | ❌ |
| IsSearchable | ✅ | ❌ | ❌ | ❌ | ✅ |
| IsFilterable | ✅ | ✅ | ✅ | ✅ | ✅ |
| IsSortable | ✅ | ✅ | ✅ | ✅ | ❌ |
| IsFacetable | ✅ | ✅ | ✅ | ✅ | ✅ |

---

## 3. Kiểu dữ liệu Field

### 3.1 Các kiểu cơ bản

| Kiểu Azure Search | Kiểu .NET / F# | Ghi chú |
|---|---|---|
| `Edm.String` | `string` | Mặc định, phổ biến nhất |
| `Edm.Int32` | `int` / `Nullable<int>` | Số nguyên 32-bit |
| `Edm.Int64` | `int64` / `Nullable<int64>` | Số nguyên 64-bit |
| `Edm.Double` | `float` / `Nullable<float>` | Số thực dấu phẩy động |
| `Edm.Boolean` | `bool` / `Nullable<bool>` | True/false |
| `Edm.DateTimeOffset` | `DateTimeOffset` / `Nullable<DateTimeOffset>` | Timestamp với timezone |
| `Edm.GeographyPoint` | `GeographyPoint` | Tọa độ địa lý (lat/lon) |
| `Collection(Edm.String)` | `string[]` hoặc `IList<string>` | Mảng chuỗi |

### 3.2 Ví dụ định nghĩa trong F#

```fsharp
open Azure.Search.Documents.Indexes
open Azure.Search.Documents.Indexes.Models

[<CLIMutable>]
type Category = {
    [<SimpleField(IsKey = true)>]
    id          : string

    [<SearchableField>]
    name        : string

    [<SearchableField>]
    description : string

    [<SimpleField(IsFilterable = true, IsFacetable = true)>]
    itemType    : string

    [<SimpleField(IsFilterable = true, IsFacetable = true)>]
    bookCount   : int

    [<SimpleField(IsFilterable = true, IsFacetable = true)>]
    isActive    : bool

    [<SimpleField(IsFilterable = true, IsSortable = true)>]
    sortOrder   : int

    [<SimpleField(IsFilterable = true, IsSortable = true)>]
    createdAt   : DateTime
}
...
```

### 3.3 Lưu ý về `Nullable` trong F#

Azure SDK yêu cầu value types phải nullable nếu field có thể không có giá trị:

```fsharp
// ✅ Đúng — Int32, Double, DateTimeOffset cần Nullable<T>
Rating: Nullable<float>
BuiltYear: Nullable<int>
LastUpdated: Nullable<DateTimeOffset>

// ✅ String không cần Nullable (reference type)
Name: string  // null nghĩa là không có giá trị

// ✅ Nếu dùng F# Option, cần custom converter hoặc dùng Nullable<T>
```

---

## 4. SearchIndexClient vs SearchClient

Azure SDK cung cấp hai client với vai trò hoàn toàn khác nhau.

### 4.1 `SearchIndexClient` — Quản lý schema

**Namespace:** `Azure.Search.Documents.Indexes`

**Dùng để:** Tạo, cập nhật, xóa Index definitions. Không tương tác với dữ liệu.

**Cần:** Admin Key (key có quyền write schema).

```fsharp
open Azure.Search.Documents.Indexes
open Azure.Search.Documents.Indexes.Models

let indexClient =
    SearchIndexClient(
        Uri("https://my-service.search.windows.net"),
        AzureKeyCredential("ADMIN-KEY-HERE")
    )

// Tạo hoặc update index
let createIndex () =
    let fields =
        FieldBuilder()
            .Build(typeof<HotelDocument>)

    let index = SearchIndex("hotels-index", fields)

    // Thêm scoring profile (optional)
    let scoringProfile = ScoringProfile("freshness-boost")
    scoringProfile.FunctionAggregation <- ScoringFunctionAggregation.Sum
    index.ScoringProfiles.Add(scoringProfile)

    indexClient.CreateOrUpdateIndex(index)

// Lấy danh sách indexes
let listIndexes () =
    indexClient.GetIndexNames()
    |> Seq.toList

// Xóa index (cẩn thận — xóa luôn dữ liệu)
let deleteIndex () =
    indexClient.DeleteIndex("hotels-index")

// Lấy thống kê index
let getStats () =
    indexClient.GetIndexStatistics("hotels-index")
```

### 4.2 `SearchClient` — Tương tác với dữ liệu

**Namespace:** `Azure.Search.Documents`

**Dùng để:** Upload documents, search, delete documents. Không thể thay đổi schema.

**Cần:** Query Key (chỉ read) hoặc Admin Key (read + write documents).

```fsharp
open Azure.Search.Documents
open Azure.Search.Documents.Models

let searchClient =
    SearchClient(
        Uri("https://my-service.search.windows.net"),
        "hotels-index",           // Tên index — cố định khi tạo client
        AzureKeyCredential("QUERY-KEY-HERE")
    )

// Upload / merge documents
let uploadDocuments (docs: HotelDocument list) =
    let batch = IndexDocumentsBatch.MergeOrUpload(docs)
    searchClient.IndexDocuments(batch) |> ignore

// Tìm kiếm
let search (query: string) (minRating: float) =
    let options = SearchOptions()
    options.Filter <- $"Rating gt {minRating}"
    options.OrderBy.Add("Rating desc")
    options.Facets.Add("Category")
    options.Size <- 10

    let response = searchClient.Search<HotelDocument>(query, options)

    response.Value.GetResults()
    |> Seq.map (fun r -> r.Document)
    |> Seq.toList

// Lấy document theo ID
let getById (id: string) =
    searchClient.GetDocument<HotelDocument>(id)

// Xóa document
let deleteDocument (id: string) =
    let batch = IndexDocumentsBatch.Delete("Id", [id])
    searchClient.IndexDocuments(batch) |> ignore
```

### 4.3 So sánh nhanh

| Đặc điểm | `SearchIndexClient` | `SearchClient` |
|---|---|---|
| **Mục đích** | Quản lý schema (DDL) | Thao tác dữ liệu (DML) |
| **Tương đương SQL** | `CREATE/ALTER/DROP TABLE` | `SELECT/INSERT/UPDATE/DELETE` |
| **Key cần** | Admin Key | Query Key hoặc Admin Key |
| **Scope** | Toàn bộ service | Một index cụ thể |
| **Dùng trong** | Startup / migration | Request handler |
| **Thread-safe** | ✅ | ✅ |
| **Nên singleton** | ✅ | ✅ (một per index) |

### 4.4 Tổ chức trong F# project

```fsharp
// SearchClients.fs — khởi tạo một lần, inject qua DI

module SearchClients =

    open Azure
    open Azure.Search.Documents
    open Azure.Search.Documents.Indexes

    let private endpoint = Uri(Env.get "SEARCH_ENDPOINT")
    let private adminKey  = AzureKeyCredential(Env.get "SEARCH_ADMIN_KEY")
    let private queryKey  = AzureKeyCredential(Env.get "SEARCH_QUERY_KEY")

    /// Dùng để tạo/update index schema (chỉ gọi khi startup/migration)
    let indexClient = SearchIndexClient(endpoint, adminKey)

    /// Dùng cho toàn bộ search queries trong production
    let hotelsClient = SearchClient(endpoint, "hotels-index", queryKey)
```

---

## 5. Constraints khi cập nhật Schema

Schema của Azure AI Search Index có **ràng buộc quan trọng** — vi phạm sẽ gây lỗi `400 Bad Request`.

### 5.1 Quy tắc vàng

> **Chỉ có thể THÊM field mới vào index đang có dữ liệu.**  
> **Không thể XÓA field hoặc THAY ĐỔI kiểu/thuộc tính của field đã tồn tại.**

### 5.2 Bảng các thao tác cho phép và cấm

| Thao tác | Cho phép? | Ghi chú |
|---|:---:|---|
| Thêm field mới vào index | ✅ | Field mới có giá trị `null` cho documents cũ |
| Thêm field với `IsSearchable = true` | ✅ | Cần rebuild index để analyze documents cũ |
| Đổi `IsFilterable` từ false → true | ❌ | Phải xóa và tạo lại index |
| Đổi `IsSearchable` từ false → true | ❌ | Phải xóa và tạo lại index |
| Đổi kiểu từ `String` → `Int32` | ❌ | Không thể thay đổi type |
| Xóa một field | ❌ | Không thể xóa field |
| Đổi tên field | ❌ | Không có rename — phải add field mới |
| Thêm `IsKey` vào field khác | ❌ | Key field là immutable |
| Cập nhật `ScoringProfile` | ✅ | Schema metadata có thể update |
| Cập nhật `Analyzer` trên field cũ | ❌ | Phải recreate index |

### 5.3 Minh họa: thêm field mới (hợp lệ)

```fsharp
// Version 1 — Index ban đầu
[<CLIMutable>]
type HotelV1 =
    {
        [<SimpleField(IsKey = true)>]
        Id: string

        [<SearchableField(IsSortable = true)>]
        Name: string
    }

// Version 2 — Thêm field mới (OK)
[<CLIMutable>]
type HotelV2 =
    {
        [<SimpleField(IsKey = true)>]
        Id: string

        [<SearchableField(IsSortable = true)>]
        Name: string

        // ✅ Field mới — documents cũ sẽ có Rating = null
        [<SimpleField(IsFilterable = true, IsSortable = true)>]
        Rating: Nullable<float>

        // ✅ Field mới — documents cũ sẽ có Tags = []
        [<SearchableField(IsFilterable = true)>]
        Tags: string[]
    }

// Gọi CreateOrUpdateIndex — SDK sẽ diff và chỉ thêm fields mới
let migrate () =
    let fields = FieldBuilder().Build(typeof<HotelV2>)
    let index = SearchIndex("hotels-index", fields)
    indexClient.CreateOrUpdateIndex(index)  // An toàn nếu chỉ thêm field
```

### 5.4 Minh họa: thay đổi field (KHÔNG hợp lệ)

```fsharp
// ❌ Cố đổi IsFilterable từ false → true trên field cũ
// SDK sẽ throw: Azure.RequestFailedException: 400 Bad Request
// "Field 'Name' cannot be modified. Only new fields can be added."

// ❌ Cố đổi kiểu
// Từ: Name: string
// Sang: Name: int  → Lỗi ngay khi call CreateOrUpdateIndex
```

### 5.5 Chiến lược khi cần thay đổi schema

**Nếu cần xóa field hoặc đổi kiểu**, quy trình bắt buộc là:

```fsharp
// Bước 1: Tạo index mới với schema mong muốn
let migrateIndex () = task {
    let newFields = FieldBuilder().Build(typeof<HotelV3>)
    let newIndex  = SearchIndex("hotels-index-v2", newFields)
    indexClient.CreateOrUpdateIndex(newIndex) |> ignore

    // Bước 2: Re-index toàn bộ dữ liệu sang index mới
    let! allDocs = fetchAllFromDatabase ()
    let newClient = SearchClient(endpoint, "hotels-index-v2", adminKey)
    let batch     = IndexDocumentsBatch.Upload(allDocs)
    newClient.IndexDocuments(batch) |> ignore

    // Bước 3: Swap alias (nếu dùng index alias)
    // hoặc cập nhật config để trỏ sang "hotels-index-v2"

    // Bước 4: Xóa index cũ sau khi verify xong
    // indexClient.DeleteIndex("hotels-index")
}
```

> **Tip:** Dùng **Index Alias** (tính năng của Azure AI Search) để swap index không downtime:  
> App luôn trỏ vào alias `hotels`, alias có thể được redirect từ `hotels-v1` → `hotels-v2` bằng một API call.

### 5.6 Checklist thiết kế schema production

Trước khi deploy index lần đầu:

- [ ] Xác định rõ field nào cần `IsSearchable` (ảnh hưởng storage và latency)
- [ ] Chỉ bật `IsFacetable` cho fields có cardinality thấp (category, status, vùng)
- [ ] Tránh `IsSortable = true` cho field không thực sự cần sort (tốn memory)
- [ ] Đặt `Analyzer` phù hợp ngôn ngữ ngay từ đầu (`vi.lucene` cho tiếng Việt)
- [ ] Dự tính trước các fields có thể cần thêm trong 6 tháng tới
- [ ] Test schema với dữ liệu thực trước khi go-live

---

## Tổng kết

```
Service
  └── Index (schema cố định — chỉ thêm field, không xóa/sửa)
        └── Document (record; mỗi document tham chiếu đúng 1 key)
              └── Field (kiểu + attributes quyết định behavior)

SearchIndexClient  →  quản lý Index schema  (DDL, cần Admin Key)
SearchClient       →  upload/search data    (DML, dùng Query Key)

Attributes:
  IsKey        → định danh duy nhất, bắt buộc 1 field
  IsSearchable → tham gia full-text search (chỉ String/Collection)
  IsFilterable → dùng trong $filter expression
  IsSortable   → dùng trong $orderby
  IsFacetable  → xuất hiện trong facet navigation

Ràng buộc schema:
  ✅ Thêm field mới bất cứ lúc nào
  ❌ Không xóa field
  ❌ Không đổi kiểu dữ liệu
  ❌ Không đổi attributes của field cũ
  → Muốn thay đổi: tạo index mới + re-index dữ liệu
```