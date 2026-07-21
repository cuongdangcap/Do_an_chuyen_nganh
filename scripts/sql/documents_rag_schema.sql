BEGIN TRANSACTION;
CREATE TABLE [knowledge_documents] (
    [Id] uniqueidentifier NOT NULL,
    [Title] nvarchar(255) NOT NULL,
    [DocumentType] nvarchar(50) NOT NULL,
    [Source] nvarchar(500) NULL,
    [Status] nvarchar(50) NOT NULL,
    [UploadedBy] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_knowledge_documents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_knowledge_documents_users_UploadedBy] FOREIGN KEY ([UploadedBy]) REFERENCES [users] ([Id]) ON DELETE SET NULL
);

CREATE TABLE [document_versions] (
    [Id] uniqueidentifier NOT NULL,
    [DocumentId] uniqueidentifier NOT NULL,
    [VersionNo] int NOT NULL,
    [FileName] nvarchar(255) NOT NULL,
    [FilePath] nvarchar(1000) NOT NULL,
    [FileType] nvarchar(20) NOT NULL,
    [ContentType] nvarchar(100) NULL,
    [FileSizeBytes] bigint NOT NULL,
    [Checksum] nvarchar(128) NULL,
    [ProcessingStatus] nvarchar(50) NOT NULL,
    [ErrorMessage] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_document_versions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_document_versions_knowledge_documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [knowledge_documents] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [document_chunks] (
    [Id] uniqueidentifier NOT NULL,
    [DocumentVersionId] uniqueidentifier NOT NULL,
    [ChunkIndex] int NOT NULL,
    [PageNumber] int NULL,
    [SectionTitle] nvarchar(255) NULL,
    [Content] nvarchar(max) NOT NULL,
    [TokenCount] int NULL,
    [QdrantCollection] nvarchar(100) NOT NULL,
    [QdrantPointId] nvarchar(100) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_document_chunks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_document_chunks_document_versions_DocumentVersionId] FOREIGN KEY ([DocumentVersionId]) REFERENCES [document_versions] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ingestion_jobs] (
    [Id] uniqueidentifier NOT NULL,
    [DocumentVersionId] uniqueidentifier NOT NULL,
    [JobType] nvarchar(50) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [StartedAt] datetime2 NULL,
    [FinishedAt] datetime2 NULL,
    [ErrorMessage] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ingestion_jobs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ingestion_jobs_document_versions_DocumentVersionId] FOREIGN KEY ([DocumentVersionId]) REFERENCES [document_versions] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_document_chunks_DocumentVersionId_ChunkIndex] ON [document_chunks] ([DocumentVersionId], [ChunkIndex]);

CREATE INDEX [IX_document_chunks_QdrantPointId] ON [document_chunks] ([QdrantPointId]);

CREATE INDEX [IX_document_versions_DocumentId] ON [document_versions] ([DocumentId]);

CREATE INDEX [IX_document_versions_ProcessingStatus] ON [document_versions] ([ProcessingStatus]);

CREATE INDEX [IX_ingestion_jobs_DocumentVersionId] ON [ingestion_jobs] ([DocumentVersionId]);

CREATE INDEX [IX_ingestion_jobs_Status] ON [ingestion_jobs] ([Status]);

CREATE INDEX [IX_knowledge_documents_Status] ON [knowledge_documents] ([Status]);

CREATE INDEX [IX_knowledge_documents_UploadedBy] ON [knowledge_documents] ([UploadedBy]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260626001554_DocumentsRagSchema', N'10.0.0');

COMMIT;
GO

