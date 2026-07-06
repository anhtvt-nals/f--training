# Quick Start Guide - New Features

## 🚀 Using the Enhanced Search API

### Basic Search with Count
```bash
curl -X POST http://localhost:5000/api/search \
  -H "Content-Type: application/json" \
  -d '{
    "query": "clean code",
    "top": 20
  }'
```

**Response:**
```json
{
  "source": "Azure Search",
  "count": 5,
  "totalCount": 127,
  "results": [...]
}
```

### Advanced Search with Filters
```bash
curl -X POST http://localhost:5000/api/search \
  -H "Content-Type: application/json" \
  -d '{
    "query": "programming",
    "top": 50,
    "categoryId": "cat_tech001",
    "minYear": 2000,
    "maxYear": 2020,
    "author": "Robert C. Martin"
  }'
```

---

## 📝 Input Validation Examples

### Valid Book Creation
```bash
curl -X POST http://localhost:5000/api/books \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Clean Code",
    "author": "Robert C. Martin",
    "categoryId": "cat_tech001",
    "categoryName": "Technology",
    "publishedYear": 2008,
    "totalCopies": 10
  }'
```

**Response:** HTTP 201 Created
```json
{
  "id": "book_abc12345",
  "bookId": "book_abc12345",
  "title": "Clean Code",
  ...
}
```

### Validation Error Example
```bash
curl -X POST http://localhost:5000/api/books \
  -H "Content-Type: application/json" \
  -d '{
    "title": "",
    "author": "Robert C. Martin",
    "categoryId": "cat_tech001",
    "categoryName": "Technology",
    "publishedYear": 3000,
    "totalCopies": -5
  }'
```

**Response:** HTTP 400 Bad Request
```json
{
  "error": "Validation failed",
  "details": [
    {
      "field": "title",
      "message": "Title is required"
    },
    {
      "field": "publishedYear",
      "message": "Published year must be between 1000 and 2027"
    },
    {
      "field": "totalCopies",
      "message": "Total copies must be non-negative"
    }
  ]
}
```

---

## 📤 Running the Parallel Import Script

### Basic Usage
```bash
cd library-book-management-api/Script
dotnet fsi SyncToAzureSearchParallel.fsx
```

### With Custom Configuration
```bash
export BATCH_SIZE=50
export MAX_PARALLEL_BATCHES=8
export RECREATE_INDEX_ON_ERROR=true

dotnet fsi SyncToAzureSearchParallel.fsx
```

### Expected Output
```
==========================================
Azure Search Import Tool with Parallel Processing
==========================================

Configuration:
  Cosmos DB: https://your-cosmos.documents.azure.com:443/
  Database: LibraryDB
  Container: Books
  Search Endpoint: https://your-search.search.windows.net
  Index Name: books-index
  Batch Size: 100
  Max Parallel Batches: 4
  Recreate on Error: false

Checking if index 'books-index' exists...
  Index already exists.

Fetching books from Cosmos DB...
  Found 1523 books in Cosmos DB.

Converting and chunking data...
  Created 16 batches of up to 100 documents each.

Uploading to Azure Search (parallel processing)...
  ✓ Batch uploaded: 100 documents
  ✓ Batch uploaded: 100 documents
  ✓ Batch uploaded: 100 documents
  ✓ Batch uploaded: 100 documents
  ...

==========================================
Import Complete
==========================================
  Total Documents: 1523
  Successfully Uploaded: 1523
  Failed: 0
  Duration: 12.45 seconds

✓ All documents uploaded successfully!
```

---

## 🧪 Running Tests with Cleanup

### Run All Tests
```bash
cd library-book-management-api/Tests
dotnet test
```

### Run Only Unit Tests
```bash
dotnet test --filter "FullyQualifiedName~BookTests"
dotnet test --filter "FullyQualifiedName~ValidationTests"
```

### Run Only Integration Tests
```bash
# Start Cosmos DB Emulator first
dotnet test --filter "FullyQualifiedName~Integration"
```

### Expected Test Output
```
Starting test execution, please wait...
A total of 24 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    35, Skipped:     0, Total:    35, Duration: 8.2s

Test cleanup summary:
  Cleaned up test book: book_abc12345
  Cleaned up test book: book_def67890
  Cleaned up test book: book_ghi13579
```

---

## 📮 Using Postman Collection

### Import Steps:
1. Open Postman
2. Click "Import" button
3. Select `postman_collection.json`
4. Collection will be added to your workspace

### Set Environment Variables:
1. Create new environment or use existing
2. Add variables:
   - `baseUrl`: `http://localhost:5000`
   - `bookId`: (will be auto-populated)
   - `categoryId`: (will be auto-populated)

### Test Workflow:
1. Run "Health Check" to verify API is running
2. Run "Create Category" (saves categoryId automatically)
3. Run "Create Book" (saves bookId automatically)
4. Run "Azure Search - Basic" to search for created book
5. Run "Update Book" to modify the book
6. Run "Delete Book" to clean up

---

## 🔧 Environment Setup

### Create `.env` file in project root:
```bash
# Cosmos DB Configuration
COSMOS_ENDPOINT_URL=https://your-cosmos.documents.azure.com:443/
COSMOS_PRIMARY_KEY=your-primary-key-here
COSMOS_DATABASE_ID=LibraryDB
COSMOS_CONTAINER_ID=Books

# Azure Search Configuration
SEARCH_ENDPOINT=https://your-search.search.windows.net
SEARCH_API_KEY=your-admin-key-here
SEARCH_INDEX_NAME=books-index

# Import Script Configuration (optional)
BATCH_SIZE=100
MAX_PARALLEL_BATCHES=4
RECREATE_INDEX_ON_ERROR=false
```

### For Cosmos DB Emulator (Local Development):
```bash
COSMOS_ENDPOINT_URL=https://localhost:8081
COSMOS_PRIMARY_KEY=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==
COSMOS_DATABASE_ID=LibraryDB
COSMOS_CONTAINER_ID=Books
```

---

## 📊 Performance Tips

### For Import Script:
- **Small datasets (<1000 books)**: Use `MAX_PARALLEL_BATCHES=2`, `BATCH_SIZE=100`
- **Medium datasets (1000-10000 books)**: Use `MAX_PARALLEL_BATCHES=4`, `BATCH_SIZE=100`
- **Large datasets (>10000 books)**: Use `MAX_PARALLEL_BATCHES=8`, `BATCH_SIZE=500`

### Azure Search Configuration:
- Ensure your search service tier can handle the load
- Monitor RUs (Request Units) consumption
- Consider implementing retry logic for production

---

## 🐛 Troubleshooting

### Import Script Fails with "Index not found"
**Solution:**
```bash
export RECREATE_INDEX_ON_ERROR=true
dotnet fsi SyncToAzureSearchParallel.fsx
```

### Validation Errors in API
**Check:**
- Title: max 200 characters, required
- Author: max 100 characters, required
- Year: between 1000 and 2027
- Copies: 0-10000 range

### Integration Tests Fail
**Ensure:**
1. Cosmos DB Emulator is running (Windows) or
2. Azure Cosmos DB connection is configured
3. Test database is accessible
4. Firewall rules allow connection

### Search Returns No Results
**Verify:**
1. Data was imported to Azure Search
2. Index exists: Check Azure Portal
3. Search query syntax is correct
4. Wait a few seconds for indexing to complete

---

## 📚 API Reference Quick Guide

| Method | Endpoint | Purpose | Returns Count |
|--------|----------|---------|---------------|
| GET | `/health` | Health check | - |
| GET | `/api/books` | Get all books | - |
| GET | `/api/books/{id}` | Get book by ID | - |
| POST | `/api/books` | Create book (validated) | - |
| PUT | `/api/books/{id}` | Update book (validated) | - |
| DELETE | `/api/books/{id}` | Delete book | - |
| POST | `/api/search` | Advanced search | ✅ Yes |
| POST | `/api/search/cosmos` | Cosmos DB search | No |
| GET | `/api/books/search/title?q=` | Search by title | No |

---

## 💡 Common Use Cases

### Use Case 1: Search Books by Category and Year Range
```json
POST /api/search
{
  "query": "*",
  "categoryId": "cat_tech001",
  "minYear": 2010,
  "maxYear": 2020,
  "top": 50
}
```

### Use Case 2: Search Specific Author's Books
```json
POST /api/search
{
  "query": "programming patterns",
  "author": "Martin Fowler",
  "top": 20
}
```

### Use Case 3: Pagination with Total Count
```json
// Page 1
POST /api/search
{
  "query": "software",
  "top": 10
}

// Response includes totalCount for pagination
{
  "count": 10,
  "totalCount": 127,  // Total matching documents
  "results": [...]
}

// Calculate: total pages = ceil(127 / 10) = 13 pages
```

---

## ✅ Checklist for Deployment

- [ ] Set production environment variables
- [ ] Run import script to populate Azure Search
- [ ] Test all API endpoints with Postman
- [ ] Run full test suite (unit + integration)
- [ ] Verify Azure Search index exists
- [ ] Check Cosmos DB connection
- [ ] Monitor initial performance
- [ ] Set up logging/monitoring
- [ ] Configure backup strategy
- [ ] Document any custom configurations

---

**Happy Coding! 🎉**

For detailed information, see [ENHANCEMENTS.md](./ENHANCEMENTS.md)
