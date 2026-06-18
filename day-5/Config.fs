module Config

// ── Cosmos DB config ──────────────────────────────────────────────────────

type CosmosConfig = {
    EndpointUrl  : string
    PrimaryKey   : string
    DatabaseId   : string
    BooksId      : string
    CategoriesId : string
    LeasesId     : string
}

let emulatorConfig = {
    EndpointUrl  = "https://localhost:8081/"
    PrimaryKey   = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
    DatabaseId   = "LibraryDatabase"
    BooksId      = "books"
    CategoriesId = "categories"
    LeasesId     = "leases"
}

let azureConfig = {
    EndpointUrl  = "https://YOUR_ACCOUNT.documents.azure.com:443/"
    PrimaryKey   = "YOUR_PRIMARY_KEY"
    DatabaseId   = "LibraryDatabase"
    BooksId      = "books"
    CategoriesId = "categories"
    LeasesId     = "leases"
}