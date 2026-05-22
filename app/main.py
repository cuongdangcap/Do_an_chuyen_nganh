from fastapi import FastAPI, Depends, HTTPException
from sqlalchemy.orm import Session
from . import models, schemas
from .database import engine, SessionLocal

# Tạo bảng trong Database
models.Base.metadata.create_all(bind=engine)

app = FastAPI(
    title="API Cổng Thông Tin Tuyển Sinh",
    description="Backend hệ thống tư vấn tuyển sinh đại học",
    version="1.0.0"
)

# Hàm gọi Database cho mỗi lần request
def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()

@app.get("/")
def read_root():
    return {"message": "Hệ thống Backend đã chạy thành công!"}

# ==========================================
# CÁC API CỦA HỆ THỐNG
# ==========================================

# 1. API Lấy danh sách ngành học (Cho Cổng học sinh)
@app.get("/api/nganh-hoc")
def lay_danh_sach_nganh(db: Session = Depends(get_db)):
    danh_sach = db.query(models.NganhHoc).all()
    return danh_sach

# 2. API Nộp hồ sơ (Cho Cổng học sinh)
@app.post("/api/nop-ho-so")
def nop_ho_so(ho_so: schemas.HoSoNop, db: Session = Depends(get_db)):
    # Kiểm tra xem CCCD này đã nộp chưa
    ho_so_cu = db.query(models.HoSoHocSinh).filter(models.HoSoHocSinh.cccd == ho_so.cccd).first()
    if ho_so_cu:
        raise HTTPException(status_code=400, detail="CCCD này đã được nộp hồ sơ!")
    
    # Lưu vào Database
    db_hoso = models.HoSoHocSinh(
        cccd=ho_so.cccd,
        ho_ten=ho_so.ho_ten,
        diem_thi=ho_so.diem_thi,
        nguyen_vong=ho_so.nguyen_vong
    )
    db.add(db_hoso)
    db.commit()
    db.refresh(db_hoso)
    
    return {"message": "Nộp hồ sơ thành công!", "data": db_hoso}