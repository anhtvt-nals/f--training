# Azure AI Search — Analysis Pipeline & Custom Analyzers (F#)

> Tài liệu tham khảo về cách Azure AI Search xử lý văn bản: từ raw text → tokens có thể tìm kiếm được, cách cấu hình analyzer tùy chỉnh, autocomplete với EdgeNgram, và synonym mapping.

---

## 1. Analysis Pipeline: Character Filter → Tokenizer → Token Filter

Khi một document được index hoặc một query được thực thi, Azure AI Search đưa text qua **analysis pipeline** — chuỗi biến đổi 3 giai đoạn:

```
Raw Text Input
      │
      ▼
┌─────────────────────┐
│  Character Filters  │  ← Tiền xử lý ký tự trước khi tokenize
│  (0 hoặc nhiều)     │    Ví dụ: xóa HTML tags, chuẩn hóa ký tự
└─────────────────────┘
      │
      ▼
┌─────────────────────┐
│     Tokenizer       │  ← Tách text thành danh sách token
│     (đúng 1)        │    Ví dụ: tách theo whitespace, unicode word boundary
└─────────────────────┘
      │
      ▼
┌─────────────────────┐
│   Token Filters     │  ← Biến đổi, thêm, hoặc xóa tokens
│  (0 hoặc nhiều)     │    Ví dụ: lowercase, stopwords, stemming, synonym
└─────────────────────┘
      │
      ▼
 [token1, token2, ...]  ← Lưu vào inverted index
```

### 1.1 Giai đoạn 1: Character Filter

Nhận vào raw string, trả về string đã được biến đổi. Chạy **trước** khi tokenize.

| Filter | Mô tả | Ví dụ |
|---|---|---|
| `html_strip` | Xóa HTML tags, decode entities | `<b>Hello</b>` → `Hello` |
| `mapping` | Thay thế ký tự theo mapping table | `ñ` → `n`, `&` → `and` |
| `pattern_replace` | Thay thế theo regex | `C#` → `CSharp` |

```fsharp
// html_strip: loại bỏ HTML trước khi index nội dung web crawl
// mapping: chuẩn hóa ký tự đặc biệt cho tiếng Việt hoặc ký hiệu kỹ thuật
```

### 1.2 Giai đoạn 2: Tokenizer

Nhận vào string (đã qua character filter), trả về **danh sách token**. Mỗi analyzer có **đúng một** tokenizer.

| Tokenizer | Mô tả | Input → Output |
|---|---|---|
| `standard` | Unicode word boundary, lowercase | `"Hà Nội 2024"` → `["hà", "nội", "2024"]` |
| `whitespace` | Tách theo khoảng trắng, giữ case | `"Hà Nội"` → `["Hà", "Nội"]` |
| `keyword` | Toàn bộ string là 1 token | `"Hà Nội"` → `["Hà Nội"]` |
| `classic` | Như standard nhưng xử lý thêm dấu chấm, gạch nối | `"U.S.A."` → `["U.S.A"]` |
| `edge_ngram` | Sinh prefix tokens từ đầu | `"hotel"` → `["h","ho","hot","hote","hotel"]` |
| `ngram` | Sinh n-gram từ mọi vị trí | `"abc"` → `["a","ab","abc","b","bc","c"]` |
| `uax_url_email` | Như standard, nhưng giữ nguyên URL và email | `"go@mail.com"` → `["go@mail.com"]` |

### 1.3 Giai đoạn 3: Token Filter

Nhận vào danh sách token từ tokenizer, biến đổi và trả về danh sách token mới.

| Filter | Mô tả | Input → Output |
|---|---|---|
| `lowercase` | Chuyển toàn bộ về chữ thường | `["Hotel", "SPA"]` → `["hotel", "spa"]` |
| `uppercase` | Chuyển toàn bộ về chữ HOA | `["hotel"]` → `["HOTEL"]` |
| `stop` | Xóa stopwords (the, a, an, và, của…) | `["the", "hotel", "a"]` → `["hotel"]` |
| `stemmer` | Rút gọn về stem (chỉ Latin languages) | `["running", "runs"]` → `["run", "run"]` |
| `synonym` | Mở rộng hoặc thay thế bằng từ đồng nghĩa | `["hotel"]` → `["hotel", "resort", "khách sạn"]` |
| `edge_ngram` | Sinh prefix từ mỗi token | `["hotel"]` → `["h","ho","hot","hote","hotel"]` |
| `length` | Lọc token theo độ dài min/max | xóa token quá ngắn hoặc quá dài |
| `trim` | Xóa khoảng trắng đầu/cuối token | `[" hotel "]` → `["hotel"]` |
| `asciifolding` | Chuyển ký tự có dấu sang ASCII | `["café"]` → `["cafe"]` |
| `unique` | Xóa token trùng lặp | `["hotel","hotel","spa"]` → `["hotel","spa"]` |

### 1.4 Tóm tắt luồng thực tế

```
Input: "  <b>Khách sạn METROPOLIS</b> & Spa  "

Sau character filters (html_strip + mapping):
  → "  Khách sạn METROPOLIS and Spa  "

Sau tokenizer (standard):
  → ["khách", "sạn", "metropolis", "and", "spa"]

Sau token filters (lowercase → stop[en] → asciifolding):
  → ["khach", "san", "metropolis", "spa"]
       ↑ lowercase đã áp dụng từ tokenizer standard
       ↑ "and" bị xóa bởi stop filter
       ↑ dấu được xóa bởi asciifolding
```

---

## 2. Built-in Analyzers

Azure AI Search cung cấp sẵn nhiều analyzers để dùng ngay, không cần cấu hình.

### 2.1 Bảng tổng quan

| Analyzer | Tokenizer | Đặc điểm | Dùng khi nào |
|---|---|---|---|
| `standard` | standard | Mặc định, Unicode word boundary, lowercase, stop words EN | Fields tiếng Anh phổ thông |
| `simple` | lowercase | Chỉ lowercase + tách theo non-letter | Tìm kiếm đơn giản, không quan tâm stopwords |
| `whitespace` | whitespace | Chỉ tách theo khoảng trắng, giữ case | Code, tags, identifiers |
| `keyword` | keyword | Không phân tích — cả string là 1 token | Email, ID, zip code, exact match |
| `en.microsoft` | microsoft NLP | Morphological analysis, stemming tiếng Anh | Fields tiếng Anh cần chất lượng cao |
| `vi.microsoft` | microsoft NLP | Word segmentation tiếng Việt | **Fields tiếng Việt** |
| `vi.lucene` | Lucene Việt | Light stemming tiếng Việt | Fallback nếu không có Microsoft license |
| `fr.microsoft` | microsoft NLP | Tiếng Pháp | Fields tiếng Pháp |
| `ja.microsoft` | microsoft NLP | Tiếng Nhật (tokenize từ, kanji…) | Fields tiếng Nhật |

> Danh sách đầy đủ: [docs.microsoft.com/azure/search/index-add-language-analyzers](https://docs.microsoft.com/azure/search/index-add-language-analyzers)

### 2.2 Chi tiết các analyzers quan trọng

#### `standard` (mặc định)

```
Input:  "The Quick Brown Fox"
Output: ["quick", "brown", "fox"]
        ↑ lowercase, ↑ "The" bị xóa bởi English stopwords
```

Phù hợp: tiếng Anh phổ thông, không cần stemming.  
Không phù hợp: tiếng Việt, tiếng Nhật, hoặc khi cần chất lượng NLP cao.

#### `en.microsoft`

```
Input:  "running quickly through the beautiful gardens"
Output: ["run", "quick", "beauti", "garden"]
        ↑ morphological stemming — run/running → run
        ↑ quickly → quick; beautiful → beauti; gardens → garden
        ↑ "through", "the" bị xóa là stopwords
```

Phù hợp: Full-text search tiếng Anh cần recall cao (tìm "run" khớp "running").

#### `vi.microsoft` — quan trọng nhất cho ứng dụng Việt Nam

```
Input:  "Khách sạn năm sao tại Hà Nội"
Output: ["khách sạn", "năm sao", "hà nội"]
        ↑ word segmentation — nhận ra "Hà Nội" là 1 đơn vị
        ↑ "khách sạn" không bị tách thành "khách" + "sạn"
```

So sánh với `standard`:
```
standard output: ["khách", "sạn", "năm", "sao", "tại", "hà", "nội"]
                  ↑ tách sai — "hà nội" thành 2 token riêng
```

#### `keyword`

```
Input:  "HN-2024-001"
Output: ["HN-2024-001"]  ← toàn bộ string là 1 token
```

Phù hợp: Order ID, mã sản phẩm, email — cần exact match, không phân tích.

### 2.3 Khai báo trong F# Schema

```fsharp
[<CLIMutable>]
type ProductDocument =
    {
        [<SimpleField(IsKey = true)>]
        Id: string  // keyword implicit (IsKey không cần AnalyzerName)

        // Tiếng Việt — dùng vi.microsoft
        [<SearchableField(AnalyzerName = "vi.microsoft")>]
        NameVi: string

        // Tiếng Anh — dùng en.microsoft
        [<SearchableField(AnalyzerName = "en.microsoft")>]
        NameEn: string

        // Mã sản phẩm — exact match, không phân tích
        [<SearchableField(AnalyzerName = "keyword")>]
        ProductCode: string

        // Category — filterable exact, không cần search analyzer
        [<SimpleField(IsFilterable = true, IsFacetable = true)>]
        Category: string
    }
```

---

## 3. Custom Analyzer

Khi built-in analyzers không đáp ứng yêu cầu, ta định nghĩa **Custom Analyzer** gồm:

```
Custom Analyzer = (0..n Character Filters) + (1 Tokenizer) + (0..n Token Filters)
```

Custom analyzer được định nghĩa **trong Index definition**, không phải trong field.

### 3.1 Cấu trúc trong F#

```fsharp
open Azure.Search.Documents.Indexes
open Azure.Search.Documents.Indexes.Models

let buildIndexWithCustomAnalyzer () =

    // Bước 1: Định nghĩa Custom Analyzer
    let customAnalyzer =
        CustomAnalyzer(
            "my-vi-analyzer",           // Tên analyzer — dùng trong AnalyzerName của field
            LexicalTokenizerName.Standard  // Tokenizer (đúng 1)
        )

    // Character Filters (optional, thứ tự quan trọng)
    customAnalyzer.CharFilters.Add(CharFilterName.HtmlStrip)

    // Token Filters (optional, thứ tự quan trọng)
    customAnalyzer.TokenFilters.Add(TokenFilterName.Lowercase)
    customAnalyzer.TokenFilters.Add(TokenFilterName.Stop)       // default EN stopwords
    customAnalyzer.TokenFilters.Add(TokenFilterName.Trim)

    // Bước 2: Tạo Index và đăng ký custom analyzer
    let fields = FieldBuilder().Build(typeof<HotelDocument>)
    let index  = SearchIndex("hotels-index", fields)

    index.Analyzers.Add(customAnalyzer)  // Đăng ký vào index

    index
```

### 3.2 Ví dụ thực tế: Analyzer cho product search

```fsharp
let buildProductSearchAnalyzer () =

    // Token filter tùy chỉnh: EdgeNgram để autocomplete
    let edgeNgramFilter = EdgeNGramTokenFilter("my-edge-ngram")
    edgeNgramFilter.MinGram <- 2
    edgeNgramFilter.MaxGram <- 15

    // Token filter tùy chỉnh: xóa token quá ngắn
    let lengthFilter = LengthTokenFilter("my-min-length")
    lengthFilter.MinLength <- 2

    // Analyzer cho indexing (sinh edge ngrams)
    let indexAnalyzer =
        CustomAnalyzer(
            "product-index-analyzer",
            LexicalTokenizerName.Standard
        )
    indexAnalyzer.TokenFilters.Add(TokenFilterName.Lowercase)
    indexAnalyzer.TokenFilters.Add(TokenFilterName.Create("my-min-length"))
    indexAnalyzer.TokenFilters.Add(TokenFilterName.Create("my-edge-ngram"))

    // Analyzer cho query (KHÔNG sinh edge ngrams — chỉ lowercase)
    let queryAnalyzer =
        CustomAnalyzer(
            "product-query-analyzer",
            LexicalTokenizerName.Standard
        )
    queryAnalyzer.TokenFilters.Add(TokenFilterName.Lowercase)

    // Tổng hợp vào index
    let fields = FieldBuilder().Build(typeof<ProductDocument>)
    let index  = SearchIndex("products-index", fields)

    index.TokenFilters.Add(edgeNgramFilter)   // Đăng ký custom token filters
    index.TokenFilters.Add(lengthFilter)
    index.Analyzers.Add(indexAnalyzer)
    index.Analyzers.Add(queryAnalyzer)

    index
```

### 3.3 Dùng custom analyzer trong field definition

Khi dùng cho autocomplete, cần tách biệt **index-time analyzer** và **search-time analyzer**:

```fsharp
[<CLIMutable>]
type ProductDocument =
    {
        [<SimpleField(IsKey = true)>]
        Id: string

        // Dùng AnalyzerName nếu index + search dùng cùng analyzer
        [<SearchableField(AnalyzerName = "my-vi-analyzer")>]
        Description: string
    }

// Nhưng với autocomplete, cần dùng SearchField builder thủ công
// vì F# attribute không hỗ trợ IndexAnalyzerName + SearchAnalyzerName song song:

let buildAutocompleteField () =
    let nameField = SearchField("Name", SearchFieldDataType.String)
    nameField.IsSearchable <- true
    nameField.IndexAnalyzerName  <- LexicalAnalyzerName.Create("product-index-analyzer")
    nameField.SearchAnalyzerName <- LexicalAnalyzerName.Create("product-query-analyzer")
    nameField
```

---

## 4. EdgeNgram Filter cho Autocomplete

EdgeNgram là kỹ thuật index prefix của mỗi token để hỗ trợ **autocomplete / type-ahead search**.

### 4.1 Nguyên lý hoạt động

```
Token input: "hotel"

EdgeNgram (minGram=2, maxGram=5):
  → ["ho", "hot", "hote", "hotel"]
       ↑ min 2 ký tự    ↑ max 5 ký tự

Khi user gõ "hot" → khớp với token "hot" trong index → trả về document chứa "hotel"
```

### 4.2 Tham số EdgeNgramTokenFilter

| Tham số | Kiểu | Mặc định | Mô tả |
|---|---|---|---|
| `MinGram` | `int` | 1 | Độ dài token ngắn nhất (thường đặt 2-3) |
| `MaxGram` | `int` | 2 | Độ dài token dài nhất (thường đặt 10-20) |
| `Side` | `EdgeNGramTokenFilterSide` | `Front` | `Front` = từ đầu; `Back` = từ cuối |

### 4.3 Full setup autocomplete trong F#

```fsharp
open Azure.Search.Documents.Indexes.Models

let setupAutocompleteIndex (indexClient: SearchIndexClient) =
    // 1. Định nghĩa EdgeNgram token filter
    let edgeNgramFilter = EdgeNGramTokenFilter("autocomplete-ngram")
    edgeNgramFilter.MinGram <- 2    // Tối thiểu 2 ký tự — tránh noise
    edgeNgramFilter.MaxGram <- 25   // Tối đa 25 ký tự — đủ cho hầu hết từ

    // 2. Analyzer cho INDEX TIME — sinh edge ngrams
    let indexAnalyzer =
        CustomAnalyzer("autocomplete-index", LexicalTokenizerName.Standard)
    indexAnalyzer.TokenFilters.Add(TokenFilterName.Lowercase)
    indexAnalyzer.TokenFilters.Add(TokenFilterName.Create("autocomplete-ngram"))

    // 3. Analyzer cho SEARCH TIME — KHÔNG sinh ngrams
    //    Lý do: "hot" phải tìm exact token "hot", không phải "h","ho","hot"
    let searchAnalyzer =
        CustomAnalyzer("autocomplete-search", LexicalTokenizerName.Standard)
    searchAnalyzer.TokenFilters.Add(TokenFilterName.Lowercase)

    // 4. Tạo SearchField thủ công để dùng split analyzer
    let nameField = SearchField("Name", SearchFieldDataType.String)
    nameField.IsSearchable        <- true
    nameField.IsSortable          <- true
    nameField.IndexAnalyzerName   <- LexicalAnalyzerName.Create("autocomplete-index")
    nameField.SearchAnalyzerName  <- LexicalAnalyzerName.Create("autocomplete-search")

    // 5. Assemble index
    let index = SearchIndex("hotels-autocomplete")
    index.Fields.Add(SearchField("Id", SearchFieldDataType.String, IsKey = true))
    index.Fields.Add(nameField)
    index.TokenFilters.Add(edgeNgramFilter)
    index.Analyzers.Add(indexAnalyzer)
    index.Analyzers.Add(searchAnalyzer)

    indexClient.CreateOrUpdateIndex(index)


// 6. Thực hiện autocomplete query
let autocomplete (searchClient: SearchClient) (prefix: string) = task {
    let options = AutocompleteOptions()
    options.Mode   <- AutocompleteMode.TwoTerms  // trả về cả cụm từ
    options.Size   <- 5

    let! result = searchClient.AutocompleteAsync(prefix, "name-suggester", options)
    return result.Value.Results |> Seq.map (fun r -> r.Text) |> Seq.toList
}
```

### 4.4 Tại sao phải tách index-time và search-time analyzer?

```
Ví dụ document: Name = "Metropolis Hotel"

Index-time (với EdgeNgram):
  Tokens: ["me","met","metr","metro","metropolis",
           "ho","hot","hote","hotel"]

Search-time query: user gõ "metro"
  Tokens: ["metro"]

  → "metro" khớp với "metro" trong inverted index → ✅ Tìm thấy

Nếu dùng EdgeNgram cả hai chiều (SAI):
  Query "metro" → tokens: ["me","met","metr","metro"]
  → Tìm 4 tokens → kết quả sai lệch về relevance scoring
```

---

## 5. SynonymMap — Từ đồng nghĩa

SynonymMap là tập hợp các mapping từ đồng nghĩa, được định nghĩa **tách biệt** khỏi analyzer và index.

### 5.1 Kiến trúc tách biệt

```
SynonymMap (resource cấp service)
    └── Được tham chiếu bởi SearchField trong Index

Lý do tách biệt:
  ✅ Cập nhật từ đồng nghĩa không cần rebuild index
  ✅ Một SynonymMap dùng được cho nhiều indexes
  ✅ Quản lý từ đồng nghĩa độc lập với schema
```

### 5.2 Cú pháp định nghĩa synonym rules

Azure dùng **Solr synonym format**:

```
# Equivalent synonyms (hai chiều)
# "a, b, c" → tìm a thì ra b,c và ngược lại
hotel, khách sạn, resort, lodging

# Explicit mapping (một chiều)
# "a => b" → tìm a thì tìm b, nhưng tìm b không tìm a
sài gòn => hồ chí minh
hcm => hồ chí minh
tp hcm => hồ chí minh

# Nhiều → một
wifi, wi-fi, wireless internet => wifi

# Từ viết tắt
ks => khách sạn
```

### 5.3 Tạo và quản lý SynonymMap trong F#

```fsharp
open Azure.Search.Documents.Indexes
open Azure.Search.Documents.Indexes.Models

let createSynonymMap (indexClient: SearchIndexClient) =
    let rules = """
        hotel, khách sạn, resort, motel, lodging, accommodation
        spa, trị liệu, wellness center
        wifi, wi-fi, wireless, internet miễn phí
        hồ bơi, bể bơi, swimming pool, pool
        sài gòn => hồ chí minh
        hcm => hồ chí minh
        tp hcm => hồ chí minh
        hn => hà nội
        đà nẵng => da nang
    """

    let synonymMap = SynonymMap("hotel-synonyms", rules)
    indexClient.CreateOrUpdateSynonymMap(synonymMap)


let updateSynonymMap (indexClient: SearchIndexClient) =
    // ✅ SynonymMap có thể update bất cứ lúc nào mà không rebuild index
    let newRules = """
        hotel, khách sạn, resort, boutique hotel
        sài gòn => hồ chí minh
        thành phố hồ chí minh => hồ chí minh
    """
    let updated = SynonymMap("hotel-synonyms", newRules)
    indexClient.CreateOrUpdateSynonymMap(updated)


let deleteSynonymMap (indexClient: SearchIndexClient) =
    indexClient.DeleteSynonymMap("hotel-synonyms")
```

### 5.4 Gắn SynonymMap vào Field

```fsharp
// Cách 1: Dùng SearchField builder (linh hoạt hơn)
let nameField = SearchField("Name", SearchFieldDataType.String)
nameField.IsSearchable   <- true
nameField.AnalyzerName   <- LexicalAnalyzerName.Create("vi.microsoft")
nameField.SynonymMapNames.Add("hotel-synonyms")  // Gắn synonym map

// Cách 2: Nếu dùng CLIMutable record, cần override field thủ công
// vì F# attribute không có SynonymMapNames property

let buildIndexWithSynonyms (indexClient: SearchIndexClient) =
    let fields = [
        SearchField("Id", SearchFieldDataType.String, IsKey = true)

        let nameField = SearchField("Name", SearchFieldDataType.String)
        nameField.IsSearchable <- true
        nameField.AnalyzerName <- LexicalAnalyzerName.ViMicrosoft
        nameField.SynonymMapNames.Add("hotel-synonyms")
        nameField  // return field

        SearchField("Category", SearchFieldDataType.String,
                    IsFilterable = true, IsFacetable = true)
    ]

    let index = SearchIndex("hotels-index")
    fields |> List.iter index.Fields.Add
    indexClient.CreateOrUpdateIndex(index)
```

### 5.5 SynonymMap vs Synonym Token Filter

| | SynonymMap | Synonym Token Filter |
|---|---|---|
| **Vị trí** | Resource cấp service | Trong custom analyzer |
| **Thời điểm áp dụng** | Search time (query) | Index time hoặc search time |
| **Cập nhật** | Không cần rebuild index | Cần rebuild index |
| **Dùng cho** | Đồng nghĩa business logic | Normalization kỹ thuật |
| **Ví dụ** | hotel ↔ resort | colour → color |

---

## 6. Verify Output thực tế bằng Analyze API

Analyze API cho phép test pipeline của bất kỳ analyzer nào trực tiếp trên service, không cần index document thật.

### 6.1 Gọi Analyze API trong F#

```fsharp
open Azure.Search.Documents.Indexes
open Azure.Search.Documents.Indexes.Models

let analyzeText (indexClient: SearchIndexClient) (indexName: string) = task {

    // Test built-in analyzer
    let request =
        AnalyzeTextOptions(
            "Khách sạn Metropolis & Spa tại Hà Nội",  // Text cần analyze
            LexicalAnalyzerName.ViMicrosoft             // Analyzer cần test
        )

    let! response = indexClient.AnalyzeTextAsync(indexName, request)

    printfn "=== Analyze Result ==="
    for token in response.Value.Tokens do
        printfn "Token: %-20s | Start: %d | End: %d | Position: %d"
            token.Token token.StartOffset token.EndOffset token.Position

    return response.Value.Tokens |> Seq.toList
}

// Output mẫu:
// Token: khách sạn           | Start: 0  | End: 9  | Position: 0
// Token: metropolis           | Start: 10 | End: 20 | Position: 1
// Token: spa                  | Start: 23 | End: 26 | Position: 2
// Token: hà nội               | Start: 30 | End: 37 | Position: 3
```

### 6.2 Test custom analyzer

```fsharp
let testCustomAnalyzer (indexClient: SearchIndexClient) (indexName: string) = task {

    // Test analyzer đã định nghĩa trong index
    let request =
        AnalyzeTextOptions(
            "Running quickly through beautiful gardens",
            LexicalAnalyzerName.Create("my-custom-analyzer")  // Tên custom analyzer
        )

    let! response = indexClient.AnalyzeTextAsync(indexName, request)
    response.Value.Tokens |> Seq.iter (fun t -> printfn "%s" t.Token)
}
```

### 6.3 Test tokenizer và token filters riêng lẻ

```fsharp
let testComponents (indexClient: SearchIndexClient) (indexName: string) = task {

    // Chỉ test tokenizer (không có token filter)
    let tokenizerRequest =
        AnalyzeTextOptions("Hotel & Spa 2024")
    tokenizerRequest.Tokenizer <- LexicalTokenizerName.Standard

    let! tokenizerResult = indexClient.AnalyzeTextAsync(indexName, tokenizerRequest)
    printfn "=== Tokenizer only ==="
    tokenizerResult.Value.Tokens |> Seq.iter (fun t -> printfn "  %s" t.Token)
    // → hotel, &, spa, 2024

    // Test tokenizer + chỉ lowercase
    let filterRequest =
        AnalyzeTextOptions("Hotel & Spa 2024")
    filterRequest.Tokenizer <- LexicalTokenizerName.Standard
    filterRequest.TokenFilters.Add(TokenFilterName.Lowercase)

    let! filterResult = indexClient.AnalyzeTextAsync(indexName, filterRequest)
    printfn "=== With lowercase filter ==="
    filterResult.Value.Tokens |> Seq.iter (fun t -> printfn "  %s" t.Token)
    // → hotel, &, spa, 2024

    // Test tokenizer + lowercase + stop
    let stopRequest =
        AnalyzeTextOptions("The Hotel and Spa are beautiful")
    stopRequest.Tokenizer <- LexicalTokenizerName.Standard
    stopRequest.TokenFilters.Add(TokenFilterName.Lowercase)
    stopRequest.TokenFilters.Add(TokenFilterName.Stop)

    let! stopResult = indexClient.AnalyzeTextAsync(indexName, stopRequest)
    printfn "=== With stop filter ==="
    stopResult.Value.Tokens |> Seq.iter (fun t -> printfn "  %s" t.Token)
    // → hotel, spa, beautiful  (the, and, are bị xóa)
}
```

### 6.4 Helper function: So sánh nhiều analyzers

```fsharp
let compareAnalyzers
    (indexClient: SearchIndexClient)
    (indexName:   string)
    (text:        string)
    (analyzers:   string list) = task {

    printfn "Input: \"%s\"\n" text

    for analyzerName in analyzers do
        let request =
            AnalyzeTextOptions(text, LexicalAnalyzerName.Create(analyzerName))

        let! response = indexClient.AnalyzeTextAsync(indexName, request)

        let tokens =
            response.Value.Tokens
            |> Seq.map (fun t -> t.Token)
            |> String.concat " | "

        printfn "%-30s → [%s]" analyzerName tokens
}

// Cách dùng:
// compareAnalyzers client "hotels-index" "Khách sạn Hà Nội"
//     ["standard"; "vi.microsoft"; "vi.lucene"; "keyword"]
//
// Output:
// standard                       → [khách | sạn | hà | nội]
// vi.microsoft                   → [khách sạn | hà nội]
// vi.lucene                      → [khach | san | ha | noi]
// keyword                        → [Khách sạn Hà Nội]
```

### 6.5 Khi nào cần dùng Analyze API

| Tình huống | Hành động |
|---|---|
| Không tìm thấy kết quả kỳ vọng | Analyze cả query lẫn indexed text — so sánh tokens |
| Custom analyzer cho kết quả lạ | Analyze từng bước: tokenizer → từng token filter |
| Chọn giữa các built-in analyzers | Compare nhiều analyzers trên cùng sample text |
| Thiết kế EdgeNgram (autocomplete) | Verify prefix tokens được sinh đúng |
| Debug synonym không hoạt động | Verify synonym map đã được apply đúng field |

---

## Tổng kết

```
Analysis Pipeline (mỗi IsSearchable field đều qua pipeline này):

  Raw Text
    │
    ├─ [Character Filters]  0..n  html_strip, mapping, pattern_replace
    │
    ├─ [Tokenizer]          1     standard, whitespace, keyword, edge_ngram
    │
    └─ [Token Filters]      0..n  lowercase, stop, stemmer, synonym,
                                  edge_ngram, asciifolding, length, unique

Built-in Analyzers quan trọng:
  standard      → mặc định, tiếng Anh cơ bản
  en.microsoft  → tiếng Anh chất lượng cao, morphological stemming
  vi.microsoft  → tiếng Việt, word segmentation đúng
  keyword       → exact match, không phân tích

Custom Analyzer = 1 tokenizer + n token filters (định nghĩa trong Index)
  → Index schema cần rebuild nếu thay đổi analyzer

EdgeNgram Autocomplete:
  → Tách index-time (sinh ngrams) vs search-time (không sinh ngrams)
  → minGram: 2-3, maxGram: 10-25 tùy use case

SynonymMap (resource cấp service, TÁCH BIỆT khỏi Index):
  → Cập nhật không cần rebuild index
  → Solr format: "a, b, c" (hai chiều) hoặc "a => b" (một chiều)
  → Gắn vào field qua SynonymMapNames

Analyze API:
  → Test pipeline trực tiếp không cần index document thật
  → Debug mismatch giữa query và indexed tokens
  → So sánh nhiều analyzers trên cùng input
```