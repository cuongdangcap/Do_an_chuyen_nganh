from pydantic import BaseModel

# Cấu trúc dữ liệu Ngành học
class NganhHocCreate(BaseModel):
    ma_nganh: str
    ten_nganh: str
    diem_chuan: float
    chi_tieu: int

# Cấu trúc dữ liệu khi Học sinh nộp hồ sơ
class HoSoNop(BaseModel):
    cccd: str
    ho_ten: str
    diem_thi: float
    nguyen_vong: str