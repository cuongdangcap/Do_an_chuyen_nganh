# 06. Bộ sơ đồ thiết kế

## 1. Use Case Diagram

```mermaid
flowchart LR
    Guest((Guest))
    Student((Student))
    Parent((Parent))
    Staff((Staff))
    Admin((Admin))
    SysAdmin((System Admin))

    UC1[Xem thông tin tuyển sinh]
    UC2[Đăng ký/đăng nhập]
    UC3[Quản lý hồ sơ]
    UC4[Chat với AI]
    UC5[Upload file trong chat]
    UC6[Đánh giá câu trả lời]
    UC7[Yêu cầu tư vấn viên]
    UC8[Phản hồi trực tiếp]
    UC9[Quản lý dữ liệu tuyển sinh]
    UC10[Upload tài liệu tri thức]
    UC11[Xem dashboard/feedback]
    UC12[Chạy evaluation AI]
    UC13[Quản lý user/role]
    UC14[Quản lý log/backup]

    Guest --> UC1
    Guest --> UC2
    Student --> UC1
    Student --> UC3
    Student --> UC4
    Student --> UC5
    Student --> UC6
    Student --> UC7
    Parent --> UC1
    Parent --> UC4
    Parent --> UC5
    Parent --> UC6
    Parent --> UC7
    Staff --> UC8
    Staff --> UC10
    Staff --> UC11
    Staff --> UC9
    Admin --> UC9
    Admin --> UC10
    Admin --> UC11
    Admin --> UC12
    Admin --> UC13
    SysAdmin --> UC14
```

## 2. Kiến trúc tổng thể

```mermaid
flowchart LR
    U[Student / Parent / Guest] --> WEB[Web Client: Public + Chat UI]
    A[Staff / Admin] --> ADMIN[Admin Portal]

    WEB --> API[ASP.NET Core Web API]
    ADMIN --> API

    API --> SQL[(SQL Server)]
    API --> FS[(File Storage)]
    API --> HUB[SignalR Hub / Polling]
    API --> AI[Python FastAPI AI Service]
    API --> LOG[(Audit/System Logs)]

    AI --> QDRANT[(Qdrant Vector DB)]
    AI --> FAISS[(FAISS Benchmark Index)]
    AI --> OCR[OCR / Document Parser]
    AI --> LLM[LLM Provider or Local LLM]
    AI --> SQL
    AI --> FS
```

## 3. Deployment local bằng Docker Compose

```mermaid
flowchart TD
    subgraph DockerHost[Local Docker Host]
        WEB[web: React/Next.js]
        API[api: ASP.NET Core]
        AI[ai-service: FastAPI]
        SQL[sqlserver]
        QDRANT[qdrant]
        MINIO[local file storage / MinIO optional]
    end

    Browser[Browser] --> WEB
    WEB --> API
    API --> SQL
    API --> AI
    API --> MINIO
    AI --> QDRANT
    AI --> MINIO
```

## 4. ERD rút gọn

```mermaid
erDiagram
    users ||--o{ user_roles : has
    roles ||--o{ user_roles : assigned
    users ||--o| student_profiles : owns
    users ||--o| parent_profiles : owns
    users ||--o| staff_profiles : owns
    users ||--o{ chat_conversations : starts

    faculties ||--o{ majors : contains
    majors ||--o{ programs : offers
    programs ||--o{ cutoff_scores : has
    programs ||--o{ tuition_fees : has
    programs ||--o{ program_subject_combinations : accepts
    subject_combinations ||--o{ program_subject_combinations : maps
    admission_methods ||--o{ cutoff_scores : uses
    admission_cycles ||--o{ cutoff_scores : groups

    knowledge_documents ||--o{ document_versions : versions
    document_versions ||--o{ document_chunks : split_into
    document_versions ||--o{ ingestion_jobs : processed_by
    document_chunks ||--o{ chat_message_sources : referenced_by

    chat_conversations ||--o{ chat_messages : contains
    chat_messages ||--o{ attachments : has
    chat_messages ||--o{ chat_message_sources : cites
    chat_messages ||--o{ message_feedback : receives
    chat_conversations ||--o{ handoff_tickets : escalates
    handoff_tickets ||--o{ staff_replies : has

    model_configs ||--o{ evaluation_runs : configures
    evaluation_runs ||--o{ evaluation_results : contains
    golden_questions ||--o{ evaluation_results : tested_by
```

## 5. Sequence: Chat RAG

```mermaid
sequenceDiagram
    participant U as User
    participant W as Web Chat
    participant API as ASP.NET Core API
    participant SQL as SQL Server
    participant AI as AI Service
    participant Q as Qdrant
    participant L as LLM

    U->>W: Nhập câu hỏi
    W->>API: POST /chat/conversations/{id}/messages
    API->>SQL: Lưu user message
    API->>AI: /internal/rag/answer
    AI->>AI: Detect intent
    AI->>SQL: Query structured data nếu cần
    AI->>Q: Vector search top-k chunks
    AI->>AI: Rerank + build context
    AI->>L: Generate answer with context
    L-->>AI: Answer
    AI->>AI: Grounding/confidence check
    AI-->>API: Answer + sources + confidence
    API->>SQL: Lưu assistant message + sources
    API-->>W: Response
    W-->>U: Hiển thị câu trả lời có nguồn
```

## 6. Activity: Upload tài liệu tri thức

```mermaid
flowchart TD
    A[Admin/Staff chọn file] --> B[POST /admin/documents]
    B --> C[API validate file]
    C -->|Invalid| X[Trả lỗi FILE_TYPE_NOT_SUPPORTED/FILE_TOO_LARGE]
    C -->|Valid| D[Lưu file gốc]
    D --> E[Tạo knowledge_document + version]
    E --> F[Tạo ingestion_job]
    F --> G[AI service parse/OCR]
    G --> H[Clean text]
    H --> I[Chunking]
    I --> J[Embedding]
    J --> K[Upsert Qdrant]
    K --> L[Lưu chunks metadata SQL Server]
    L --> M[Cập nhật job completed]
```

## 7. Sequence: Staff handoff

```mermaid
sequenceDiagram
    participant U as User
    participant W as Web Chat
    participant API as API
    participant SQL as SQL Server
    participant S as Staff Portal
    participant RT as SignalR/Polling

    U->>W: Yêu cầu gặp tư vấn viên
    W->>API: POST /chat/conversations/{id}/handoff
    API->>SQL: Tạo handoff_ticket status=open
    S->>API: GET /staff/handoffs
    API-->>S: Danh sách ticket
    S->>API: POST /staff/handoffs/{id}/assign
    API->>SQL: Gán staff
    S->>API: POST /staff/handoffs/{id}/reply
    API->>SQL: Lưu staff message
    API->>RT: Push notification/message
    RT-->>W: Tin nhắn staff
    W-->>U: Hiển thị phản hồi
```

## 8. Activity: Evaluation AI

```mermaid
flowchart TD
    A[Admin chọn model config] --> B[Tạo evaluation_run]
    B --> C[Lấy golden_questions]
    C --> D[Chạy từng câu hỏi qua RAG]
    D --> E[Lưu retrieved chunks]
    E --> F[So sánh expected source]
    F --> G[Tính hit@k, citation correctness, latency]
    G --> H[Lưu evaluation_results]
    H --> I[Tổng hợp report]
    I --> J[Admin xem bảng so sánh cấu hình]
```

## 9. State: Handoff ticket

```mermaid
stateDiagram-v2
    [*] --> open
    open --> assigned: staff nhận xử lý
    assigned --> resolved: staff giải quyết
    resolved --> closed: đóng ticket
    open --> closed: admin đóng
    assigned --> open: staff trả lại hàng đợi
```

## 10. State: Document ingestion

```mermaid
stateDiagram-v2
    [*] --> pending
    pending --> processing
    processing --> completed
    processing --> failed
    failed --> pending: retry/reindex
    completed --> processing: reindex
```

## 11. Component: Backend modular monolith

```mermaid
flowchart TD
    API[Admissions.Api] --> Auth[Auth Module]
    API --> Users[Users & Profiles]
    API --> Admissions[Admissions Data]
    API --> Documents[Documents]
    API --> Chat[Chat]
    API --> Handoff[Handoff]
    API --> Evaluation[Evaluation]
    API --> Common[Common: Guards, Validation, Error Handling]

    Auth --> DB[(SQL Server)]
    Users --> DB
    Admissions --> DB
    Documents --> DB
    Chat --> DB
    Handoff --> DB
    Evaluation --> DB
    Documents --> AIClient[AI Service Client]
    Chat --> AIClient
```

