module Types
open System

[<CLIMutable>]
type Category = {
    id          : string    // Partition Key
    name        : string
    description : string
    bookCount   : int
    isActive    : bool
    sortOrder   : int
    createdAt   : DateTime
}

// Partition Key : /category  (slug, VD: "van-hoc")
[<CLIMutable>]
type Book = {
    id           : string
    category     : string
    categoryName : string
    title        : string
    author       : string
    description  : string
    isAvailable  : bool
    createdAt    : DateTime
    isDeleted    : bool
}