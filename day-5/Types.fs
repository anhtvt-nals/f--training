module Types
open System

// Partition Key : /itemType
[<CLIMutable>]
type Category = {
    id          : string
    name        : string
    description : string

    itemType    : string

    bookCount   : int
    isActive    : bool
    sortOrder   : int
    createdAt   : DateTime
}

// Partition Key : /itemType
[<CLIMutable>]
type Book = {
    id           : string
    category     : string
    categoryName : string

    itemType     : string

    title        : string
    author       : string
    description  : string
    isAvailable  : bool
    createdAt    : DateTime
    isDeleted    : bool
}