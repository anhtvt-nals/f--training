# 📚 Library Book Management API (F#)

API quản lý sách thư viện đơn giản, viết bằng **F# + Giraffe (.NET 10)**.

> 📖 **[Xem API Documentation chi tiết →](./API.MD)**

## 🏗️ Kiến trúc

```
Models/Book.fs          → Domain model
Repositories/BookRepository.fs        → Cosmos DB CRUD
Repositories/BookSearchRepository.fs   → Claude-Opus AI Search
Controllers/BookController.fs          → HTTP handlers (Giraffe)
Program.fs             → Entry point, routing, DI config
```

## 📦 Packages

| Package | Vai trò |
|---|---|
| Giraffe 6.4 | F# web framework |
| FSharp.SystemTextJson | Serialize F# types |
| Microsoft.Azure.Cosmos 3.39 | Cosmos DB client |
| Azure.Search.Documents 11.6 | Azure AI Search client |
| DotNetEnv 3.0 | Load .env |

## 🔧 Cài đặt

```bash
# 1. Copy và điền env
cp .env.example .env

# 2. Build & run
dotnet run
```

## 🌐 API Endpoints

### Books CRUD
| Method | Path | Mô tả |
|---|---|---|
| GET | `/api/books` | Danh sách tất cả sách |
| GET | `/api/books/{id}` | Chi tiết sách theo ID |
| POST | `/api/books` | Tạo sách mới |
| PUT | `/api/books/{id}` | Cập nhật sách |
| DELETE | `/api/books/{id}` | Xóa sách |

### Categories CRUD
| Method | Path | Mô tả |
|---|---|---|
| GET | `/api/categories` | Danh sách categories |
| GET | `/api/categories/{id}` | Chi tiết category |
| POST | `/api/categories` | Tạo category mới |
| PUT | `/api/categories/{id}` | Cập nhật category |
| DELETE | `/api/categories/{id}` | Xóa category |

### Search (Azure Search - Phức tạp)
| Method | Path | Mô tả |
|---|---|---|
| POST | `/api/search` | Full-text search với Azure AI Search |

### Search (Cosmos DB - Đơn giản)
| Method | Path | Mô tả |
|---|---|---|
| POST | `/api/search/cosmos` | Search tổng hợp |
| GET | `/api/books/search/title?q=...` | Tìm theo title |
| GET | `/api/books/search/author?q=...` | Tìm theo author |
| GET | `/api/books/category/{categoryId}` | Lọc theo category |

### Health Check
| Method | Path | Mô tả |
|---|---|---|
| GET | `/health` | Health check |

> 📖 **[Chi tiết request/response examples →](./API.MD)**

## 📝 Request Examples

**Tạo sách (POST /api/books):**
```json
{
  "title": "Clean Code",
  "author": "Robert C. Martin",
  "genre": "Technology",
  "publishedYear": 2008,
  "totalCopies": 5
}
```

**Cập nhật sách (PUT /api/books/{id}):**
```json
{
  "title": "Clean Code (Updated)",
  "availableCopies": 3
}
```

**Tìm kiếm (POST /api/search):**
```json
{
  "query": "clean code"
}
```

## 🧠 F# Key Concepts

- **Record types** cho data models
- **task {}** computation expression cho async
- **Pattern matching** cho error handling
- **Module** cho organization
- **Piping (|>)** cho data transformation

## 🔄 Scripts

### Import Sample Data vào Cosmos DB

```bash
dotnet fsi Script/ImportToCosmos.fsx
```

**Kết quả:**
- 30 categories
- 200 sample books

### Sync Data từ Cosmos DB lên Azure Search

```bash
dotnet fsi Script/SyncToAzureSearch.fsx
```

**Chức năng:**
- Tạo/cập nhật Azure Search index
- Sync tất cả books từ Cosmos DB
- Batch upload (100 docs/request)

## 🧪 Testing

```bash
cd Tests
dotnet test
```

**Test Coverage:**
- ✅ Book/Category models
- ✅ ID generation (IdGen module)
- ✅ ItemType constants
- ✅ Request/Response types

## 📊 So sánh: Azure Search vs Cosmos DB Search

| Tính năng | Azure Search | Cosmos DB |
|-----------|--------------|-----------|
| **Loại search** | Full-text search | Filter-based query |
| **Fuzzy matching** | ✅ Có | ❌ Không |
| **Relevance scoring** | ✅ Có (BM25) | ❌ Không |
| **Performance** | ⚡ Rất nhanh (< 100ms) | 🚀 Nhanh (< 200ms) |
| **Syntax** | Lucene | SQL-like |
| **Phù hợp cho** | User-facing search | Exact filtering |

## 🚀 Quick Start

```bash
# 1. Clone repo
cd library-book-management-api

# 2. Setup .env với credentials
cp .env.example .env
# Điền: COSMOS_ENDPOINT_URL, COSMOS_PRIMARY_KEY, SEARCH_ENDPOINT, SEARCH_API_KEY

# 3. Import sample data
dotnet fsi Script/ImportToCosmos.fsx

# 4. Sync to Azure Search
dotnet fsi Script/SyncToAzureSearch.fsx

# 5. Run API
dotnet run

# 6. Test endpoints
curl http://localhost:5000/health
curl http://localhost:5000/api/books

# 7. Unit Test
dotnet test Tests/Library.Tests.fsproj
```

## 📚 Tài liệu

- [API Documentation chi tiết](./API.MD) - Request/response examples, error codes
- [Azure Cosmos DB Docs](https://learn.microsoft.com/en-us/azure/cosmos-db/)
- [Azure AI Search Docs](https://learn.microsoft.com/en-us/azure/search/)
- [Giraffe Framework](https://github.com/giraffe-fsharp/Giraffe)

---

**Last Updated:** July 3, 2026
