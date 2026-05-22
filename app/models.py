from sqlalchemy import Column, Integer, String, Float
from .database import Base

class NganhHoc(Base):
    __tablename__ = "nganh_hoc"

    id = Column(Integer, primary_key=True, index=True)
    ma_nganh = Column(String, unique=True, index=True)
    ten_nganh = Column(String)
    diem_chuan = Column(Float)
    chi_tieu = Column(Integer)

class HoSoHocSinh(Base):
    __tablename__ = "ho_so"

    id = Column(Integer, primary_key=True, index=True)
    cccd = Column(String, unique=True, index=True)
    ho_ten = Column(String)
    diem_thi = Column(Float)
    nguyen_vong = Column(String) # Ghi mã ngành học sinh muốn nộp
    trang_thai = Column(String, default="Chờ duyệt") # Chờ duyệt, Trúng tuyển, Trượt