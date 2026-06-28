module Config

// ── Cosmos DB config ──────────────────────────────────────────────────────

type CosmosConfig = {
    EndpointUrl  : string
    PrimaryKey   : string
    DatabaseId   : string
    BooksId      : string
    LeasesId     : string
}

let azureConfig = {
    EndpointUrl  = "ENDPOINT_URL"  // Fixed: windows.net
    PrimaryKey   = "PRIMARY_KEY"  // Fixed: windows.net
    DatabaseId   = "LibraryDatabase"
    BooksId      = "books"
    LeasesId     = "leases"
}

type AzureSearchConfig = {
    EndpointUrl   : string
    AdminKey      : string
    ReadKey       : string
    BookIndex     : string
    CategoryIndex : string
}

let azureSearchConfig = {
    EndpointUrl   = "ENDPOINT_URL"  // Fixed: windows.net
    AdminKey      = "ADMIN_KEY"  // Fixed: windows.net
    ReadKey       = "READ_KEY"  // Fixed: windows.net
    BookIndex     = "book-index"
    CategoryIndex = "category-index"
}