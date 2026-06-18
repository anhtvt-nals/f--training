# 🌟 Azure Cosmos DB với F# — Hướng dẫn toàn diện

## 📋 Tổng quan

Tài liệu này trình bày cách sử dụng **Azure Cosmos DB** trong **F#** một cách đầy đủ, bao gồm toàn bộ các thao tác CRUD, query nâng cao, và best practices theo chuẩn của Azure.

> **Stack:** F# · .NET 8 · Azure.Cosmos SDK · Azure Cosmos DB Emulator

---

## 🏗️ Kiến trúc dữ liệu trong Cosmos DB

### Hierarchy tổng quát

```
Account
  └── Database
        └── Container (Collection)
              └── Item (Document)
```

| Cosmos DB         | SQL Server   | MongoDB       | Ghi chú                                   |
|-------------------|--------------|---------------|-------------------------------------------|
| **Account**       | Server       | Cluster       | Endpoint + access key                     |
| **Database**      | Database     | Database      | Nhóm logic các containers                 |
| **Container**     | Table        | Collection    | Đơn vị scale, cần Partition Key           |
| **Item**          | Row          | Document      | JSON linh hoạt, tối đa 2MB/item          |
| **Partition Key** | Primary Key  | Shard Key     | Quyết định phân tán dữ liệu vật lý       |

---

## 🔹 1. Database

- Là **logical container** chứa các Containers.
- Một Cosmos Account có thể chứa **nhiều Databases**.
- Không có throughput settings riêng, trừ khi dùng chế độ **Shared Throughput**.
- Quản lý **security** và **access control** ở cấp Database.

---

## 🔹 2. Container (Collection)

Đơn vị **lưu trữ và scale** cơ bản. Mỗi Container yêu cầu cấu hình:

| Cấu hình              | Mô tả                                                        |
|-----------------------|--------------------------------------------------------------|
| **Partition Key**     | Cách phân tán data; **không thể thay đổi** sau khi tạo      |
| **Throughput (RU/s)** | Request Units per second — chi phí xử lý                    |
| **Indexing Policy**   | Field nào được index tự động                                 |
| **TTL**               | Tự động xóa items hết hạn                                    |

```fsharp
// Các containers trong project và chiến lược Partition Key:
// ┌─────────────────────┬──────────────────────────────────────────────────┐
// │ Container           │ Partition Key Strategy                           │
// ├─────────────────────┼──────────────────────────────────────────────────┤
// │ customers           │ /country        → geographic distribution         │
// │ orders              │ /orderDate      → time-based distribution         │
// │ products            │ /category       → category-based distribution     │
// │ analytics           │ /yearMonth      → time-series analytics           │
// └─────────────────────┴──────────────────────────────────────────────────┘
```

---

## 🔹 3. Item (Document)

- Lưu dưới dạng **JSON thuần**.
- **Schema-flexible**: Các items trong cùng container có thể có cấu trúc khác nhau.
- **Auto-indexing**: Tất cả properties được index theo mặc định.
- Giới hạn **2MB** mỗi item.

```fsharp
[<CLIMutable>]
type Customer = {
    id          : string         // Bắt buộc — định danh duy nhất trong partition
    customerId  : string         // Business identifier
    email       : string
    firstName   : string
    lastName    : string
    country     : string         // Partition Key → phân tán theo quốc gia
    loyaltyTier : string         // "Bronze" | "Silver" | "Gold" | "Platinum"
    totalSpent  : decimal
    totalOrders : int
    isActive    : bool
    createdAt   : DateTime
    _etag       : string option  // Optimistic concurrency control
}
```

---

## 🔹 4. Partition Key — Điều quan trọng nhất

> ⚠️ **Partition Key không thể thay đổi** sau khi container được tạo. Thiết kế cẩn thận ngay từ đầu.

### Nguyên tắc chọn Partition Key

```fsharp
module PartitionKeys =

    // ✅ TỐT — Phân tán đều theo địa lý
    let customerPartition (country: string) = country.ToUpper()
    // → "USA", "UK", "GERMANY", "VIETNAM"

    // ✅ TỐT — Phân tán theo thời gian (time-series data)
    let orderPartition (orderDate: DateTime) = orderDate.ToString("yyyy-MM")
    // → "2024-11", "2024-12", "2025-01"

    // ✅ TỐT — Phân tán theo danh mục
    let productPartition (category: string) = category.ToLower()
    // → "electronics", "clothing", "books"

    // ❌ XẤU — Cardinality thấp, tạo "hot partition"
    let badStatus (status: string) = status
    // → chỉ có "active" / "inactive", toàn bộ data dồn vào 2 partition

    // ❌ XẤU — Sequential IDs tạo hot spots
    let badSequential (orderId: int) = orderId.ToString()
    // → tất cả writes liên tục vào cùng một partition cuối
```

### Ảnh hưởng đến performance

| Loại query                    | Hiệu năng  | Chi phí RU  |
|-------------------------------|------------|-------------|
| Point read (ID + Partition Key) | ⚡ Nhanh nhất | ~1 RU     |
| Query trong cùng partition    | ✅ Nhanh    | Thấp        |
| Cross-partition query         | ⚠️ Chậm hơn | Cao hơn     |
| Full scan (không có filter)   | ❌ Rất chậm | Rất cao     |

---

## 🔹 5. ID — Composite Primary Key

Trong Cosmos DB, **ID + Partition Key** mới tạo thành khóa chính duy nhất.

```fsharp
module IdGeneration =

    /// ID chỉ cần unique trong cùng partition, không phải globally
    let generateCustomerId () =
        $"customer_{Guid.NewGuid().ToString("N")[..7]}"
        // → "customer_a3f82b1c"

    let generateOrderId (date: DateTime) =
        $"order_{date:yyyyMMdd}_{Guid.NewGuid().ToString("N")[..7]}"
        // → "order_20241215_b9e1f4a2"

    let generateProductId () =
        $"product_{Guid.NewGuid().ToString("N")[..7]}"
        // → "product_c7d3a9e5"

// Point read — thao tác nhanh nhất và rẻ nhất (~1 RU)
let! customer = repo.ReadCustomerAsync "customer_a3f82b1c" "USA"
//                                      ↑ ID              ↑ Partition Key
```

## Partition Key là gì và tại sao cần nó?
 
Khi bạn lưu dữ liệu vào Cosmos DB, hệ thống cần biết **đặt item này vào đâu** trong cụm máy chủ. Partition Key chính là câu trả lời cho câu hỏi đó — một field trong document mà Cosmos DB dùng để tính toán vị trí vật lý của item.
 
Nếu không có Partition Key, toàn bộ dữ liệu sẽ chồng chất vào một chỗ, không thể scale ra nhiều máy, và mọi query đều phải quét toàn bộ container — cực kỳ chậm và tốn kém.
 
```
Container: "customers"  |  Partition Key: /country
 
Items:
┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ id: u001     │  │ id: u002     │  │ id: u003     │  │ id: u004     │
│ country: USA │  │ country: DEU │  │ country: USA │  │ country: VNM │
└──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘
       │                 │                 │                 │
       ▼                 ▼                 ▼                 ▼
  Logical            Logical           Logical           Logical
  Partition          Partition         Partition         Partition
    USA                DEU               USA               VNM
```
 
---
 
## Logical Partition vs Physical Partition
 
Cosmos DB tách hai khái niệm này ra hoàn toàn:
 
### Logical Partition
 
Là tập hợp tất cả các items có cùng giá trị Partition Key. Ví dụ tất cả customers có `country = "USA"` tạo thành một logical partition. Bạn kiểm soát logical partition bằng cách chọn Partition Key — đây là thứ bạn thiết kế.
 
- Giới hạn tối đa **20 GB** dữ liệu mỗi logical partition.
- Tất cả items trong cùng logical partition **luôn nằm trên cùng physical partition**.
- Transactions (stored procedures, triggers) chỉ hoạt động **trong phạm vi một logical partition**.
### Physical Partition
 
Là máy chủ thực sự (node) chứa dữ liệu. Cosmos DB tự động quản lý việc ánh xạ logical → physical — bạn không cần quan tâm đến chi tiết này.
 
- Khi một physical partition đầy (**50 GB** hoặc **10,000 RU/s**), Cosmos DB tự động tách ra thêm node mới — hoàn toàn trong suốt với ứng dụng.
- Nhiều logical partition nhỏ có thể được **ghép vào cùng một physical partition**.
- Ngược lại, một logical partition quá lớn sẽ **không thể tách** — đây chính là lý do giới hạn 20 GB tồn tại.
```
Logical Partitions           Physical Partitions
                             (Cosmos DB tự quản lý)
 
┌─────────────┐
│ USA (40 GB) │ ──────────► Physical Partition 1  [USA]
└─────────────┘
 
┌─────────────┐
│ DEU (8 GB)  │ ──────────► Physical Partition 2  [DEU + VNM]
└─────────────┘              (ghép 2 partition nhỏ vào cùng node)
┌─────────────┐              ▲
│ VNM (2 GB)  │ ─────────────┘
└─────────────┘
```
 
---
 
## Hot Partition
 
Hot partition xảy ra khi **một partition nhận quá nhiều traffic** trong khi các partition khác gần như rảnh rỗi.
 
### Ví dụ thực tế
 
```
❌ BAD — Partition Key: /status
 
┌──────────────────────────────────┐   ┌────────────────────┐
│ status = "active"                │   │ status = "inactive" │
│ 99% users  →  99% traffic  🔥   │   │ 1% traffic          │
│ → Lỗi 429 (Too Many Requests)   │   │ → Gần như rảnh      │
└──────────────────────────────────┘   └────────────────────┘
 
✅ GOOD — Partition Key: /country
 
┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ USA          │  │ DEU          │  │ VNM          │  │ GBR          │
│ 30% traffic  │  │ 25% traffic  │  │ 20% traffic  │  │ 25% traffic  │
│ ✓ Balanced   │  │ ✓ Balanced   │  │ ✓ Balanced   │  │ ✓ Balanced   │
└──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘
```
 
### Triệu chứng của Hot Partition
 
- Nhận lỗi **HTTP 429 (TooManyRequests)** liên tục dù đã provision đủ RU/s.
- Latency tăng đột biến ở một số thao tác cụ thể.
- Dashboard Cosmos DB cho thấy một partition bị normalize RU consumption cao bất thường.
- Scale up throughput không giải quyết được vấn đề — vì vấn đề nằm ở thiết kế, không phải capacity.
### Nguyên nhân phổ biến
 
| Pattern sai                 | Vấn đề                                              |
|-----------------------------|-----------------------------------------------------|
| `status` ("active"/"inactive") | Cardinality quá thấp, dồn vào 1–2 partitions    |
| `isActive` (true/false)     | Chỉ có 2 giá trị                                   |
| `createdDate` (theo ngày)   | Writes liên tục dồn vào ngày hiện tại              |
| `type` ("order"/"product")  | Ít loại, một loại thường chiếm đa số              |
| Sequential ID (1, 2, 3...)  | Tất cả writes vào partition cuối cùng             |
 
---
 
## Bốn nguyên tắc chọn Partition Key
 
### 1. Cardinality cao
 
Giá trị của Partition Key phải có nhiều biến thể khác nhau. Càng nhiều giá trị duy nhất, dữ liệu càng phân tán đều.
 
```fsharp
// ✅ Cardinality cao
let byUserId   userId   = userId          // Hàng triệu giá trị duy nhất
let byCountry  country  = country.ToUpper() // ~200 quốc gia
let byCategory category = category.ToLower() // Hàng chục danh mục
 
// ❌ Cardinality thấp — tránh dùng làm Partition Key
let byStatus   status   = status          // Chỉ "active" / "inactive"
let byBoolean  (b:bool) = b.ToString()   // Chỉ "True" / "False"
let byYear     year     = year.ToString() // Chỉ ~5–10 năm
```
 
### 2. Phân phối đều
 
Cardinality cao chưa đủ — traffic và dữ liệu phải trải đều qua các partitions. Nếu 90% user đến từ một quốc gia, `country` vẫn tạo hot partition dù có nhiều giá trị.
 
```fsharp
// ✅ Đều — mỗi user có data riêng, traffic phân tán tự nhiên
let byUserId userId = userId
 
// ⚠️  Cần kiểm tra phân phối thực tế trước khi dùng
let byCountry country = country.ToUpper()
// → Tốt nếu user trải đều nhiều quốc gia
// → Xấu nếu 90% user ở cùng một quốc gia
 
// ✅ Synthetic key — tăng phân tán nhân tạo khi cần
let bySyntheticKey (userId: string) =
    let suffix = userId.GetHashCode() % 10  // tạo 10 buckets
    $"{userId}_{suffix}"
```
 
### 3. Khớp query pattern
 
Khi query có partition filter, Cosmos DB chỉ scan đúng partition đó thay vì quét toàn bộ container — tiết kiệm RU đáng kể.
 
```fsharp
// Nếu query phổ biến nhất là:
// "Lấy tất cả orders của một customer"
// → Chọn /customerId làm Partition Key
 
// ✅ Single-partition query — ~5 RU
let! orders =
    repo.QueryOrdersAsync
        "SELECT * FROM c WHERE c.customerId = @id"
        (Some "customer_abc123")
 
// ❌ Cross-partition query — ~50 RU (tốn kém hơn 10x)
let! orders =
    repo.QueryOrdersAsync
        "SELECT * FROM c WHERE c.status = 'shipped'"
        None  // Không có partition filter → quét tất cả
```
 
### 4. Không thể thay đổi
 
Đây là ràng buộc khắt khe nhất. Sau khi container được tạo, Partition Key là bất biến. Muốn đổi thì phải tạo container mới và migrate toàn bộ dữ liệu.
 
```fsharp
// Quy trình migrate nếu chọn sai Partition Key:
// 1. Tạo container mới với Partition Key đúng
// 2. Đọc toàn bộ dữ liệu từ container cũ (tốn kém)
// 3. Ghi lại vào container mới (tốn kém)
// 4. Chuyển traffic sang container mới
// 5. Xóa container cũ
//
// → Chi phí: downtime hoặc dual-write complexity + tiền RU
// → Phòng tránh: thiết kế kỹ ngay từ đầu
```
 
---
 
## Tổng hợp: Good vs Bad Partition Keys
 
| Use case                  | ❌ Tránh            | ✅ Nên dùng               |
|---------------------------|---------------------|---------------------------|
| E-commerce orders         | `/status`           | `/customerId`             |
| Social media posts        | `/isPublic`         | `/authorId`               |
| IoT sensor data           | `/deviceType`       | `/deviceId`               |
| Multi-tenant SaaS         | `/planTier`         | `/tenantId`               |
| Analytics events          | `/eventType`        | `/sessionId` hoặc `/userId` |
| Product catalog           | `/isAvailable`      | `/category` hoặc `/productId` |
| Time-series logs          | `/date` (ngày đơn lẻ) | `/yearMonth` (phân tán theo tháng) |
 
---
 
## Checklist trước khi chọn Partition Key
 
```
□ Có bao nhiêu giá trị duy nhất?  (> 100 là tốt, > 1000 là lý tưởng)
□ Phân phối traffic có đều không?  (không một giá trị nào chiếm > 20%)
□ Query phổ biến nhất có filter theo field này không?
□ Transactions có cần chạy trong cùng partition key không?
□ Giá trị có bất biến theo thời gian không?  (không nên thay đổi sau khi tạo item)
□ Đã tính đến tăng trưởng trong 2–3 năm tới chưa?
```

---

## 🚀 CRUD Operations
```
dotnet add package Microsoft.Azure.Cosmos
dotnet add package Newtonsoft.Json --version 13.0.3
dotnet add package FSharp.Control.AsyncSeq
```
# Cosmos DB — Thiết kế Library Catalog (Books + Categories)

## 2 containers
 
```
LibraryDB
  ├── books         Partition Key: /category
  └── categories    Partition Key: /id
```

## Container 1: `books`
 
### Partition Key: `/category`
 
Lý do chọn `/category`:
- Query phổ biến nhất là "lấy sách theo thể loại" → single partition, rẻ
- Point read sách cần **ISBN + category** → đủ để định vị chính xác
- Cardinality ~20–50 thể loại → đủ để phân tán đều vài nghìn sách
### Document schema
 
```json
{
  "id"           : "978-604-0-12345-6",
  "category"     : "van-hoc",
  "categoryName" : "Văn học",
  "title"        : "Số đỏ",
  "author"       : "Vũ Trọng Phụng",
  "publishYear"  : 1936,
  "language"     : "vi",
  "pageCount"    : 300,
  "publisher"    : "NXB Hội Nhà Văn",
  "description"  : "...",
  "coverUrl"     : "https://...",
  "tags"         : ["satire", "classic", "vietnam"],
  "isAvailable"  : true,
  "createdAt"    : "2024-01-15T00:00:00Z"
}
```
 
**Lưu ý thiết kế:**
- `category` (slug: `"van-hoc"`) = Partition Key — dùng slug thay tên tiếng Việt để tránh encoding issues
- `categoryName` (tên hiển thị: `"Văn học"`) = embed để tránh join khi hiển thị danh sách sách
- `author` embed dưới dạng string — với catalog đơn giản không cần tách container Author riêng

### F# type
 
```fsharp
[<CLIMutable>]
type Book = {
    id           : string       // ISBN: "978-604-0-12345-6"
    category     : string       // Partition Key (slug): "van-hoc"
    categoryName : string       // Tên hiển thị: "Văn học" — embed để tránh JOIN
    title        : string
    author       : string       // Embed đơn giản — catalog nhỏ không cần tách
    publishYear  : int
    language     : string       // "vi", "en", "fr"...
    pageCount    : int
    publisher    : string
    description  : string
    coverUrl     : string option
    tags         : string[]
    isAvailable  : bool
    createdAt    : DateTime
}
```
 
### Logical partitions
 
```
books container
  ├── "van-hoc"    → tất cả sách Văn học
  ├── "khoa-hoc"   → tất cả sách Khoa học
  ├── "lich-su"    → tất cả sách Lịch sử
  ├── "thieu-nhi"  → tất cả sách Thiếu nhi
  └── ...
```
 
---
 
## Container 2: `categories`
 
### Partition Key: `/id`
 
Lý do chọn `/id`:
- Mỗi category là 1 document độc lập → `id` làm Partition Key = point read với 1 RU
- Số lượng categories ít (20–50) → không cần partition key phức tạp hơn
### Document schema
 
```json
{
  "id"          : "van-hoc",
  "name"        : "Văn học",
  "description" : "Sách văn học trong và ngoài nước",
  "bookCount"   : 142,
  "isActive"    : true,
  "sortOrder"   : 1,
  "createdAt"   : "2024-01-01T00:00:00Z"
}
```
 
**Lưu ý thiết kế:**
- `id` = slug (`"van-hoc"`) — trùng với giá trị Partition Key trong `books` container → dễ reference
- `bookCount` denormalize để tránh aggregate query mỗi lần hiển thị menu
### F# type
 
```fsharp
[<CLIMutable>]
type Category = {
    id          : string    // Partition Key (slug): "van-hoc"
    name        : string    // Tên hiển thị: "Văn học"
    description : string
    bookCount   : int       // Denormalize — update khi thêm/xóa sách
    isActive    : bool
    sortOrder   : int       // Thứ tự hiển thị trên menu
    createdAt   : DateTime
}
```
 
---

## Mối quan hệ giữa 2 containers
 
Cosmos DB không có foreign key. Thay vào đó dùng **reference by value**:
 
```
categories.id = "van-hoc"
              ↕ reference (không phải foreign key — app tự join nếu cần)
books.category = "van-hoc"
```
 
Khi **thêm sách mới** → tăng `bookCount` trong category tương ứng:
 
```fsharp
// Sau CreateItemAsync cho book mới:
let patchOps = [ PatchOperation.Increment("/bookCount", 1) ]
do! categoriesContainer.PatchItemAsync(categoryId, PartitionKey(categoryId), patchOps)
```
 
Khi **xóa sách** → giảm `bookCount`:
 
```fsharp
let patchOps = [ PatchOperation.Increment("/bookCount", -1) ]
do! categoriesContainer.PatchItemAsync(categoryId, PartitionKey(categoryId), patchOps)
```
 
---

---

## 🎯 Best Practices

### 🔹 Thiết kế Partition Key

```fsharp
// ✅ TỐT: High cardinality, phân tán đều
let byCountry     country   = country.ToUpper()         // ~200 giá trị
let byYearMonth   (d: DateTime) = d.ToString("yyyy-MM") // nhiều tháng
let byCategory    category  = category.ToLower()        // nhiều danh mục

// ❌ XẤU: Low cardinality → hot partition
let byStatus      status    = status                    // chỉ "active"/"inactive"
let byBoolean     (b: bool) = b.ToString()             // chỉ "True"/"False"
let byConstant    ()        = "global"                  // tất cả cùng partition
```

### 🔹 Indexing Policy

```json
{
  "indexingMode": "consistent",
  "automatic": true,
  "includedPaths": [
    { "path": "/*" }
  ],
  "excludedPaths": [
    { "path": "/rawPayload/*" },
    { "path": "/largeBlob/*" }
  ],
  "compositeIndexes": [
    [
      { "path": "/country",     "order": "ascending"  },
      { "path": "/loyaltyTier", "order": "ascending"  },
      { "path": "/totalSpent",  "order": "descending" }
    ]
  ]
}
```

> 💡 Loại trừ các field lớn không cần query khỏi index để tiết kiệm RU và storage.

### 🔹 Error Handling toàn diện

```fsharp
let handleCosmosError (operation: string) (ex: exn) =
    match ex with
    | :? CosmosException as e ->
        match e.StatusCode with
        | HttpStatusCode.NotFound ->
            Error $"[{operation}] Item không tồn tại"
        | HttpStatusCode.Conflict ->
            Error $"[{operation}] Item đã tồn tại (conflict)"
        | HttpStatusCode.TooManyRequests ->
            Error $"[{operation}] Rate limited (429). Retry after: {e.RetryAfter}"
        | HttpStatusCode.RequestEntityTooLarge ->
            Error $"[{operation}] Item vượt giới hạn 2MB"
        | HttpStatusCode.PreconditionFailed ->
            Error $"[{operation}] Optimistic concurrency conflict (etag mismatch)"
        | HttpStatusCode.ServiceUnavailable ->
            Error $"[{operation}] Cosmos DB tạm thời không khả dụng"
        | code ->
            Error $"[{operation}] Cosmos error ({code}): {e.Message}"
    | ex ->
        Error $"[{operation}] Lỗi không xác định: {ex.Message}"
```

### 🔹 Optimistic Concurrency với ETag

```fsharp
// Dùng _etag để tránh ghi đè dữ liệu đồng thời
member this.UpdateCustomerSafeAsync (customer: Customer) =
    task {
        match customer._etag with
        | None -> return Error "❌ Cần có _etag để update an toàn"
        | Some etag ->
            let partitionKey = PartitionKey(PartitionKeys.customerPartition customer.country)
            let options = ItemRequestOptions()
            options.IfMatchEtag <- etag  // Chỉ update nếu etag khớp

            try
                let! response = customerContainer.ReplaceItemAsync(customer, customer.id, partitionKey, options)
                return Ok response.Resource
            with
            | :? CosmosException as ex when ex.StatusCode = HttpStatusCode.PreconditionFailed ->
                return Error "❌ Dữ liệu đã bị thay đổi bởi người khác — cần đọc lại"
    }
```

---

## 📊 Performance Monitoring

```fsharp
// Theo dõi RU consumption sau mỗi thao tác
let logPerformance (operationName: string) (requestCharge: float) (diagnostics: CosmosDiagnostics) =
    printfn "📊 [%s]" operationName
    printfn "   RU consumed : %.2f" requestCharge
    printfn "   Duration    : %s" (diagnostics.GetClientElapsedTime().ToString())

// Áp dụng:
let! response = customerContainer.CreateItemAsync(customer, partitionKey)
logPerformance "CreateCustomer" response.RequestCharge response.Diagnostics
```

### Tham khảo chi phí RU

| Thao tác                    | Chi phí RU ước tính  |
|-----------------------------|----------------------|
| ReadItemAsync (point read)  | ~1 RU                |
| CreateItemAsync (1KB item)  | ~5 RU                |
| UpsertItemAsync (1KB item)  | ~5–15 RU             |
| PatchItemAsync              | ~2–5 RU              |
| DeleteItemAsync             | ~5 RU                |
| Query (single partition)    | Tùy số kết quả       |
| Query (cross-partition)     | Cao hơn đáng kể      |

---

## 🏃‍♂️ Chạy Demo

### Yêu cầu

- .NET 8.0 SDK
- Azure Cosmos DB Emulator hoặc Azure Cosmos DB Account

### Cài đặt Emulator bằng Docker

```bash
docker run -d \
  --name cosmosdb-emulator \
  -p 8081:8081 \
  -p 10251-10254:10251-10254 \
  -e AZURE_COSMOS_EMULATOR_PARTITION_COUNT=10 \
  -e AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=true \
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
```

hoặc 

```bash
docker compose up -d
```

Enable https localhost
```
curl -k https://localhost:8081/_explorer/emulator.pem -o emulator.pem
sudo cp emulator.pem /usr/local/share/ca-certificates/cosmos-emulator.crt
sudo update-ca-certificates
```

### Cấu hình connection

```fsharp
// Local Emulator (mặc định)
let emulatorConfig = {
    EndpointUrl  = "https://localhost:8081/"
    PrimaryKey   = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
    DatabaseId   = "LibraryDatabase"
    BooksId      = "books"
    CategoriesId = "categories"
}

// Azure Production
let productionConfig = {
    EndpointUrl  = "https://YOUR_ACCOUNT.documents.azure.com:443/"
    PrimaryKey   = "YOUR_PRIMARY_KEY"
    DatabaseId   = "LibraryDatabase"
    BooksId      = "books"
    CategoriesId = "categories"
}
```

### Chạy

```bash
cd CosmosDB-FSharp-Demo
dotnet restore
dotnet run
```

### Demo sẽ thực hiện

| Bước | Thao tác               | Mô tả                                          |
|------|------------------------|------------------------------------------------|
| ✅   | **CREATE**             | Tạo customers, products, orders                |
| ✅   | **READ**               | Point reads bằng ID + Partition Key            |
| ✅   | **UPSERT**             | Tạo hoặc cập nhật tùy trường hợp              |
| ✅   | **PATCH**              | Cập nhật một số fields cụ thể                  |
| ✅   | **DELETE**             | Xóa item an toàn với error handling            |
| ✅   | **QUERY**              | SQL queries trong và cross-partition           |
| ✅   | **BATCH**              | Xử lý dataset lớn theo async sequence          |
| ✅   | **AGGREGATION**        | Thống kê, phân tích dữ liệu                   |
| ✅   | **CONCURRENCY**        | Optimistic locking với ETag                    |

---

## 🎯 Key Takeaways

1. **Partition Key** là yếu tố quan trọng nhất — ảnh hưởng đến performance, scale, và chi phí. Không thể thay đổi sau khi tạo.
2. **Point read** (`ReadItemAsync`) là thao tác nhanh nhất và rẻ nhất (~1 RU) — dùng khi biết ID + Partition Key.
3. **Cross-partition query** tốn kém hơn single-partition — thiết kế queries để luôn có partition filter.
4. **PatchItemAsync** hiệu quả hơn replace toàn bộ item khi chỉ cần update một vài fields.
5. **Async sequences** phù hợp xử lý dataset lớn mà không gây memory pressure.
6. **ETag** giúp tránh lost update trong môi trường concurrent.
7. **Indexing policy** nên loại trừ các field lớn không cần query để giảm RU và storage.

---

## 🔗 Tài liệu tham khảo

- [Azure Cosmos DB Documentation](https://docs.microsoft.com/azure/cosmos-db/)
- [Cosmos DB .NET SDK v3](https://docs.microsoft.com/azure/cosmos-db/sql/sql-api-sdk-dotnet-standard)
- [Partition Key Design Guide](https://docs.microsoft.com/azure/cosmos-db/partitioning-overview)
- [Request Units (RU) explained](https://docs.microsoft.com/azure/cosmos-db/request-units)
- [Query Optimization](https://docs.microsoft.com/azure/cosmos-db/sql/how-to-sql-query)
- [Indexing Policy](https://docs.microsoft.com/azure/cosmos-db/index-policy)
- [Best Practices .NET SDK](https://docs.microsoft.com/azure/cosmos-db/sql/best-practice-dotnet)