module SearchIndexClient

open System
open Azure
open Azure.Search.Documents.Indexes
open Azure.Search.Documents.Indexes.Models
open Config
open Types


let indexClient = 
  new SearchIndexClient(Uri(azureSearchConfig.EndpointUrl), AzureKeyCredential(azureSearchConfig.AdminKey))

let createCategoryIndex () =
    let fields = FieldBuilder().Build(typeof<Category>)
    let index = SearchIndex(azureSearchConfig.CategoryIndex, fields)
    indexClient.CreateOrUpdateIndex(index)


let createBookIndex () =
    let fields = FieldBuilder().Build(typeof<Book>)
    let index = SearchIndex(azureSearchConfig.BookIndex, fields)
    
    // Thêm suggester cho autocomplete
    let suggester = SearchSuggester("sg", ["title"; "author"])
    index.Suggesters.Add(suggester)
    
    indexClient.CreateOrUpdateIndex(index)


let listIndexes () =
    indexClient.GetIndexes() |> Seq.toList

let deleteIndex (indexName: string) =
    indexClient.DeleteIndex(indexName, System.Threading.CancellationToken.None) |> ignore

let deleteBookIndex () =
    deleteIndex azureSearchConfig.BookIndex

let deleteCategoryIndex () =
    deleteIndex azureSearchConfig.CategoryIndex

let getIndexStats (indexName: string) =
    indexClient.GetIndexStatistics(indexName)