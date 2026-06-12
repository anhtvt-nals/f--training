# Day 4 - User Management

## Tổng quan
Bài tập trong thư mục `day-4` xây dựng một module quản lý người dùng bằng F# với các chức năng đọc, tìm, thêm, cập nhật và xóa người dùng từ file JSON.

```
dotnet add package FSharp.Control.AsyncSeq
```

## Code đã làm
- `Users.fs`
  - Định nghĩa kiểu dữ liệu `User` với các trường `Id`, `Name`, `Age`, `Email`.
  - Định nghĩa kiểu `Repo` để trừu tượng hoá cơ chế đọc/ghi dữ liệu.
  - Cài đặt `userRepo` dùng `System.IO` và `System.Text.Json` để đọc và ghi danh sách người dùng từ file `users.json`.
  - Các hàm chính:
    - `readUsers` - đọc danh sách người dùng từ repo.
    - `findUserById` - tìm người dùng theo `Id`.
    - `addUser` - thêm người dùng mới vào danh sách.
    - `updateUser` - cập nhật người dùng hiện tại theo `Id`, trả về `Ok(true)` hoặc `Error("User not found")`.
    - `deleteUser` - xoá người dùng theo `Id`, trả về `Ok(true)` hoặc `Error("User not found")`.
- `Program.fs`
  - Demo các thao tác với `userRepo` và file `users.json`:
    - liệt kê người dùng
    - tìm người dùng theo id
    - thêm người dùng mới
    - cập nhật người dùng tồn tại và không tồn tại
    - xoá người dùng

- `BulkImportUser.fs`
  - Xây dựng hàm `importUsers` để đọc file người dùng `.txt`, chuyển đổi thành `User`, ghi ra `users.json` và ghi lỗi ra `errors.txt`.
  - Dùng `FSharp.Control.AsyncSeq` để xử lý các dòng input song song với giới hạn concurrency.
  - Xử lý lỗi:
    - định dạng sai
    - id không hợp lệ
    - age không phải số

## Unit test đã làm
- Tạo unit-test
```
dotnet new xunit -lang F# -n Users.Tests
```

- Thêm package vào project
```
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
dotnet add package Microsoft.NET.Test.Sdk
```

- `Users.Tests/UsersTests.fs`
  - Tạo `fakeRepo` để test tách biệt với file thực tế bằng cách dùng bộ nhớ trong.
  - Các nhóm test:
    - `FindUserTests`
      - tìm được người dùng tồn tại theo id
      - không tìm được người dùng không tồn tại
    - `AddUserTests`
      - thêm người dùng mới và kiểm tra đã tồn tại sau khi thêm
    - `UpdateUserTests`
      - cập nhật người dùng tồn tại trả về `Ok(true)`
      - cập nhật người dùng không tồn tại trả về `Error "User not found"`
    - `ReadUsersTests`
      - đọc danh sách người dùng từ repo giả và kiểm tra số lượng / tên người dùng
    - `DeleteUserTests`
      - xoá người dùng tồn tại trả về `Ok(true)`
      - xoá người dùng không tồn tại trả về `Error "User not found"`
- `Users.Tests/BulkImportUserTest.fs`
  - Kiểm tra `BulkImportUser.importUsers` với:
    - dòng user hợp lệ
    - dòng invalid id
    - dòng invalid age
    - dòng định dạng sai
    - file nguồn rỗng
  - Xác nhận:
    - số lượng import thành công
    - số lượng lỗi
    - file `users.json` và `errors.txt` được tạo đúng

## Chạy thử
- Chạy ứng dụng: từ thư mục `day-4` dùng lệnh `dotnet run`
- Chạy unit test: từ thư mục `day-4` dùng lệnh `dotnet test Users.Tests`

## Ghi chú
- `Users.fs` hiện đang đọc và ghi cố định vào file `users.json`.
- Unit test sử dụng repo giả để tránh thay đổi `users.json` thật khi kiểm tra.
