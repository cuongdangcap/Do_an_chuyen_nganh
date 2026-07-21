# Bá»™ tÃ i liá»‡u thiáº¿t káº¿ chi tiáº¿t

## Má»¥c Ä‘Ã­ch

Bá»™ tÃ i liá»‡u nÃ y chuyá»ƒn báº£n phÆ°Æ¡ng Ã¡n tá»•ng thá»ƒ thÃ nh thiáº¿t káº¿ Ä‘á»§ chi tiáº¿t Ä‘á»ƒ sau nÃ y code khÃ´ng bá»‹ lá»‡ch. Thá»© tá»± Ä‘á»c Ä‘á» xuáº¥t:

1. [01_mvp_scope.md](01_mvp_scope.md): Chá»‘t pháº¡m vi MVP, chá»©c nÄƒng báº¯t buá»™c/nÃ¢ng cao, tiÃªu chÃ­ hoÃ n thÃ nh.
2. [02_srs_use_cases.md](02_srs_use_cases.md): SRS, actor, quyá»n, yÃªu cáº§u chá»©c nÄƒng/phi chá»©c nÄƒng, use case.
3. [03_database_design_sql_server.md](03_database_design_sql_server.md): Thiáº¿t káº¿ SQL Server, báº£ng, cá»™t, khÃ³a, index, quan há»‡.
4. [04_api_contract.md](04_api_contract.md): API contract, endpoint, request/response, auth, lá»—i chuáº©n.
5. [05_ai_pipeline_design.md](05_ai_pipeline_design.md): AI/RAG pipeline, Qdrant, FAISS, intent classifier, evaluation.
6. [06_diagrams.md](06_diagrams.md): Bá»™ sÆ¡ Ä‘á»“ Mermaid cho bÃ¡o cÃ¡o vÃ  triá»ƒn khai.
7. [07_project_roadmap.md](07_project_roadmap.md): Roadmap triá»ƒn khai theo phase, má»‘c demo, rá»§i ro.
8. [08_implementation_backlog.md](08_implementation_backlog.md): Backlog task chi tiáº¿t theo epic, priority, dependency.
9. [09_auth_rbac_test_checklist.md](09_auth_rbac_test_checklist.md): Checklist test register/login/profile/admin users cho module Auth/RBAC.
10. [10_admissions_data_test_checklist.md](10_admissions_data_test_checklist.md): Checklist test public/admin admissions data API.
11. [11_local_api_test_report.md](11_local_api_test_report.md): Ket qua build, database setup, API test local.
12. [12_frontend_portal_admin_report.md](12_frontend_portal_admin_report.md): Ket qua frontend cong thong tin va cong quan tri.
13. [13_documents_rag_ingestion_report.md](13_documents_rag_ingestion_report.md): Ket qua module upload tai lieu va ingestion RAG.
14. [14_tesseract_qdrant_rag_report.md](14_tesseract_qdrant_rag_report.md): Ket qua Tesseract OCR, vector upsert, retrieval API va chat RAG.
15. [15_chat_feedback_report.md](15_chat_feedback_report.md): Ket qua chat history, source logging va feedback RAG.
16. [16_evaluation_runner_report.md](16_evaluation_runner_report.md): Ket qua Evaluation Runner va golden questions cho RAG.
17. [20_cmcu_data_scope_report.md](20_cmcu_data_scope_report.md): Chot pham vi du lieu theo Truong Dai hoc CMC va kich ban demo.
17. [17_handoff_support_report.md](17_handoff_support_report.md): Ket qua staff handoff va live support ticket.
18. [18_chat_history_ui_report.md](18_chat_history_ui_report.md): Ket qua chat history va ChatGPT-like UI.
19. [19_completion_hardening_report.md](19_completion_hardening_report.md): Hoan thien Qdrant, LLM adapter, chat upload, SignalR, dashboard, smoke test, deployment va security baseline.
20. [admissions_ai_system_plan.md](admissions_ai_system_plan.md): PhÆ°Æ¡ng Ã¡n tá»•ng thá»ƒ ban Ä‘áº§u, dÃ¹ng nhÆ° tÃ i liá»‡u ná»n.

## Stack Ä‘Ã£ chá»‘t

| ThÃ nh pháº§n | CÃ´ng nghá»‡ |
|---|---|
| Frontend | React + Vite hoáº·c Next.js |
| Backend | ASP.NET Core Web API |
| ORM | Entity Framework Core |
| Relational DB | SQL Server |
| Vector DB | Qdrant |
| Vector benchmark | FAISS |
| AI service | Python FastAPI |
| Auth | JWT + refresh token + RBAC |
| Realtime | SignalR hoáº·c polling trong MVP |
| Local deploy | Docker Compose |

## Nhá»¯ng quyáº¿t Ä‘á»‹nh quan trá»ng

- KhÃ´ng tá»± train LLM lá»›n tá»« Ä‘áº§u.
- LLM chá»‰ lÃ  bÆ°á»›c sinh cÃ¢u tráº£ lá»i cuá»‘i pipeline.
- Pháº§n há»c mÃ¡y cáº§n chá»©ng minh báº±ng embedding, vector search, intent classifier, reranking/evaluation, feedback mining.
- SQL Server lÆ°u nghiá»‡p vá»¥ vÃ  metadata; Qdrant lÆ°u vector; FAISS dÃ¹ng benchmark/prototype.
- Backend chÃ­nh lÃ  modular monolith, AI service tÃ¡ch riÃªng.
- CÃ¢u tráº£ lá»i tuyá»ƒn sinh pháº£i cÃ³ nguá»“n hoáº·c pháº£i bÃ¡o khÃ´ng Ä‘á»§ dá»¯ liá»‡u.
- Frontend chá»‘t lÃ  `React + Vite`.
- Staff reply Ä‘i theo `polling` á»Ÿ MVP; `SignalR` lÃ  bÆ°á»›c nÃ¢ng cáº¥p tiáº¿p theo.
- Embedding baseline Æ°u tiÃªn model local nháº¹ trÆ°á»›c khi thá»­ model lá»›n hÆ¡n.

## Checklist trÆ°á»›c khi code

- [ ] Tháº§y/nhÃ³m Ä‘á»“ng Ã½ pháº¡m vi MVP trong `01_mvp_scope.md`.
- [ ] Chá»‘t frontend: React + Vite hay Next.js.
- [ ] Chá»‘t dÃ¹ng SignalR ngay hay polling trÆ°á»›c.
- [ ] Chá»‘t embedding model local Ä‘áº§u tiÃªn.
- [ ] Chuáº©n bá»‹ tÃ i liá»‡u tuyá»ƒn sinh máº«u PDF/DOCX/áº£nh.
- [ ] Chuáº©n bá»‹ seed data ngÃ nh, Ä‘iá»ƒm chuáº©n, há»c phÃ­.
- [ ] Chuáº©n bá»‹ 50 golden questions Ä‘áº§u tiÃªn.
- [ ] Chá»‘t convention source code.
- [ ] Duyá»‡t roadmap trong `07_project_roadmap.md`.
- [ ] Duyá»‡t backlog P0 trong `08_implementation_backlog.md`.
- [ ] Táº¡o repo structure theo tÃ i liá»‡u.

## Khi báº¯t Ä‘áº§u code

Thá»© tá»± code khuyáº¿n nghá»‹:

1. Setup solution/repo/Docker/env.
2. SQL Server + EF Core migration.
3. Auth/RBAC.
4. Admissions CRUD.
5. Public/admin UI.
6. Documents upload + ingestion.
7. Qdrant vector search.
8. Chat UI + RAG.
9. Feedback + handoff.
10. Evaluation + bÃ¡o cÃ¡o.
