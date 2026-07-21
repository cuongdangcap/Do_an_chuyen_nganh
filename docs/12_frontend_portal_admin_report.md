# 12. Frontend Portal/Admin Report

## 1. Muc tieu

Da xay dung frontend dau tien cho:

- Cong thong tin tuyen sinh public.
- Cong quan tri du lieu tuyen sinh.

Frontend dung React + Vite trong:

```text
apps/web
```

## 2. URL khi chay local

API:

```text
http://localhost:5000
```

Frontend:

```text
http://127.0.0.1:5173
```

Tai khoan admin demo:

```text
Email: admin@example.com
Password: Admin123456!
```

## 3. Chuc nang public da co

- Xem so lieu tong quan: dot tuyen sinh, khoa, nganh, to hop mon.
- Loc nganh theo:
  - Tu khoa.
  - Khoa.
  - To hop mon.
  - Hoc phi toi da.
  - Co so.
- Xem danh sach nganh.
- Xem chi tiet nganh:
  - Khoa.
  - Mo ta.
  - Co hoi nghe nghiep.
  - Chuong trinh dao tao.
  - To hop mon.
  - Diem chuan.
  - Hoc phi.
- Xem FAQ.
- Chon nhieu chuong trinh va so sanh.

## 4. Chuc nang quan tri da co

- Dang nhap admin bang JWT.
- Luu token vao `localStorage`.
- Dang xuat.
- Them dot tuyen sinh.
- Them khoa.
- Them to hop mon.
- Them phuong thuc xet tuyen.
- Them nganh.
- Them chuong trinh dao tao.
- Them diem chuan.
- Them hoc phi.
- Them FAQ.

## 5. Backend thay doi kem theo

Da them CORS policy `WebDev` de frontend Vite goi API:

```text
http://localhost:5173
http://127.0.0.1:5173
```

## 6. Kiem tra da thuc hien

Build backend:

```powershell
dotnet build AdmissionsAiSystem.slnx --artifacts-path $env:TEMP\admissions-ai-build\solution -v:minimal
```

Ket qua:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

Build frontend:

```powershell
cd apps/web
npm run build
```

Ket qua:

```text
vite build thanh cong
29 modules transformed
```

Kiem tra Vite:

```text
HTTP/1.1 200 OK
http://127.0.0.1:5173/
```

Kiem tra CORS preflight:

```text
HTTP/1.1 204 No Content
Access-Control-Allow-Origin: http://127.0.0.1:5173
Access-Control-Allow-Methods: POST
```

## 7. Lenh chay lai

Chay API voi LocalDB:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:ASPNETCORE_URLS='http://localhost:5000'
$env:ConnectionStrings__DefaultConnection='Server=(localdb)\AdmissionsLocal;Database=AdmissionsAiSystem;Trusted_Connection=True;TrustServerCertificate=True'
$env:Jwt__Issuer='AdmissionsAiSystem'
$env:Jwt__Audience='AdmissionsAiSystem.Web'
$env:Jwt__Secret='change-this-development-secret-at-least-32-characters'
$env:Jwt__AccessTokenMinutes='60'
$env:Jwt__RefreshTokenDays='14'
$env:SeedAdmin__Email='admin@example.com'
$env:SeedAdmin__Password='Admin123456!'
$env:SeedAdmin__FullName='System Admin'
dotnet run --project apps/api/src/Admissions.Api/Admissions.Api/Admissions.Api.csproj
```

Chay frontend:

```powershell
cd apps/web
npm run dev -- --host 127.0.0.1 --port 5173
```

## 8. Buoc tiep theo

Sau frontend admissions data, buoc tiep theo nen lam module tai lieu:

- Upload PDF/DOCX/image.
- Luu metadata vao SQL Server.
- Trich xuat text.
- Chia chunk.
- Tao embedding.
- Luu vector vao Qdrant/FAISS.
- Tao API chatbot RAG doc quy che/hoc phi.
