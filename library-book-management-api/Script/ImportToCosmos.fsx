#!/usr/bin/env dotnet fsi

#r "nuget: Microsoft.Azure.Cosmos, 3.39.0"
#r "nuget: DotNetEnv, 3.0.0"

open System
open System.IO
open Microsoft.Azure.Cosmos
open DotNetEnv

module ItemType =
    let book = "book"
    let category = "category"

type Book =
    { id: string
      bookId: string
      title: string
      author: string
      categoryId: string
      categoryName: string
      publishedYear: int
      totalCopies: int
      availableCopies: int
      itemType: string
      addedDate: DateTime }

type Category =
    { id: string
      categoryId: string
      name: string
      description: string
      itemType: string }

let requireEnv name =
    match Environment.GetEnvironmentVariable(name) with
    | null | "" -> failwith ("Missing env: " + name)
    | v -> v

let defaultEnv name fallback =
    match Environment.GetEnvironmentVariable(name) with
    | null | "" -> fallback
    | v -> v

try Env.Load() |> ignore with _ -> ()

let endpointUrl = requireEnv "COSMOS_ENDPOINT_URL"
let primaryKey = requireEnv "COSMOS_PRIMARY_KEY"
let databaseId = defaultEnv "COSMOS_DATABASE_ID" "LibraryDB"
let containerId = defaultEnv "COSMOS_CONTAINER_ID" "Books"

let client = new CosmosClient(endpointUrl, primaryKey)
let database = client.GetDatabase(databaseId)

let container =
    let containerProperties = ContainerProperties(containerId, partitionKeyPath = "/itemType")
    database.CreateContainerIfNotExistsAsync(containerProperties).GetAwaiter().GetResult() |> ignore
    database.GetContainer(containerId)

let scriptDir = __SOURCE_DIRECTORY__
let projectRoot = Directory.GetParent(scriptDir).FullName
let catPath = Path.Combine(projectRoot, "categories.txt")
let bookPath = Path.Combine(projectRoot, "book.txt")

// ------- Import Categories -------
printfn "Importing categories from %s ..." catPath
let catLines = File.ReadAllLines catPath |> Array.filter (fun l -> not (String.IsNullOrWhiteSpace l))

for line in catLines do
    let parts = line.Split('|')
    let catId = parts.[0].Trim()
    let name = parts.[1].Trim()
    let description = parts.[2].Trim()
    let cat: Category =
        { id = catId
          categoryId = catId
          name = name
          description = description
          itemType = ItemType.category }
    let pk = PartitionKey(ItemType.category)
    container.CreateItemAsync(cat, pk).GetAwaiter().GetResult() |> ignore
    printfn "  + Category: %s (%s)" name catId

// ------- Import Books -------
printfn "\nImporting books from %s ..." bookPath
let bookLines = File.ReadAllLines bookPath |> Array.filter (fun l -> not (String.IsNullOrWhiteSpace l))

for line in bookLines do
    let parts = line.Split('|')
    let bookId = parts.[0].Trim()
    let title = parts.[1].Trim()
    let author = parts.[2].Trim()
    let categoryId = parts.[3].Trim()
    let categoryName = parts.[4].Trim()
    let year = int (parts.[5].Trim())
    let copies = int (parts.[6].Trim())
    let book: Book =
        { id = bookId
          bookId = bookId
          title = title
          author = author
          categoryId = categoryId
          categoryName = categoryName
          publishedYear = year
          totalCopies = copies
          availableCopies = copies
          itemType = ItemType.book
          addedDate = DateTime.UtcNow }
    let pk = PartitionKey(ItemType.book)
    container.CreateItemAsync(book, pk).GetAwaiter().GetResult() |> ignore
    printfn "  + Book: %s (%s)" title bookId

printfn "\nDone. Imported %d categories and %d books." catLines.Length bookLines.Length
