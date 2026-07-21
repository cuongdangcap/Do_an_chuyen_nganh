from pydantic import BaseModel


class Settings(BaseModel):
    app_name: str = "Admissions AI Service"
    environment: str = "development"
