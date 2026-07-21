# 09. Auth/RBAC Test Checklist

## 1. Muc tieu

Checklist nay dung de test lat cat Auth/RBAC dau tien sau khi SQL Server va API chay duoc.

## 2. Dieu kien truoc khi test

- SQL Server dang chay.
- Connection string trong `appsettings.json` hoac bien moi truong dung.
- API backend chay duoc.
- Database da tao schema Auth/RBAC.
- Seeder da tao roles: `student`, `parent`, `staff`, `admin`.
- Seeder da tao admin demo:
  - Email: `admin@example.com`
  - Password: `Admin123456!`

Tao database schema bang migration:

```powershell
cd apps/api
dotnet ef database update `
  --project src/Admissions.Infrastructure/Admissions.Infrastructure/Admissions.Infrastructure.csproj `
  --startup-project src/Admissions.Api/Admissions.Api/Admissions.Api.csproj
```

Xuat SQL script neu muon chay thu cong:

```powershell
cd apps/api
dotnet ef migrations script `
  --project src/Admissions.Infrastructure/Admissions.Infrastructure/Admissions.Infrastructure.csproj `
  --startup-project src/Admissions.Api/Admissions.Api/Admissions.Api.csproj `
  --output ../../scripts/sql/initial_auth_schema.sql
```

## 3. Health check

### GET `/api/health`

Expected:

```json
{
  "success": true,
  "service": "Admissions.Api",
  "status": "ok"
}
```

## 4. Student self-register is blocked

### POST `/api/auth/register-student`

Request:

```json
{
  "email": "bit240048@st.cmcu.edu.vn",
  "password": "Student123456!",
  "fullName": "Tai khoan sinh vien do nha truong cap",
  "phone": "0912345678"
}
```

Expected:

- HTTP 400.
- Response co code `STUDENT_SELF_REGISTER_DISABLED`.
- Bang `users` khong tao user moi tu endpoint nay.
- Tai khoan sinh vien dung de dang nhap phai co san tu seed, vi du `BIT240048@st.cmcu.edu.vn`.

## 5. Register parent

### POST `/api/auth/register-parent`

Request:

```json
{
  "email": "parent@example.com",
  "password": "Parent123456!",
  "fullName": "Nguyen Van Parent",
  "phone": "0987654321"
}
```

Expected:

- HTTP 200.
- Response co role `parent`.
- Bang `parent_profiles` co profile moi.

## 6. Login

### POST `/api/auth/login`

Request:

```json
{
  "email": "BIT240048@st.cmcu.edu.vn",
  "password": "Student123456!"
}
```

Expected:

- HTTP 200.
- Co `accessToken`.
- Co `refreshToken`.
- Bang `refresh_tokens` co token hash.

## 7. Current user

### GET `/api/auth/me`

Header:

```http
Authorization: Bearer <accessToken>
```

Expected:

- HTTP 200.
- Tra dung email user dang login.
- Tra dung role.

## 8. Profile

### GET `/api/profiles/me`

Expected:

- Student co `studentProfile`.
- Parent co `parentProfile`.

### PUT `/api/profiles/me`

Request student:

```json
{
  "fullName": "Nguyen Van Student Updated",
  "phone": "0900000000",
  "studentProfile": {
    "highSchool": "THPT Demo",
    "province": "Ha Noi",
    "graduationYear": 2026,
    "expectedScore": 24.5,
    "examScore": null,
    "interestedSubjectGroup": "A01",
    "notes": "Quan tam CNTT"
  },
  "parentProfile": null
}
```

Expected:

- HTTP 200.
- Profile duoc update trong database.

## 9. Admin user list

### GET `/api/admin/users`

Header:

```http
Authorization: Bearer <adminAccessToken>
```

Expected:

- Admin: HTTP 200.
- Student/Parent: HTTP 403.

## 10. Create staff

### POST `/api/admin/users/staff`

Header:

```http
Authorization: Bearer <adminAccessToken>
```

Request:

```json
{
  "email": "staff@example.com",
  "fullName": "Admissions Staff",
  "phone": "0911111111",
  "department": "Admissions",
  "position": "Consultant",
  "roles": ["staff"],
  "temporaryPassword": "Staff123456!"
}
```

Expected:

- HTTP 200.
- User co role `staff`.
- Bang `staff_profiles` co profile moi.

## 11. Lock user

### PATCH `/api/admin/users/{id}/status`

Request:

```json
{
  "status": "locked",
  "reason": "Test lock"
}
```

Expected:

- HTTP 200.
- User login lai bi tu choi.

## 12. Refresh token

### POST `/api/auth/refresh`

Request:

```json
{
  "refreshToken": "<refreshToken>"
}
```

Expected:

- HTTP 200.
- Tra access token moi.
- Refresh token cu bi revoke.

## 13. Logout

### POST `/api/auth/logout`

Request:

```json
{
  "refreshToken": "<refreshToken>"
}
```

Expected:

- HTTP 200.
- Refresh token bi revoke.
- Goi refresh bang token do tra 401.

## 14. Loi can bat

- Register trung email tra 409.
- Login sai password tra 401.
- Goi admin API bang student token tra 403.
- Goi API can auth khong co token tra 401.
- Refresh token het han/bi revoke tra 401.
