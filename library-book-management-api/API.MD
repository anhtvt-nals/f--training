# Library Book Management API

**F# + Giraffe + Cosmos DB + Azure Search**

Base URL: `http://localhost:5000`

---

## 📋 Table of Contents

- [Health Check](#health-check)
- [Books CRUD](#books-crud)
- [Categories CRUD](#categories-crud)
- [Search (Azure Search)](#search-azure-search)
- [Search (Cosmos DB)](#search-cosmos-db)
- [Data Models](#data-models)

---

## 🏥 Health Check

### Check API Status

```http
GET /health
```

**Response (200 OK)**
```json
{
  "status": "healthy"
}
```

---

## 📚 Books CRUD

### Get All Books

```http
GET /api/books
```

**Response (200 OK)**
```json
[
  {
    "id": "book_a1b2c3d4",
    "bookId": "book_a1b2c3d4",
    "title": "Clean Code",
    "author": "Robert C. Martin",
    "categoryId": "cat_tech001",
    "categoryName": "Technology",
    "publishedYear": 2008,
    "totalCopies": 5,
    "availableCopies": 3,
    "itemType": "book",
    "addedDate": "2024-01-15T10:30:00Z"
  }
]
```

---

### Get Book by ID

```http
GET /api/books/{id}
```

**Path Parameters**
- `id` (string, required): Book ID (e.g., `book_a1b2c3d4`)

**Response (200 OK)**
```json
{
  "id": "book_a1b2c3d4",
  "bookId": "book_a1b2c3d4",
  "title": "Clean Code",
  "author": "Robert C. Martin",
  "categoryId": "cat_tech001",
  "categoryName": "Technology",
  "publishedYear": 2008,
  "totalCopies": 5,
  "availableCopies": 3,
  "itemType": "book",
  "addedDate": "2024-01-15T10:30:00Z"
}
```

**Response (404 Not Found)**
```json
{
  "error": "Book not found"
}
```

---

### Create New Book

```http
POST /api/books
```

**Request Body**
```json
{
  "title": "The Pragmatic Programmer",
  "author": "Andy Hunt, Dave Thomas",
  "categoryId": "cat_tech001",
  "categoryName": "Technology",
  "publishedYear": 1999,
  "totalCopies": 10
}
```

**Response (201 Created)**
```json
{
  "id": "book_e5f6g7h8",
  "bookId": "book_e5f6g7h8",
  "title": "The Pragmatic Programmer",
  "author": "Andy Hunt, Dave Thomas",
  "categoryId": "cat_tech001",
  "categoryName": "Technology",
  "publishedYear": 1999,
  "totalCopies": 10,
  "availableCopies": 10,
  "itemType": "book",
  "addedDate": "2024-06-15T14:25:00Z"
}
```

**Note:** Book is automatically indexed in Azure Search after creation.

---

### Update Book

```http
PUT /api/books/{id}
```

**Path Parameters**
- `id` (string, required): Book ID

**Request Body** (all fields optional)
```json
{
  "title": "Clean Code: A Handbook of Agile Software Craftsmanship",
  "author": "Robert C. Martin",
  "categoryId": "cat_tech001",
  "categoryName": "Technology",
  "publishedYear": 2008,
  "totalCopies": 8,
  "availableCopies": 5
}
```

**Response (200 OK)**
```json
{
  "id": "book_a1b2c3d4",
  "bookId": "book_a1b2c3d4",
  "title": "Clean Code: A Handbook of Agile Software Craftsmanship",
  "author": "Robert C. Martin",
  "categoryId": "cat_tech001",
  "categoryName": "Technology",
  "publishedYear": 2008,
  "totalCopies": 8,
  "availableCopies": 5,
  "itemType": "book",
  "addedDate": "2024-01-15T10:30:00Z"
}
```

**Response (404 Not Found)**
```json
{
  "error": "Book not found"
}
```

**Note:** Azure Search index is automatically updated after book update.

---

### Delete Book

```http
DELETE /api/books/{id}
```

**Path Parameters**
- `id` (string, required): Book ID

**Response (204 No Content)**

No response body.

**Response (404 Not Found)**
```json
{
  "error": "Book not found"
}
```

**Note:** Book is automatically removed from Azure Search index after deletion.

---

## 📂 Categories CRUD

### Get All Categories

```http
GET /api/categories
```

**Response (200 OK)**
```json
[
  {
    "id": "cat_tech001",
    "categoryId": "cat_tech001",
    "name": "Technology",
    "description": "Books about programming, software engineering, and computer science",
    "itemType": "category"
  }
]
```

---

### Get Category by ID

```http
GET /api/categories/{id}
```

**Path Parameters**
- `id` (string, required): Category ID

**Response (200 OK)**
```json
{
  "id": "cat_tech001",
  "categoryId": "cat_tech001",
  "name": "Technology",
  "description": "Books about programming, software engineering, and computer science",
  "itemType": "category"
}
```

**Response (404 Not Found)**
```json
{
  "error": "Category not found"
}
```

---

### Create New Category

```http
POST /api/categories
```

**Request Body**
```json
{
  "name": "Science Fiction",
  "description": "Imaginative and futuristic concepts"
}
```

**Response (201 Created)**
```json
{
  "id": "cat_i9j0k1l2",
  "categoryId": "cat_i9j0k1l2",
  "name": "Science Fiction",
  "description": "Imaginative and futuristic concepts",
  "itemType": "category"
}
```

---

### Update Category

```http
PUT /api/categories/{id}
```

**Path Parameters**
- `id` (string, required): Category ID

**Request Body** (all fields optional)
```json
{
  "name": "Sci-Fi & Fantasy",
  "description": "Science fiction and fantasy literature"
}
```

**Response (200 OK)**
```json
{
  "id": "cat_i9j0k1l2",
  "categoryId": "cat_i9j0k1l2",
  "name": "Sci-Fi & Fantasy",
  "description": "Science fiction and fantasy literature",
  "itemType": "category"
}
```

**Response (404 Not Found)**
```json
{
  "error": "Category not found"
}
```

---

### Delete Category

```http
DELETE /api/categories/{id}
```

**Path Parameters**
- `id` (string, required): Category ID

**Response (204 No Content)**

No response body.

**Response (404 Not Found)**
```json
{
  "error": "Category not found"
}
```

---

## 🔍 Search (Azure Search)

### Full-Text Search with Azure AI Search

**Phức tạp, nhanh, hỗ trợ fuzzy search, scoring, highlighting**

```http
POST /api/search
```

**Request Body**
```json
{
  "query": "clean code",
  "top": 10
}
```

**Parameters**
- `query` (string, required): Search query (full-text search)
- `top` (integer, optional): Number of results to return (default: 20)

**Response (200 OK)**
```json
{
  "source": "Azure Search",
  "count": 2,
  "results": [
    {
      "id": "book_a1b2c3d4",
      "bookId": "book_a1b2c3d4",
      "title": "Clean Code",
      "author": "Robert C. Martin",
      "categoryId": "cat_tech001",
      "categoryName": "Technology",
      "publishedYear": 2008,
      "totalCopies": 5,
      "availableCopies": 3,
      "itemType": "book",
      "addedDate": "2024-01-15T10:30:00Z"
    }
  ]
}
```

**Features:**
- ✅ Full-text search across title, author, category
- ✅ Fuzzy matching (typo tolerance)
- ✅ Scoring & ranking
- ✅ Fast performance (< 100ms)
- ✅ Scalable for large datasets

---

## 🔎 Search (Cosmos DB)

### Search All Fields (Cosmos DB)

**Đơn giản, filter-based, không có fuzzy matching**

```http
POST /api/search/cosmos
```

**Request Body**
```json
{
  "query": "martin",
  "top": 10
}
```

**Parameters**
- `query` (string, required): Search term (exact or partial match)
- `top` (integer, optional): Number of results to return (default: 20)

**Response (200 OK)**
```json
{
  "source": "Cosmos DB",
  "count": 3,
  "results": [
    {
      "id": "book_a1b2c3d4",
      "bookId": "book_a1b2c3d4",
      "title": "Clean Code",
      "author": "Robert C. Martin",
      "categoryId": "cat_tech001",
      "categoryName": "Technology",
      "publishedYear": 2008,
      "totalCopies": 5,
      "availableCopies": 3,
      "itemType": "book",
      "addedDate": "2024-01-15T10:30:00Z"
    }
  ]
}
```

**Features:**
- ✅ Search across title, author, description
- ✅ Case-insensitive CONTAINS matching
- ❌ No fuzzy matching
- ❌ No relevance scoring

---

### Search by Title

```http
GET /api/books/search/title?q={query}
```

**Query Parameters**
- `q` (string, required): Title search term

**Example**
```http
GET /api/books/search/title?q=clean
```

**Response (200 OK)**
```json
[
  {
    "id": "book_a1b2c3d4",
    "bookId": "book_a1b2c3d4",
    "title": "Clean Code",
    "author": "Robert C. Martin",
    "categoryId": "cat_tech001",
    "categoryName": "Technology",
    "publishedYear": 2008,
    "totalCopies": 5,
    "availableCopies": 3,
    "itemType": "book",
    "addedDate": "2024-01-15T10:30:00Z"
  }
]
```

---

### Search by Author

```http
GET /api/books/search/author?q={query}
```

**Query Parameters**
- `q` (string, required): Author search term

**Example**
```http
GET /api/books/search/author?q=martin
```

**Response (200 OK)**
```json
[
  {
    "id": "book_a1b2c3d4",
    "bookId": "book_a1b2c3d4",
    "title": "Clean Code",
    "author": "Robert C. Martin",
    "categoryId": "cat_tech001",
    "categoryName": "Technology",
    "publishedYear": 2008,
    "totalCopies": 5,
    "availableCopies": 3,
    "itemType": "book",
    "addedDate": "2024-01-15T10:30:00Z"
  }
]
```

---

### Get Books by Category

```http
GET /api/books/category/{categoryId}
```

**Path Parameters**
- `categoryId` (string, required): Category ID

**Example**
```http
GET /api/books/category/cat_tech001
```

**Response (200 OK)**
```json
[
  {
    "id": "book_a1b2c3d4",
    "bookId": "book_a1b2c3d4",
    "title": "Clean Code",
    "author": "Robert C. Martin",
    "categoryId": "cat_tech001",
    "categoryName": "Technology",
    "publishedYear": 2008,
    "totalCopies": 5,
    "availableCopies": 3,
    "itemType": "book",
    "addedDate": "2024-01-15T10:30:00Z"
  }
]
```

---

## 📊 Data Models

### Book Model

```fsharp
type Book = {
    id: string                // Primary key, e.g., "book_a1b2c3d4"
    bookId: string            // Same as id (redundant for compatibility)
    title: string             // Book title
    author: string            // Book author(s)
    categoryId: string        // Category ID reference
    categoryName: string      // Denormalized category name
    publishedYear: int        // Year of publication
    totalCopies: int          // Total number of copies
    availableCopies: int      // Available copies for borrowing
    itemType: string          // Always "book" (partition key)
    addedDate: DateTime       // UTC timestamp when added
}
```

---

### Category Model

```fsharp
type Category = {
    id: string                // Primary key, e.g., "cat_tech001"
    categoryId: string        // Same as id (redundant for compatibility)
    name: string              // Category name
    description: string       // Category description
    itemType: string          // Always "category" (partition key)
}
```

---

### ID Generation

**Books:** `book_` + 8-character GUID substring
- Example: `book_a1b2c3d4`

**Categories:** `cat_` + 8-character GUID substring
- Example: `cat_tech001`

---

## 🔧 Configuration

### Environment Variables

Create a `.env` file in the project root:

```env
# Cosmos DB
COSMOS_ENDPOINT_URL=https://your-cosmos-account.documents.azure.com:443/
COSMOS_PRIMARY_KEY=your-cosmos-primary-key-here==
COSMOS_DATABASE_ID=LibraryDatabase
COSMOS_CONTAINER_ID=books

# Azure Search
SEARCH_ENDPOINT=https://your-search-service.search.windows.net
SEARCH_API_KEY=your-admin-key-here
SEARCH_INDEX_NAME=books-v2-index
```

---

## 🚀 Running the API

### Prerequisites
- .NET 10.0 SDK
- Azure Cosmos DB account
- Azure AI Search service

### Start the API

```bash
cd library-book-management-api
dotnet restore
dotnet run
```

API will be available at: `http://localhost:5000`

---

## 📝 Scripts

### Import Sample Data to Cosmos DB

```bash
dotnet fsi Script/ImportToCosmos.fsx
```

Imports:
- 30 categories
- 200 sample books

---

### Sync Data to Azure Search

```bash
dotnet fsi Script/SyncToAzureSearch.fsx
```

Features:
- Creates/updates Azure Search index
- Syncs all books from Cosmos DB
- Batch uploads (100 documents per request)

---

## 🧪 Testing

### Run Unit Tests

```bash
cd Tests
dotnet test
```

Test coverage:
- ✅ Book/Category models
- ✅ ID generation
- ✅ ItemType constants
- ✅ Request/Response types

---

## 🎯 API Comparison: Azure Search vs Cosmos DB

| Feature | Azure Search | Cosmos DB |
|---------|-------------|-----------|
| **Search Type** | Full-text search | Filter-based query |
| **Fuzzy Matching** | ✅ Yes (typo tolerance) | ❌ No |
| **Relevance Scoring** | ✅ Yes (BM25) | ❌ No |
| **Performance** | ⚡ Very Fast (< 100ms) | 🚀 Fast (< 200ms) |
| **Query Syntax** | Lucene syntax | SQL-like syntax |
| **Best For** | User-facing search | Exact filtering |
| **Cost** | Per query + storage | Per RU + storage |

---

## 📚 Example Requests (cURL)

### Create a Book

```bash
curl -X POST http://localhost:5000/api/books \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Domain-Driven Design",
    "author": "Eric Evans",
    "categoryId": "cat_tech001",
    "categoryName": "Technology",
    "publishedYear": 2003,
    "totalCopies": 7
  }'
```

---

### Search with Azure Search

```bash
curl -X POST http://localhost:5000/api/search \
  -H "Content-Type: application/json" \
  -d '{
    "query": "clean code",
    "top": 5
  }'
```

---

### Search by Title (Cosmos DB)

```bash
curl "http://localhost:5000/api/books/search/title?q=clean"
```

---

### Get Books by Category

```bash
curl "http://localhost:5000/api/books/category/cat_tech001"
```

---

## 🐛 Error Responses

### 400 Bad Request

```json
{
  "error": "Invalid request body"
}
```

### 404 Not Found

```json
{
  "error": "Book not found"
}
```

```json
{
  "error": "Category not found"
}
```

### 500 Internal Server Error

```json
{
  "error": "Internal server error"
}
```

---

## 📖 Additional Resources

- [Azure Cosmos DB Documentation](https://learn.microsoft.com/en-us/azure/cosmos-db/)
- [Azure AI Search Documentation](https://learn.microsoft.com/en-us/azure/search/)
- [Giraffe Framework](https://github.com/giraffe-fsharp/Giraffe)
- [F# Documentation](https://fsharp.org/)

---

**Last Updated:** July 3, 2026
