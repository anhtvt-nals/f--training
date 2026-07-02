module CategoryController

open System
open Giraffe
open Book
open CategoryRepository

[<CLIMutable>]
type CreateCategoryRequest =
    { name: string
      description: string }

[<CLIMutable>]
type UpdateCategoryRequest =
    { name: string option
      description: string option }

// GET /api/categories
let getCategoriesHandler: HttpHandler =
    fun next ctx ->
        task {
            let repo = ctx.GetService<CategoryRepository>()
            let! cats = repo.GetAllAsync()
            return! json cats next ctx
        }

// GET /api/categories/{id}
let getCategoryHandler (catId: string) : HttpHandler =
    fun next ctx ->
        task {
            let repo = ctx.GetService<CategoryRepository>()
            try
                let! cat = repo.GetByIdAsync(catId)
                return! json cat next ctx
            with _ ->
                return! (setStatusCode 404 >=> json {| error = "Category not found" |}) next ctx
        }

// POST /api/categories
let createCategoryHandler: HttpHandler =
    fun next ctx ->
        task {
            let! req = ctx.BindJsonAsync<CreateCategoryRequest>()
            let catId = IdGen.generateCategoryId()
            let cat: Category =
                { id = catId
                  categoryId = catId
                  name = req.name
                  description = req.description
                  itemType = ItemType.category }
            let repo = ctx.GetService<CategoryRepository>()
            let! created = repo.CreateAsync(cat)
            return! (setStatusCode 201 >=> json created) next ctx
        }

// PUT /api/categories/{id}
let updateCategoryHandler (catId: string) : HttpHandler =
    fun next ctx ->
        task {
            let! req = ctx.BindJsonAsync<UpdateCategoryRequest>()
            let repo = ctx.GetService<CategoryRepository>()
            try
                let! existing = repo.GetByIdAsync(catId)
                let updated: Category =
                    { existing with
                        name = req.name |> Option.defaultValue existing.name
                        description = req.description |> Option.defaultValue existing.description }
                let! saved = repo.UpdateAsync(updated)
                return! json saved next ctx
            with _ ->
                return! (setStatusCode 404 >=> json {| error = "Category not found" |}) next ctx
        }

// DELETE /api/categories/{id}
let deleteCategoryHandler (catId: string) : HttpHandler =
    fun next ctx ->
        task {
            let repo = ctx.GetService<CategoryRepository>()
            try
                let _ = repo.DeleteAsync(catId).Result
                return! (setStatusCode 204) next ctx
            with _ ->
                return! (setStatusCode 404 >=> json {| error = "Category not found" |}) next ctx
        }
