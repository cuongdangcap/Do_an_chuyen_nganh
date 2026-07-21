# 10. Admissions Data Test Checklist

## 1. Muc tieu

Checklist nay dung de test module du lieu tuyen sinh sau Auth/RBAC:

- Khoa/nganh/chuong trinh dao tao.
- Dot tuyen sinh, phuong thuc xet tuyen, to hop mon.
- Diem chuan, hoc phi, FAQ.
- API public cho cong thong tin.
- API admin/staff cho cong quan tri.

## 2. Dieu kien truoc khi test

- SQL Server dang chay.
- Da chay migration Auth/RBAC va AdmissionsDataSchema.
- Seeder da tao du lieu demo: dot tuyen sinh 2026, khoa CNTT, nganh CNTT, nganh Khoa hoc du lieu, A00/A01/D01/D07, THPT/HOC_BA/DGNL.
- Co token `admin` hoac `staff` de test API quan tri.

Cap nhat database bang migration:

```powershell
cd apps/api
dotnet ef database update `
  --project src/Admissions.Infrastructure/Admissions.Infrastructure/Admissions.Infrastructure.csproj `
  --startup-project src/Admissions.Api/Admissions.Api/Admissions.Api.csproj
```

Hoac chay SQL script:

```powershell
scripts/sql/initial_auth_schema.sql
scripts/sql/admissions_data_schema.sql
```

## 3. Public API

### GET `/api/admissions/cycles`

Expected: HTTP 200, co dot tuyen sinh nam 2026.

### GET `/api/admissions/faculties`

Expected: HTTP 200, co khoa `CNTT`.

### GET `/api/admissions/majors`

Query goi thu:

```http
GET /api/admissions/majors?keyword=du%20lieu&page=1&pageSize=10
```

Expected: HTTP 200, co phan trang, moi nganh co chuong trinh, diem chuan gan nhat, hoc phi.

### GET `/api/admissions/majors/{id}`

Expected:

- HTTP 200 voi id hop le.
- Tra chi tiet khoa, chuong trinh, to hop mon, diem chuan, hoc phi.
- Id khong ton tai tra 404 `MAJOR_NOT_FOUND`.

### GET `/api/admissions/subject-combinations`

Expected: HTTP 200, co A00, A01, D01, D07.

### GET `/api/admissions/methods`

Expected: HTTP 200, co THPT, HOC_BA, DGNL.

### GET `/api/admissions/faqs`

Expected: HTTP 200, co FAQ ve hoc phi va diem chuan.

### POST `/api/admissions/compare-programs`

Request:

```json
{
  "programIds": ["<program-id-1>", "<program-id-2>"]
}
```

Expected: HTTP 200, tra danh sach chuong trinh can so sanh va summary.

## 4. Admin/Staff API

Tat ca API duoi day can header:

```http
Authorization: Bearer <admin-or-staff-access-token>
```

### POST `/api/admin/admissions/cycles`

Request:

```json
{
  "year": 2027,
  "name": "Tuyen sinh 2027",
  "applicationStartDate": "2027-03-01",
  "applicationEndDate": "2027-08-31",
  "status": "active"
}
```

Expected: Admin/staff HTTP 200; student/parent/anonymous HTTP 403 hoac 401.

### POST `/api/admin/admissions/faculties`

Request:

```json
{
  "code": "QTKD",
  "name": "Khoa Quan tri kinh doanh",
  "description": "Dao tao kinh doanh va quan tri.",
  "status": "active"
}
```

Expected: HTTP 200, khoa moi xuat hien o API public.

### POST `/api/admin/admissions/subject-combinations`

Request:

```json
{
  "code": "D10",
  "subjects": "Toan, Dia ly, Tieng Anh",
  "description": "To hop minh hoa"
}
```

Expected: HTTP 200, to hop moi xuat hien o API public.

### POST `/api/admin/admissions/methods`

Request:

```json
{
  "code": "UTXT",
  "name": "Xet tuyen thang/uu tien xet tuyen",
  "description": "Phuong thuc minh hoa",
  "status": "active"
}
```

Expected: HTTP 200, phuong thuc moi xuat hien o API public.

### POST `/api/admin/admissions/majors`

Request:

```json
{
  "facultyId": "<faculty-id>",
  "code": "7340101",
  "name": "Quan tri kinh doanh",
  "description": "Nganh minh hoa",
  "careerOutcomes": "Chuyen vien kinh doanh, quan ly du an",
  "status": "active"
}
```

Expected: HTTP 200; `facultyId` khong ton tai tra 404 `REFERENCE_NOT_FOUND`.

### PUT `/api/admin/admissions/majors/{id}`

Expected: cap nhat ten, mo ta, khoa, trang thai; id khong ton tai tra 404.

### DELETE `/api/admin/admissions/majors/{id}`

Expected: soft delete bang cach doi `status` sang `inactive`.

### POST `/api/admin/admissions/programs`

Expected: tao chuong trinh dao tao; tham chieu sai tra 404 `REFERENCE_NOT_FOUND`.

### POST `/api/admin/admissions/cutoff-scores`

Expected: tao diem chuan cho chuong trinh, dot tuyen sinh, phuong thuc, to hop mon.

### POST `/api/admin/admissions/tuition-fees`

Expected: tao hoc phi theo nam hoc; API public tra hoc phi moi trong chi tiet nganh.

### POST `/api/admin/admissions/faqs`

Expected: tao FAQ moi; API public `/api/admissions/faqs` tra FAQ moi neu status `active`.

## 5. Loi can bat

- Goi admin API khong co token tra 401.
- Goi admin API bang student/parent token tra 403.
- Tao nganh voi `facultyId` sai tra 404.
- Tao program voi `majorId` sai tra 404.
- Tao diem chuan voi reference id sai tra 404.
- Public API khong duoc tra item co `status = inactive`.
