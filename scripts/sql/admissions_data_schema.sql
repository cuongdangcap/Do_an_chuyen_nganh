BEGIN TRANSACTION;
CREATE TABLE [admission_cycles] (
    [Id] uniqueidentifier NOT NULL,
    [Year] int NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [StartDate] date NULL,
    [EndDate] date NULL,
    [Status] nvarchar(50) NOT NULL,
    CONSTRAINT [PK_admission_cycles] PRIMARY KEY ([Id])
);

CREATE TABLE [admission_methods] (
    [Id] uniqueidentifier NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Status] nvarchar(50) NOT NULL,
    CONSTRAINT [PK_admission_methods] PRIMARY KEY ([Id])
);

CREATE TABLE [faculties] (
    [Id] uniqueidentifier NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Status] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_faculties] PRIMARY KEY ([Id])
);

CREATE TABLE [faqs] (
    [Id] uniqueidentifier NOT NULL,
    [Category] nvarchar(100) NULL,
    [Question] nvarchar(max) NOT NULL,
    [Answer] nvarchar(max) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_faqs] PRIMARY KEY ([Id])
);

CREATE TABLE [subject_combinations] (
    [Id] uniqueidentifier NOT NULL,
    [Code] nvarchar(20) NOT NULL,
    [Subjects] nvarchar(255) NOT NULL,
    [Description] nvarchar(500) NULL,
    CONSTRAINT [PK_subject_combinations] PRIMARY KEY ([Id])
);

CREATE TABLE [majors] (
    [Id] uniqueidentifier NOT NULL,
    [FacultyId] uniqueidentifier NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [Description] nvarchar(max) NULL,
    [CareerOutcomes] nvarchar(max) NULL,
    [Status] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_majors] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_majors_faculties_FacultyId] FOREIGN KEY ([FacultyId]) REFERENCES [faculties] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [programs] (
    [Id] uniqueidentifier NOT NULL,
    [MajorId] uniqueidentifier NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [DegreeType] nvarchar(100) NULL,
    [Language] nvarchar(100) NULL,
    [Campus] nvarchar(255) NULL,
    [DurationYears] decimal(3,1) NULL,
    [Description] nvarchar(max) NULL,
    [Status] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_programs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_programs_majors_MajorId] FOREIGN KEY ([MajorId]) REFERENCES [majors] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [cutoff_scores] (
    [Id] uniqueidentifier NOT NULL,
    [ProgramId] uniqueidentifier NOT NULL,
    [AdmissionCycleId] uniqueidentifier NOT NULL,
    [AdmissionMethodId] uniqueidentifier NOT NULL,
    [SubjectCombinationId] uniqueidentifier NULL,
    [Score] decimal(5,2) NOT NULL,
    [Note] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_cutoff_scores] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_cutoff_scores_admission_cycles_AdmissionCycleId] FOREIGN KEY ([AdmissionCycleId]) REFERENCES [admission_cycles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_cutoff_scores_admission_methods_AdmissionMethodId] FOREIGN KEY ([AdmissionMethodId]) REFERENCES [admission_methods] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_cutoff_scores_programs_ProgramId] FOREIGN KEY ([ProgramId]) REFERENCES [programs] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_cutoff_scores_subject_combinations_SubjectCombinationId] FOREIGN KEY ([SubjectCombinationId]) REFERENCES [subject_combinations] ([Id])
);

CREATE TABLE [program_subject_combinations] (
    [ProgramId] uniqueidentifier NOT NULL,
    [SubjectCombinationId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_program_subject_combinations] PRIMARY KEY ([ProgramId], [SubjectCombinationId]),
    CONSTRAINT [FK_program_subject_combinations_programs_ProgramId] FOREIGN KEY ([ProgramId]) REFERENCES [programs] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_program_subject_combinations_subject_combinations_SubjectCombinationId] FOREIGN KEY ([SubjectCombinationId]) REFERENCES [subject_combinations] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [tuition_fees] (
    [Id] uniqueidentifier NOT NULL,
    [ProgramId] uniqueidentifier NOT NULL,
    [AcademicYear] nvarchar(20) NOT NULL,
    [AmountMin] decimal(18,2) NULL,
    [AmountMax] decimal(18,2) NULL,
    [Currency] nvarchar(10) NOT NULL,
    [Unit] nvarchar(50) NOT NULL,
    [Note] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_tuition_fees] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_tuition_fees_programs_ProgramId] FOREIGN KEY ([ProgramId]) REFERENCES [programs] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_admission_cycles_Year] ON [admission_cycles] ([Year]);

CREATE UNIQUE INDEX [IX_admission_methods_Code] ON [admission_methods] ([Code]);

CREATE INDEX [IX_cutoff_scores_AdmissionCycleId] ON [cutoff_scores] ([AdmissionCycleId]);

CREATE INDEX [IX_cutoff_scores_AdmissionMethodId] ON [cutoff_scores] ([AdmissionMethodId]);

CREATE INDEX [IX_cutoff_scores_ProgramId_AdmissionCycleId_AdmissionMethodId_SubjectCombinationId] ON [cutoff_scores] ([ProgramId], [AdmissionCycleId], [AdmissionMethodId], [SubjectCombinationId]);

CREATE INDEX [IX_cutoff_scores_SubjectCombinationId] ON [cutoff_scores] ([SubjectCombinationId]);

CREATE UNIQUE INDEX [IX_faculties_Code] ON [faculties] ([Code]);

CREATE UNIQUE INDEX [IX_majors_Code] ON [majors] ([Code]);

CREATE INDEX [IX_majors_FacultyId] ON [majors] ([FacultyId]);

CREATE INDEX [IX_program_subject_combinations_SubjectCombinationId] ON [program_subject_combinations] ([SubjectCombinationId]);

CREATE UNIQUE INDEX [IX_programs_Code] ON [programs] ([Code]);

CREATE INDEX [IX_programs_MajorId] ON [programs] ([MajorId]);

CREATE UNIQUE INDEX [IX_subject_combinations_Code] ON [subject_combinations] ([Code]);

CREATE INDEX [IX_tuition_fees_ProgramId_AcademicYear] ON [tuition_fees] ([ProgramId], [AcademicYear]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260625152108_AdmissionsDataSchema', N'10.0.0');

COMMIT;
GO

