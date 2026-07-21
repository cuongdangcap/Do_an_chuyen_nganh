IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [roles] (
    [Id] uniqueidentifier NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    CONSTRAINT [PK_roles] PRIMARY KEY ([Id])
);

CREATE TABLE [users] (
    [Id] uniqueidentifier NOT NULL,
    [Email] nvarchar(255) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [FullName] nvarchar(255) NOT NULL,
    [Phone] nvarchar(30) NULL,
    [AvatarUrl] nvarchar(500) NULL,
    [Status] nvarchar(50) NOT NULL,
    [EmailVerifiedAt] datetime2 NULL,
    [LastLoginAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_users] PRIMARY KEY ([Id])
);

CREATE TABLE [parent_profiles] (
    [UserId] uniqueidentifier NOT NULL,
    [Occupation] nvarchar(255) NULL,
    [Province] nvarchar(100) NULL,
    [ContactPreference] nvarchar(50) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_parent_profiles] PRIMARY KEY ([UserId]),
    CONSTRAINT [FK_parent_profiles_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [refresh_tokens] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [TokenHash] nvarchar(max) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [RevokedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedByIp] nvarchar(50) NULL,
    CONSTRAINT [PK_refresh_tokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_refresh_tokens_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [staff_profiles] (
    [UserId] uniqueidentifier NOT NULL,
    [Department] nvarchar(255) NULL,
    [Position] nvarchar(255) NULL,
    [CanManageDocuments] bit NOT NULL,
    [CanReplyChat] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_staff_profiles] PRIMARY KEY ([UserId]),
    CONSTRAINT [FK_staff_profiles_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [student_profiles] (
    [UserId] uniqueidentifier NOT NULL,
    [HighSchool] nvarchar(255) NULL,
    [Province] nvarchar(100) NULL,
    [GraduationYear] int NULL,
    [ExpectedScore] decimal(5,2) NULL,
    [ExamScore] decimal(5,2) NULL,
    [InterestedSubjectGroup] nvarchar(50) NULL,
    [Notes] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_student_profiles] PRIMARY KEY ([UserId]),
    CONSTRAINT [FK_student_profiles_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [user_roles] (
    [UserId] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    [AssignedAt] datetime2 NOT NULL,
    [AssignedBy] uniqueidentifier NULL,
    CONSTRAINT [PK_user_roles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_user_roles_roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_user_roles_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [users] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_refresh_tokens_UserId] ON [refresh_tokens] ([UserId]);

CREATE UNIQUE INDEX [IX_roles_Code] ON [roles] ([Code]);

CREATE INDEX [IX_user_roles_RoleId] ON [user_roles] ([RoleId]);

CREATE UNIQUE INDEX [IX_users_Email] ON [users] ([Email]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260625145552_InitialAuthSchema', N'10.0.0');

COMMIT;
GO

