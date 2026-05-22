from fastapi import FastAPI

app = FastAPI(
    title="API Cổng Thông Tin Tuyển Sinh",
    description="Backend hệ thống tư vấn tuyển sinh đại học (RAG Chatbot)",
    version="1.0.0"
)

@app.get("/")
def read_root():
    return {"message": "Hệ thống Backend đã chạy thành công! Chào mừng 5 anh em."}