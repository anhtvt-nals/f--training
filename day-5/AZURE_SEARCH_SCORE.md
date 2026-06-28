# Azure AI Search — Indexing, Search & Scoring (F#)

> Tài liệu tham khảo về cách upload dữ liệu vào index, thực hiện search queries với các tùy chọn nâng cao, đọc relevance score, và tùy chỉnh ranking theo nghiệp vụ bằng ScoringProfile.

---

## 1. IndexDocumentsBatch — 4 Actions

Mọi thao tác ghi dữ liệu vào Azure Search đều thông qua `IndexDocumentsBatch`, cho phép gộp nhiều documents vào một request.

### 1.1 Bảng tóm tắt 4 actions

| Action | Hành vi | Document tồn tại | Document chưa tồn tại |
|---|---|---|---|
| `Upload` | Insert hoặc replace toàn bộ | Thay thế hoàn toàn | Tạo mới |
| `Merge` | Cập nhật chỉ các fields được gửi | Merge fields | **Lỗi 404** |
| `MergeOrUpload` | Merge nếu tồn tại, Upload nếu chưa | Merge fields | Tạo mới |
| `Delete` | Xóa document theo key | Xóa | Không làm gì (idempotent) |

### 1.2 Upload — Insert hoặc Replace hoàn toàn

```fsharp
open Azure.Search.Documents
open Azure.Search.Documents.Models

// Upload: nếu document đã tồn tại → toàn bộ fields bị THAY THẾ
// Các fields không có trong object mới sẽ bị SET VỀ NULL
let uploadDocuments (client: SearchClient) (docs: HotelDocument list) =
    let batch = IndexDocumentsBatch.Upload(docs)
    let result = client.IndexDocuments(batch)
    result.Value.Results
    |> Seq.iter (fun r ->
        if not r.Succeeded then
            eprintfn "Failed to upload %s: %s" r.Key r.ErrorMessage)

// ⚠️  Gotcha: Upload không phải upsert thông minh
// Document cũ:  { Id="h1", Name="Old Hotel", Rating=4.5, Category="Resort" }
// Upload mới:   { Id="h1", Name="New Hotel", Rating=null, Category=null }
// Kết quả:      { Id="h1", Name="New Hotel", Rating=null, Category=null }
//                                                          ↑ Category bị mất!
```

### 1.3 Merge — Cập nhật một phần (partial update)

```fsharp
// Merge: chỉ cập nhật fields có trong object, giữ nguyên fields còn lại
// ⚠️  Nếu document KHÔNG tồn tại → throw IndexBatchException với status 404

// Tạo partial object chỉ với fields cần update
[<CLIMutable>]
type HotelPartialUpdate =
    {
        [<SimpleField(IsKey = true)>]
        Id: string
        // Chỉ update Rating, các fields khác giữ nguyên
        [<SimpleField(IsFilterable = true, IsSortable = true)>]
        Rating: Nullable<float>
    }

let mergeDocuments (client: SearchClient) =
    let updates = [
        { Id = "h001"; Rating = Nullable 4.8 }
        { Id = "h002"; Rating = Nullable 3.9 }
    ]
    let batch = IndexDocumentsBatch.Merge(updates)
    client.IndexDocuments(batch) |> ignore

// Kết quả:
// Document h001 trước: { Id="h001", Name="Grand Hotel", Rating=4.5, Category="Resort" }
// Sau merge:           { Id="h001", Name="Grand Hotel", Rating=4.8, Category="Resort" }
//                                                              ↑ chỉ Rating thay đổi
```

### 1.4 MergeOrUpload — Upsert (khuyến nghị dùng mặc định)

```fsharp
// MergeOrUpload = Upsert thông minh
// - Đã tồn tại → Merge (partial update)
// - Chưa tồn tại → Upload (insert mới)
// Đây là action phổ biến nhất cho pipeline sync dữ liệu

let upsertDocuments (client: SearchClient) (docs: HotelDocument list) =
    let batch = IndexDocumentsBatch.MergeOrUpload(docs)
    client.IndexDocuments(batch) |> ignore

// Dùng cho: ETL pipeline, data sync từ database, incremental indexing
```

### 1.5 Delete — Xóa theo key

```fsharp
// Delete theo key field — không cần gửi toàn bộ document
// Idempotent: xóa document không tồn tại → không lỗi

let deleteDocuments (client: SearchClient) (ids: string list) =
    // Tham số 1: tên key field; Tham số 2: danh sách key values
    let batch = IndexDocumentsBatch.Delete("Id", ids)
    client.IndexDocuments(batch) |> ignore

// Hoặc xóa từ list document objects
let deleteByObjects (client: SearchClient) (docs: HotelDocument list) =
    let batch = IndexDocumentsBatch.Delete(docs)
    client.IndexDocuments(batch) |> ignore
```

### 1.6 Mix actions trong một batch

```fsharp
// Một batch có thể chứa nhiều loại action khác nhau
let mixedBatch (client: SearchClient) =
    let batch = IndexDocumentsBatch<HotelDocument>()

    // Thêm từng action
    batch.Actions.Add(IndexDocumentsAction.Upload(newHotel))
    batch.Actions.Add(IndexDocumentsAction.MergeOrUpload(updatedHotel))
    batch.Actions.Add(IndexDocumentsAction.Delete(obsoleteHotel))
    batch.Actions.Add(IndexDocumentsAction.Merge(partialUpdate))

    client.IndexDocuments(batch) |> ignore
```

---

## 2. Giới hạn Batch và Xử lý IndexBatchException

### 2.1 Hard limits của Azure Search

| Giới hạn | Giá trị | Ghi chú |
|---|---|---|
| Documents per batch | **1,000** | Hard limit — vượt quá sẽ throw |
| Payload size per batch | **16 MB** | Tổng size của request body |
| Document size tối đa | **16 MB** | Một document đơn lẻ |
| Key field length | **1,024 ký tự** | |

```fsharp
// Chunking helper — tự động chia nhỏ nếu vượt 1000 docs
let chunkBatches (batchSize: int) (docs: 'a list) =
    docs
    |> List.chunkBySize (min batchSize 1000)

let indexInBatches (client: SearchClient) (docs: HotelDocument list) =
    docs
    |> chunkBatches 500  // Dùng 500 để an toàn về size
    |> List.iter (fun chunk ->
        let batch = IndexDocumentsBatch.MergeOrUpload(chunk)
        client.IndexDocuments(batch) |> ignore)
```

### 2.2 IndexBatchException — Per-document failure

Azure Search **không fail cả batch** khi một số documents có lỗi. Thay vào đó, nó throw `IndexBatchException` chứa kết quả của từng document.

```
Batch: [doc1 ✅, doc2 ❌, doc3 ✅, doc4 ❌, doc5 ✅]
         ↓
Azure xử lý tất cả 5 documents
doc1, doc3, doc5 → indexed thành công
doc2, doc4       → có lỗi riêng

→ Throw IndexBatchException
  .IndexingResults chứa status của cả 5 documents
```

### 2.3 Xử lý IndexBatchException đúng cách

```fsharp
open Azure
open Azure.Search.Documents
open Azure.Search.Documents.Models

let indexWithErrorHandling (client: SearchClient) (docs: HotelDocument list) =
    let batch = IndexDocumentsBatch.MergeOrUpload(docs)
    try
        let result = client.IndexDocuments(batch)
        // Nếu không throw, tất cả thành công
        printfn "All %d documents indexed successfully" docs.Length

    with
    | :? RequestFailedException as ex ->
        // Lỗi toàn bộ request (network, auth, service unavailable)
        eprintfn "Request failed: %d %s" ex.Status ex.Message

    | :? IndexBatchException as batchEx ->
        // Một số documents thất bại — xử lý per-document
        let succeeded, failed =
            batchEx.IndexingResults
            |> Seq.partition (fun r -> r.Succeeded)

        printfn "✅ Succeeded: %d" (Seq.length succeeded)
        printfn "❌ Failed:    %d" (Seq.length failed)

        failed |> Seq.iter (fun r ->
            eprintfn "  Key: %-20s | Status: %d | Error: %s"
                r.Key r.Status r.ErrorMessage)

        // Retry chỉ các documents thất bại
        let failedKeys = failed |> Seq.map (fun r -> r.Key) |> Set.ofSeq
        let docsToRetry =
            docs |> List.filter (fun d -> failedKeys.Contains(d.Id))

        if not docsToRetry.IsEmpty then
            printfn "Retrying %d failed documents..." docsToRetry.Length
            indexWithErrorHandling client docsToRetry  // Retry recursively


// Async version
let indexWithErrorHandlingAsync (client: SearchClient) (docs: HotelDocument list) = task {
    let batch = IndexDocumentsBatch.MergeOrUpload(docs)
    try
        let! _ = client.IndexDocumentsAsync(batch)
        ()
    with
    | :? IndexBatchException as ex ->
        let failed =
            ex.IndexingResults
            |> Seq.filter (fun r -> not r.Succeeded)
            |> Seq.toList

        eprintfn "Batch partial failure: %d/%d documents failed"
            failed.Length (ex.IndexingResults.Count)

        // Log chi tiết để investigate
        failed |> List.iter (fun r ->
            eprintfn "  [%d] Key=%s: %s" r.Status r.Key r.ErrorMessage)
}
```

### 2.4 Status codes trong IndexingResult

| Status | Ý nghĩa | Hành động |
|---|---|---|
| `200` | Merge thành công | — |
| `201` | Upload (tạo mới) thành công | — |
| `400` | Document không hợp lệ (sai kiểu, missing key) | Fix data, không retry |
| `404` | Merge vào document không tồn tại | Dùng MergeOrUpload thay thế |
| `409` | Conflict (concurrent update) | Retry với backoff |
| `422` | Document quá lớn | Chia nhỏ document |
| `503` | Service tạm thời bận | Retry với exponential backoff |

```fsharp
// Phân loại lỗi để retry thông minh
let classifyError (status: int) =
    match status with
    | 400 | 404 | 422 -> "permanent"  // Không retry — fix data trước
    | 409 | 503       -> "transient"  // Retry được
    | _               -> "unknown"
```

---

## 3. Search — SearchText, SearchMode và SearchOptions

### 3.1 SearchText và cú pháp query

```fsharp
open Azure.Search.Documents
open Azure.Search.Documents.Models

let client: SearchClient = // ... khởi tạo

// Search tất cả documents (wildcard)
let searchAll () =
    client.Search<HotelDocument>("*")

// Simple search — tìm từ khóa
let searchSimple () =
    client.Search<HotelDocument>("khách sạn hồ bơi")

// Quoted phrase — exact phrase match
let searchPhrase () =
    client.Search<HotelDocument>("\"hồ bơi vô cực\"")

// Wildcard — prefix match
let searchWildcard () =
    client.Search<HotelDocument>("metro*")    // Khớp: metropolis, metropolitan

// Fuzzy search — tìm gần đúng (edit distance)
let searchFuzzy () =
    client.Search<HotelDocument>("hotell~")   // Khớp: hotel (1 edit distance)
    client.Search<HotelDocument>("hotell~2")  // Khớp: hotel, hosel (2 edit distance)

// Boolean operators
let searchBoolean () =
    client.Search<HotelDocument>("hotel AND spa")
    client.Search<HotelDocument>("resort OR hotel")
    client.Search<HotelDocument>("hotel NOT motel")
    client.Search<HotelDocument>("hotel +(spa | pool) -motel")

// Field-scoped query (Lucene syntax)
let searchFieldScoped () =
    client.Search<HotelDocument>("Name:metropolis AND Category:resort")

// Boosting term — tăng relevance của từ cụ thể
let searchBoosted () =
    client.Search<HotelDocument>("hotel^3 spa")  // "hotel" được weight x3
```

### 3.2 SearchMode — Any vs All

`SearchMode` quyết định cách kết hợp nhiều từ trong query.

```fsharp
let options = SearchOptions()

// SearchMode.Any (mặc định) — OR logic
// Document phải chứa ÍT NHẤT MỘT trong các từ
// → Recall cao, Precision thấp
options.SearchMode <- SearchMode.Any
// Query: "khách sạn hồ bơi"
// Khớp: document có "khách sạn" ĐỦ để match
// Khớp: document có "hồ bơi" ĐỦ để match
// Ranking: document có cả hai được score cao hơn

// SearchMode.All — AND logic
// Document phải chứa TẤT CẢ các từ
// → Recall thấp, Precision cao
options.SearchMode <- SearchMode.All
// Query: "khách sạn hồ bơi"
// Chỉ khớp: document có CẢ "khách", "sạn", "hồ", "bơi"
// (Tuỳ tokenizer — standard tách thành 4 token riêng)
```

**Hướng dẫn chọn SearchMode:**

| Tình huống | SearchMode | Lý do |
|---|---|---|
| Search box tổng quát (như Google) | `Any` | User muốn tìm thấy kết quả dù chỉ match một phần |
| Filter chính xác theo nhiều tiêu chí | `All` | Mọi từ đều quan trọng |
| Full-text search + `$filter` | `Any` + filter | Filter xử lý điều kiện bắt buộc, SearchMode lo về relevance |
| Autocomplete / suggest | `Any` | Partial input |

### 3.3 SearchOptions — Toàn bộ tùy chọn

```fsharp
let buildSearchOptions () =
    let options = SearchOptions()

    // ── Scope: tìm ở những fields nào ──────────────────────────────
    // Mặc định: tìm trong TẤT CẢ IsSearchable fields
    options.SearchFields.Add("Name")           // Chỉ tìm trong Name
    options.SearchFields.Add("Description")    // và Description

    // ── Projection: chỉ trả về fields cần thiết ────────────────────
    // Giảm payload size — quan trọng khi document có nhiều fields
    options.Select.Add("Id")
    options.Select.Add("Name")
    options.Select.Add("Rating")
    options.Select.Add("Category")
    // Description (có thể lớn) không cần trả về trong list view

    // ── Filtering: điều kiện bắt buộc (không ảnh hưởng score) ──────
    options.Filter <- "Rating gt 3.5 and Category eq 'Resort'"
    // OData filter syntax:
    //   eq, ne, gt, ge, lt, le (so sánh)
    //   and, or, not (logic)
    //   search.in(Category, 'Resort,Boutique', ',') (IN operator)

    // ── Sorting: thứ tự kết quả ─────────────────────────────────────
    // Khi có OrderBy → score không còn là tiêu chí sort mặc định
    options.OrderBy.Add("Rating desc")
    options.OrderBy.Add("Name asc")
    // Đặc biệt: dùng "search.score() desc" để sort theo score + field khác
    options.OrderBy.Add("search.score() desc")
    options.OrderBy.Add("LastUpdated desc")

    // ── Pagination ──────────────────────────────────────────────────
    options.Size <- 20          // Số kết quả trả về (mặc định: 50, max: 1000)
    options.Skip <- 40          // Bỏ qua N kết quả đầu (page 3 = skip 40)
    // Page 1: Skip=0,  Size=20
    // Page 2: Skip=20, Size=20
    // Page 3: Skip=40, Size=20

    // ── Total count ─────────────────────────────────────────────────
    options.IncludeTotalCount <- true
    // Response.TotalCount sẽ có giá trị (tốn thêm ~5% latency)

    // ── Highlighting: đánh dấu từ khóa trong kết quả ───────────────
    options.HighlightFields.Add("Description")   // Field cần highlight
    options.HighlightPreTag  <- "<em>"           // Tag bọc từ khóa (mặc định: <em>)
    options.HighlightPostTag <- "</em>"
    // Response: r.Highlights["Description"] = ["...khách <em>sạn</em>..."]

    // ── Facets: thống kê phân nhóm ──────────────────────────────────
    options.Facets.Add("Category")                         // Đếm theo Category
    options.Facets.Add("Rating,interval:1")                // Histogram theo khoảng 1
    options.Facets.Add("LastUpdated,interval:7day")        // Histogram theo 7 ngày
    options.Facets.Add("Category,count:5,sort:count")      // Top 5, sort by count

    // ── SearchMode ──────────────────────────────────────────────────
    options.SearchMode <- SearchMode.Any

    // ── Scoring Profile ─────────────────────────────────────────────
    options.ScoringProfile <- "freshness-boost"  // Xem Section 5

    options


// Thực hiện search và đọc kết quả đầy đủ
let searchFull (client: SearchClient) (query: string) = task {
    let options = buildSearchOptions()
    let! response = client.SearchAsync<HotelDocument>(query, options)
    let value = response.Value

    // Total count (chỉ có nếu IncludeTotalCount = true)
    printfn "Total results: %d" (value.TotalCount |> Option.ofNullable |> Option.defaultValue 0L)

    // Facets
    if value.Facets <> null then
        for kvp in value.Facets do
            printfn "Facet: %s" kvp.Key
            kvp.Value |> Seq.iter (fun f ->
                printfn "  %s: %d" (string f.Value) f.Count.GetValueOrDefault())

    // Documents
    let results =
        value.GetResults()
        |> Seq.map (fun r ->
            let score     = r.Score                      // @search.score
            let doc       = r.Document
            let highlights = r.Highlights                // null nếu không có highlight
            (score, doc, highlights))
        |> Seq.toList

    return results
}
```

---

## 4. `@search.score` — Đọc và Hiểu Relevance Score

### 4.1 Score là gì?

`@search.score` là điểm **relevance** Azure Search tính cho mỗi document với query cụ thể. Score **không có đơn vị** và chỉ có ý nghĩa khi so sánh tương đối giữa các kết quả trong cùng một query.

```fsharp
// Đọc score từ SearchResult
let printScores (client: SearchClient) (query: string) =
    let response = client.Search<HotelDocument>(query)
    response.Value.GetResults()
    |> Seq.iter (fun r ->
        printfn "Score: %8.4f | Name: %s" r.Score r.Document.Name)

// Output ví dụ:
// Score:   12.4832 | Name: Metropolis Hotel & Spa
// Score:    8.1234 | Name: Grand Spa Resort
// Score:    3.2100 | Name: Cozy Hotel
// Score:    1.0050 | Name: Airport Motel
```

### 4.2 Cách Azure tính score — TF-IDF + BM25

Azure Search dùng thuật toán **BM25** (Okapi BM25), nâng cấp của TF-IDF:

```
Score(document, query) được quyết định bởi:

1. Term Frequency (TF) — từ khóa xuất hiện nhiều lần trong document → score cao hơn
   "hotel spa" document có "spa" x5 lần → score cao hơn document có "spa" x1 lần

2. Inverse Document Frequency (IDF) — từ khóa hiếm trong toàn index → score cao hơn
   Nếu 90% documents đều có từ "hotel" → "hotel" ít có giá trị phân biệt
   Nếu chỉ 2% documents có "rooftop pool" → term này rất có giá trị

3. Field Length Normalization — document ngắn có cùng TF thì score cao hơn document dài
   "hotel" trong Description 10 từ → score cao hơn Description 500 từ

BM25 cải tiến TF-IDF bằng cách giới hạn ảnh hưởng của TF (saturation)
→ Tránh spam từ khóa để game score
```

### 4.3 Khi nào score quan trọng và không quan trọng

| Tình huống | Score quan trọng? | Ghi chú |
|---|---|---|
| Search box tổng quát | ✅ Rất quan trọng | Sort mặc định theo score |
| Filter + sort theo giá | ❌ Không quan trọng | Dùng `OrderBy` thay thế |
| Autocomplete | ❌ Không | Chỉ cần match, không cần rank |
| Semantic search | ⚠️ Khác biệt | Dùng `@search.reranker_score` |
| Faceted browsing | ❌ Tùy | User tự sort theo filter |

### 4.4 Debugging score với `featuresMode`

```fsharp
// Bật verbose scoring để debug tại sao document được score cao/thấp
let debugScoring (client: SearchClient) (query: string) = task {
    let options = SearchOptions()
    options.IncludeTotalCount <- true

    // Chế độ debug — chỉ dùng trong development, không dùng production
    // Trả về breakdown chi tiết của score
    let! response = client.SearchAsync<HotelDocument>(query, options)

    response.Value.GetResults()
    |> Seq.take 3
    |> Seq.iter (fun r ->
        printfn "\n=== %s ===" r.Document.Name
        printfn "Final Score: %.4f" r.Score)
}
```

### 4.5 Score vs các loại score khác

```fsharp
// @search.score    — BM25 relevance score (luôn có)
// @search.reranker_score — Semantic ranking score (nếu bật Semantic Search)
// @search.captions — Extracted passages (Semantic Search)

// Đọc reranker score (khi dùng Semantic Search)
let readSemanticScores (client: SearchClient) (query: string) = task {
    let options = SearchOptions()
    options.QueryType <- SearchQueryType.Semantic

    let! response = client.SearchAsync<HotelDocument>(query, options)
    response.Value.GetResults()
    |> Seq.iter (fun r ->
        // Score là BM25 score, SemanticSearch.RerankerScore là semantic score
        let rerankerScore =
            if r.SemanticSearch <> null
            then r.SemanticSearch.RerankerScore |> Option.ofNullable
            else None
        printfn "BM25: %.3f | Semantic: %A | %s"
            r.Score rerankerScore r.Document.Name)
}
```

---

## 5. ScoringProfile — Tùy chỉnh Ranking theo Nghiệp vụ

`ScoringProfile` cho phép điều chỉnh score theo logic nghiệp vụ, ví dụ: nội dung mới hơn → score cao hơn, đánh giá cao hơn → nổi bật hơn.

### 5.1 Cấu trúc ScoringProfile

```
ScoringProfile
├── FieldWeights   — Nhân hệ số cho score của một field cụ thể
└── Functions[]    — Áp dụng bonus score dựa trên giá trị field
    ├── FreshnessScoringFunction   — Ưu tiên document mới hơn
    ├── MagnitudeScoringFunction   — Ưu tiên document có giá trị cao/thấp hơn
    ├── DistanceScoringFunction    — Ưu tiên document gần vị trí địa lý hơn
    └── TagScoringFunction         — Ưu tiên document khớp tag của user
```

### 5.2 FieldWeights — Boost field quan trọng hơn

```fsharp
open Azure.Search.Documents.Indexes.Models

// FieldWeights nhân BM25 score của từng field với hệ số boost
// Score cuối = Σ (BM25_score_field_i × weight_i)

let createFieldWeightProfile () =
    let profile = ScoringProfile("field-importance")

    // Name match quan trọng hơn Description match 3 lần
    // Description quan trọng hơn Tags 2 lần
    profile.TextWeights <- TextWeights(dict [
        "Name",        5.0   // Từ khóa trong Name → score x5
        "Description", 2.0   // Từ khóa trong Description → score x2
        "Category",    1.5   // Từ khóa trong Category → score x1.5
        // Tags: không khai báo → weight = 1.0 (mặc định)
    ])

    profile

// Ví dụ:
// Query: "spa resort"
// Document A: Name="Spa Resort" → BM25=10, sau weight → 10×5 = 50
// Document B: Name="Hotel", Description="luxury spa resort" → BM25=8, sau weight → 8×2 = 16
// → Document A được rank cao hơn dù BM25 gốc có thể thấp hơn
```

### 5.3 FreshnessScoringFunction — Ưu tiên nội dung mới

```fsharp
// Tính bonus score dựa trên khoảng cách từ field DateTimeOffset đến hiện tại
// Document càng mới → bonus càng cao

let createFreshnessProfile () =
    let profile = ScoringProfile("freshness-boost")

    let freshness = FreshnessScoringFunction(
        "LastUpdated",         // Field DateTimeOffset cần dùng
        boost = 5.0,           // Nhân tối đa x5 cho document mới nhất
        FreshnessScoringParameters(TimeSpan.FromDays(30.0))  // "Mới" = trong 30 ngày
    )
    // Decay: document cũ hơn 30 ngày → bonus giảm dần về 1.0

    freshness.Interpolation <- ScoringFunctionInterpolation.Logarithmic
    // Quadratic: giảm nhanh ban đầu, chậm dần
    // Logarithmic: giảm chậm ban đầu, nhanh dần (thường dùng nhất)
    // Linear: giảm đều đặn
    // Constant: không giảm (bonus cố định nếu trong khoảng)

    profile.Functions.Add(freshness)
    profile
```

**Decay interpolation visualization:**

```
Boost x5 ─┐
           │▓▓▓ (Linear)
           │▓▓▓▓▓▓ (Logarithmic)
           │▓▓▓▓▓▓▓▓▓▓ (Quadratic)
Boost x1 ──┴──────────────→ Thời gian (30 ngày)
           Mới             Cũ
```

### 5.4 MagnitudeScoringFunction — Ưu tiên theo giá trị số

```fsharp
// Boost score dựa trên giá trị của field số (Int32, Double)
// Ví dụ: Rating cao → score cao; Price thấp → score cao

let createMagnitudeProfile () =
    let profile = ScoringProfile("rating-boost")

    // Boost theo Rating: Rating cao → score cao
    let ratingBoost = MagnitudeScoringFunction(
        "Rating",             // Field số cần dùng
        boost = 3.0,          // Boost tối đa x3
        MagnitudeScoringParameters(
            boostingRangeStart = 1.0,   // Rating từ 1 trở lên bắt đầu boost
            boostingRangeEnd   = 5.0    // Rating 5.0 được boost tối đa
        )
    )
    ratingBoost.Interpolation <- ScoringFunctionInterpolation.Linear

    // Tùy chọn: boost ngược (giá trị THẤP hơn được boost nhiều hơn)
    // Dùng cho: Price (giá rẻ ưu tiên hơn), Distance (gần ưu tiên hơn)
    let priceBoost = MagnitudeScoringFunction(
        "PricePerNight",
        boost = 2.0,
        MagnitudeScoringParameters(
            boostingRangeStart   = 0.0,
            boostingRangeEnd     = 500.0,
            shouldBoostBeyondRangeByConstant = false
        )
    )
    priceBoost.Parameters.BoostingRangeEnd <- 500.0
    // shouldBoostBeyondRange = false → giá trên 500 không được boost

    profile.Functions.Add(ratingBoost)
    profile.Functions.Add(priceBoost)

    // Cách kết hợp nhiều functions
    profile.FunctionAggregation <- ScoringFunctionAggregation.Sum
    // Sum: Cộng tất cả function scores + BM25
    // Average: Trung bình
    // Minimum/Maximum: Lấy min/max
    // FirstMatching: Dùng function đầu tiên match

    profile
```

### 5.5 Kết hợp FieldWeights + Functions (production pattern)

```fsharp
let createProductionScoringProfile () =
    let profile = ScoringProfile("production-ranking")

    // 1. FieldWeights: Name match quan trọng nhất
    profile.TextWeights <- TextWeights(dict [
        "Name",        4.0
        "Description", 2.0
        "Tags",        1.5
    ])

    // 2. Freshness: ưu tiên content được cập nhật gần đây
    let freshness = FreshnessScoringFunction(
        "LastUpdated",
        boost = 2.0,
        FreshnessScoringParameters(TimeSpan.FromDays(14.0))
    )
    freshness.Interpolation <- ScoringFunctionInterpolation.Logarithmic

    // 3. Magnitude: ưu tiên rating cao
    let magnitude = MagnitudeScoringFunction(
        "Rating",
        boost = 2.5,
        MagnitudeScoringParameters(boostingRangeStart = 3.0, boostingRangeEnd = 5.0)
    )
    magnitude.Interpolation <- ScoringFunctionInterpolation.Linear

    profile.Functions.Add(freshness)
    profile.Functions.Add(magnitude)
    profile.FunctionAggregation <- ScoringFunctionAggregation.Sum

    profile


// Đăng ký vào Index
let registerScoringProfile (indexClient: SearchIndexClient) =
    let fields = FieldBuilder().Build(typeof<HotelDocument>)
    let index  = SearchIndex("hotels-index", fields)

    index.ScoringProfiles.Add(createProductionScoringProfile())

    // Đặt profile mặc định (dùng khi không chỉ định trong query)
    index.DefaultScoringProfile <- "production-ranking"

    indexClient.CreateOrUpdateIndex(index)


// Kích hoạt profile trong query (override default)
let searchWithProfile (client: SearchClient) (query: string) =
    let options = SearchOptions()
    options.ScoringProfile <- "production-ranking"  // Chỉ định profile
    // Hoặc bỏ qua để dùng DefaultScoringProfile của index

    client.Search<HotelDocument>(query, options)
```

### 5.6 Tóm tắt khi nào dùng gì

| Nhu cầu nghiệp vụ | Giải pháp |
|---|---|
| Match tên sản phẩm quan trọng hơn mô tả | `FieldWeights { "Name": 5.0 }` |
| Ưu tiên sản phẩm mới ra mắt | `FreshnessScoringFunction` trên `CreatedAt` |
| Ưu tiên sản phẩm bán chạy | `MagnitudeScoringFunction` trên `SalesCount` |
| Ưu tiên sản phẩm rating cao | `MagnitudeScoringFunction` trên `Rating` |
| Kết hợp nhiều tiêu chí | Nhiều Functions + `FunctionAggregation.Sum` |
| Không cần relevance, chỉ cần sort | Dùng `OrderBy` thay ScoringProfile |

---

## Tổng kết

```
IndexDocumentsBatch — 4 Actions:
  Upload        → Replace toàn bộ document (mất fields không gửi)
  Merge         → Partial update (lỗi 404 nếu doc không tồn tại)
  MergeOrUpload → Upsert thông minh ← dùng mặc định
  Delete        → Xóa theo key (idempotent)

Batch Limits:
  Max 1,000 docs/batch | Max 16MB payload
  IndexBatchException: per-document failure, không fail cả batch
  → Luôn check IndexingResults, retry chỉ docs có status 409/503

SearchMode:
  Any (mặc định) → OR logic, recall cao
  All            → AND logic, precision cao

SearchOptions quan trọng:
  SearchFields      → scope search vào fields cụ thể
  Select            → projection, giảm payload
  Filter            → điều kiện bắt buộc (OData), không ảnh hưởng score
  OrderBy           → sort (override score-based ranking)
  Size / Skip       → pagination
  IncludeTotalCount → đếm tổng (tốn ~5% latency)
  HighlightFields   → đánh dấu từ khóa trong kết quả

@search.score:
  BM25 algorithm (TF × IDF × field length norm)
  Chỉ có ý nghĩa tương đối trong cùng một query
  Không có đơn vị, không so sánh giữa các queries

ScoringProfile:
  FieldWeights   → nhân hệ số BM25 score theo field
  Freshness      → bonus cho document mới (DateTimeOffset field)
  Magnitude      → bonus theo giá trị số (rating, price, count)
  FunctionAggregation: Sum | Average | Minimum | Maximum
  → Đăng ký trong Index, kích hoạt qua SearchOptions.ScoringProfile
```