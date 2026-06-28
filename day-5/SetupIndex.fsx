#r "nuget: Azure.Search.Documents"

#load "Config.fs"
#load "Types.fs"
#load "SearchIndexClient.fs"

open SearchIndexClient

printfn "Setting up Azure Search Indexes..."
printfn ""

printfn "Creating Book Index..."
try
    createBookIndex()
    printfn "Book index created successfully"
with ex ->
    printfn "Error: %s" ex.Message
    printfn "Index might already exist - this is OK"

printfn ""

printfn "Creating Category Index..."
try
    createCategoryIndex()
    printfn "Category index created successfully"
with ex ->
    printfn "Error: %s" ex.Message
    printfn "Index might already exist - this is OK"

printfn ""
printfn "Done!"