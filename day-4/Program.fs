module Program
open Users

let main _ =
    // List users
    printfn "List all users:"
    let users = readUsers userRepo "users.json"
    users |> List.iter (fun user -> printfn "Name: %s, Age: %d" user.Name user.Age)

    printfn "----"

    printfn "Find user id = 2:"
    // Find user by ID
    let id = 2
    let foundUser = findUserById userRepo id
    match foundUser with
    | Some user -> printfn "Found user: %s" user.Name
    | None -> printfn "User with ID %d not found" id

    printfn "----"

    printfn "Add a new user:"

    // Add a new user
    let newUser = { Id = 4; Name = "Diana"; Age = 28; Email = "diana@example.com" }
    addUser userRepo newUser
    printfn "User added: %s" newUser.Name

    printfn "----"

    printfn "Update user id = 2 with new name:"
    // Update an existing user
    let updatedUser = { Id = 2; Name = "Bob Updated"; Age = 26; Email = "bob.updated@example.com" }
    let afterUpdatedUser = updateUser userRepo updatedUser
    match afterUpdatedUser with
    | Ok(_) -> printfn "User updated: %s" updatedUser.Name
    | Error(msg) -> printfn "Error: %s" msg

    printfn "----"

    printfn "Try to update non-existing user id = 5:"
    let updatedUser2 = { Id = 5; Name = "Eve"; Age = 35; Email = "eve@example.com" }
    let afterUpdatedUser2 = updateUser userRepo updatedUser2
    match afterUpdatedUser2 with
    | Ok(_) -> printfn "User updated: %s" updatedUser2.Name
    | Error(msg) -> printfn "Error: %s" msg

    printfn "----"

    printfn "Delete user id = 2:"
    let deleteId = 2
    let deleteResult = deleteUser userRepo deleteId
    match deleteResult with
    | Ok(_) -> printfn "User with ID %d deleted" deleteId
    | Error(msg) -> printfn "Error: %s" msg
    0
