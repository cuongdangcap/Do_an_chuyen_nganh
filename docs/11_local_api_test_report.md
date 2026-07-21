# 11. Local API Test Report

## 1. Thoi diem test

- Ngay test: 2026-06-26.
- May test: local Windows workspace.
- Backend: ASP.NET Core API.
- Database engine: SQL Server LocalDB.
- Database name: `AdmissionsAiSystem`.

## 2. Ghi chu moi truong

Docker chua co trong `PATH`, nen chua chay duoc `docker compose`.

Thay vao do, da dung SQL Server LocalDB:

```text
(localdb)\AdmissionsLocal
```

Connection string khi test API:

```text
Server=(localdb)\AdmissionsLocal;Database=AdmissionsAiSystem;Trusted_Connection=True;TrustServerCertificate=True
```

API chay tai:

```text
http://localhost:5000
```

## 3. Database setup da thuc hien

Da tao database:

```sql
AdmissionsAiSystem
```

Da apply SQL scripts:

```text
scripts/sql/initial_auth_schema.sql
scripts/sql/admissions_data_schema.sql
```

Da xac minh `__EFMigrationsHistory` co:

```text
20260625145552_InitialAuthSchema
20260625152108_AdmissionsDataSchema
```

Da xac minh database co 18 bang:

```text
__EFMigrationsHistory
admission_cycles
admission_methods
cutoff_scores
faculties
faqs
majors
parent_profiles
program_subject_combinations
programs
refresh_tokens
roles
staff_profiles
student_profiles
subject_combinations
tuition_fees
user_roles
users
```

## 4. Build

Do workspace bi loi quyen ghi voi `obj` mac dinh, build duoc chay vao thu muc TEMP:

```powershell
dotnet build AdmissionsAiSystem.slnx --artifacts-path $env:TEMP\admissions-ai-build\solution -v:minimal
```

Ket qua:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

## 5. API test da pass

Public API:

```text
PASS GET /api/health
PASS GET /api/admissions/cycles
PASS GET /api/admissions/faculties
PASS GET /api/admissions/subject-combinations
PASS GET /api/admissions/methods
PASS GET /api/admissions/faqs
PASS GET /api/admissions/majors
PASS GET /api/admissions/majors/{id}
PASS POST /api/admissions/compare-programs
```

Auth/Admin API:

```text
PASS POST /api/auth/login as admin
PASS POST /api/admin/admissions/cycles
PASS POST /api/admin/admissions/faculties
PASS POST /api/admin/admissions/subject-combinations
PASS POST /api/admin/admissions/methods
PASS POST /api/admin/admissions/majors
PASS POST /api/admin/admissions/programs
PASS POST /api/admin/admissions/cutoff-scores
PASS POST /api/admin/admissions/tuition-fees
PASS POST /api/admin/admissions/faqs
```

Authorization checks:

```text
PASS anonymous admin API returns 401
PASS student admin API returns 403
```

## 6. Ket luan

Module Auth/RBAC va Admissions Data da chay duoc end-to-end tren SQL Server LocalDB:

- Tao schema thanh cong.
- Seeder tao du lieu demo thanh cong.
- API public doc du lieu thanh cong.
- Admin API ghi du lieu thanh cong.
- Phan quyen admin/staff duoc bao ve dung.

Buoc tiep theo hop ly:

1. Lam frontend cho cong thong tin va cong quan tri admissions data.
2. Sau do lam module upload tai lieu PDF/DOCX/image va pipeline ingestion cho RAG.
