module Book

open System

// ============ Item Types ============
// Both Book and Category live in the SAME Cosmos container.
// We use "itemType" to distinguish them AND as the partition key.

module ItemType =
    let book = "book"
    let category = "category"

// ============ ID Generation ============

module IdGen =
    let generateBookId () =
        let guid = System.Guid.NewGuid().ToString("N")
        "book_" + guid.[..7]
    let generateCategoryId () =
        let guid = System.Guid.NewGuid().ToString("N")
        "cat_" + guid.[..7]

// ============ Book ============

[<CLIMutable>]
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

// ============ Category ============

[<CLIMutable>]
type Category =
    { id: string
      categoryId: string
      name: string
      description: string
      itemType: string }
