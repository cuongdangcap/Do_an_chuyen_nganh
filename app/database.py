from sqlalchemy import create_engine
from sqlalchemy.orm import declarative_base
from sqlalchemy.orm import sessionmaker

# Tạo file SQLite tên là tuyensinh.db nằm ở thư mục gốc
SQLALCHEMY_DATABASE_URL = "sqlite:///./tuyensinh.db"

engine = create_engine(
    SQLALCHEMY_DATABASE_URL, connect_args={"check_same_thread": False}
)
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

Base = declarative_base()