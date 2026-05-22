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

from fastapi.middleware.cors import CORSMiddleware

# Mở cửa cho Frontend kết nối không bị chặn
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"], # Cho phép mọi trang web gọi vào
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
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

    # 3. API Lấy danh sách hồ sơ (Cho Cổng Admin)
@app.get("/api/admin/ho-so")
def lay_danh_sach_ho_so(db: Session = Depends(get_db)):
    return db.query(models.HoSoHocSinh).all()

# 4. API Cập nhật trạng thái hồ sơ (Cho Cổng Admin)
@app.put("/api/admin/duyet-ho-so/{cccd}")
def duyet_ho_so(cccd: str, trang_thai_moi: str, db: Session = Depends(get_db)):
    ho_so = db.query(models.HoSoHocSinh).filter(models.HoSoHocSinh.cccd == cccd).first()
    if not ho_so:
        raise HTTPException(status_code=404, detail="Không tìm thấy hồ sơ")
    
    ho_so.trang_thai = trang_thai_moi
    db.commit()
    return {"message": f"Đã cập nhật trạng thái thành: {trang_thai_moi}"}

# 5. API Ổ cắm Chatbot (Chờ Dev AI gắn code thật vào)
@app.post("/api/chat")
def chat_voi_ai(request: schemas.ChatRequest):
    # Đây là code giả (mock). Trưởng nhóm làm sẵn ổ cắm, Dev AI sẽ tự thay ruột sau.
    cau_tra_loi_gia = f"Trưởng nhóm làm ổ cắm. Bạn vừa hỏi: '{request.cau_hoi}'"
    return {"cau_tra_loi": cau_tra_loi_gia}