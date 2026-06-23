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
    EndpointUrl  = "AZURE_COSMOSDB_ENDPOINT_URL"
    PrimaryKey   = "AZURE_COSMOSDB_PRIMARY_KEY"
    DatabaseId   = "LibraryDatabase"
    BooksId      = "books"
    LeasesId     = "leases"
}