module Users.Tests

open Xunit
open FsUnit.Xunit
open Users

let createFakeRepo () =
    let mutable fakeDb =
        [
            { Id = 1; Name = "Alice"; Age = 30; Email = "alice@example.com" }
            { Id = 2; Name = "Diana"; Age = 28; Email = "diana@example.com" }
        ]

    {
        Read = fun _ -> fakeDb
        Write = fun _ users -> fakeDb <- users
    }
    
module FindUserTests =
    [<Fact>]
    let ``Find existing user by id`` () = 
        let fakeRepo = createFakeRepo ()
        let user = findUserById fakeRepo 1
        user |> should not' (be None)
        user.Value.Name |> should equal "Alice"

    [<Fact>]
    let ``Find non existing user by id`` () = 
        let fakeRepo = createFakeRepo ()
        let user = findUserById fakeRepo 999
        user |> should equal None

module AddUserTests =
    [<Fact>]
    let ``Add new user`` () =
        let fakeRepo = createFakeRepo ()
        let newUser = { Id = 3; Name = "Bob"; Age = 25; Email = "bob@gmail.com" }
        addUser fakeRepo newUser
        let user = findUserById fakeRepo 3
        user |> should not' (be None)
        user.Value.Name |> should equal "Bob"

module UpdateUserTests =
    [<Fact>]
    let ``Update existing user`` () =
        let fakeRepo = createFakeRepo ()
        let user = { Id = 1; Name = "Alice Smith"; Age = 31; Email = "alice.smith@example.com" }
        let result = updateUser fakeRepo user
        match result with
        | Ok (e) -> e |> should equal true
        | Error (e) -> failwithf "Expected success but got error: %s" e

    let ``Update non existing user`` () =
        let fakeRepo = createFakeRepo ()
        let user = { Id = 999; Name = "Non Existent"; Age = 40; Email = "non.existent@example.com" }
        let result = updateUser fakeRepo user
        result |> should equal (Error "User not found")

module ReadUsersTests =
    [<Fact>]
    let ``Read users from repo`` () =
        let fakeRepo = createFakeRepo ()
        let users = readUsers fakeRepo "users.json"
        users |> should haveLength 2
        users.[0].Name |> should equal "Alice"
        users.[1].Name |> should equal "Diana"

module DeleteUserTests =
    [<Fact>]
    let ``Delete existing user`` () =
        let fakeRepo = createFakeRepo ()
        let result = deleteUser fakeRepo 1
        match result with
        | Ok (e) -> e |> should equal true
        | Error (e) -> failwithf "Expected success but got error: %s" e

    [<Fact>]
    let ``Delete non existing user`` () =
        let fakeRepo = createFakeRepo ()
        let result = deleteUser fakeRepo 999
        match result with
        | Ok (e) -> failwithf "Expected error but got ok"
        | Error (e) -> e |> should equal "User not found"