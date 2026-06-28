module Types

open System
open Azure.Search.Documents.Indexes
open Azure.Search.Documents.Indexes.Models

// Partition Key : /itemType
[<CLIMutable>]
type Category = {
    [<SimpleField(IsKey = true)>]
    id          : string

    [<SearchableField>]
    name        : string

    [<SearchableField>]
    description : string

    [<SimpleField(IsFilterable = true, IsFacetable = true)>]
    itemType    : string

    [<SimpleField(IsFilterable = true, IsFacetable = true)>]
    bookCount   : int

    [<SimpleField(IsFilterable = true, IsFacetable = true)>]
    isActive    : bool

    [<SimpleField(IsFilterable = true, IsSortable = true)>]
    sortOrder   : int

    [<SimpleField(IsFilterable = true, IsSortable = true)>]
    createdAt   : DateTime
}

// Partition Key : /itemType
[<CLIMutable>]
type Book = {
    [<SimpleField(IsKey = true)>]
    id           : string

    [<SimpleField(IsFilterable = true, IsFacetable = true)>]
    category     : string

    [<SearchableField>]
    categoryName : string

    [<SimpleField(IsFilterable = true, IsFacetable = true)>]
    itemType     : string

    [<SearchableField>]
    title        : string

    [<SearchableField>]
    author       : string

    [<SearchableField>]
    description  : string

    [<SimpleField(IsFilterable = true, IsFacetable = true)>]
    isAvailable  : bool

    [<SimpleField(IsFilterable = true, IsFacetable = true)>]
    isDeleted    : bool

    [<SimpleField(IsFilterable = true, IsSortable = true)>]
    createdAt    : DateTime
}

// Pagination result type
type PaginationResult<'T> = {
    Items: 'T list
    TotalCount: int64
    PageNumber: int
    PageSize: int
    TotalPages: int
}