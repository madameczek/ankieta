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
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105112325_InitialSchema'
)
BEGIN
    CREATE TABLE [data_protection_keys] (
        [id] int NOT NULL IDENTITY,
        [friendly_name] nvarchar(max) NULL,
        [xml] nvarchar(max) NULL,
        CONSTRAINT [pk_data_protection_keys] PRIMARY KEY ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105112325_InitialSchema'
)
BEGIN
    CREATE TABLE [polls] (
        [id] int NOT NULL IDENTITY,
        [title] nvarchar(200) NOT NULL,
        [description] nvarchar(2000) NULL,
        [created_at] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [modified_at] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [deleted_at] datetime2 NULL,
        CONSTRAINT [pk_polls] PRIMARY KEY ([id]),
        CONSTRAINT [ck_polls_title_not_empty] CHECK (LEN([title]) > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105112325_InitialSchema'
)
BEGIN
    CREATE TABLE [threshold_warnings] (
        [resource] varchar(20) NOT NULL,
        [sent_at] datetime2 NOT NULL,
        CONSTRAINT [pk_threshold_warnings] PRIMARY KEY ([resource]),
        CONSTRAINT [ck_threshold_warnings_resource] CHECK ([resource] IN ('users', 'polls'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105112325_InitialSchema'
)
BEGIN
    CREATE TABLE [users] (
        [id] int NOT NULL IDENTITY,
        [name] nvarchar(100) NOT NULL,
        [created_at] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [pk_users] PRIMARY KEY ([id]),
        CONSTRAINT [ck_users_name_not_empty] CHECK (LEN([name]) > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105112325_InitialSchema'
)
BEGIN
    CREATE TABLE [questions] (
        [id] int NOT NULL IDENTITY,
        [id_poll] int NOT NULL,
        [sort_order] int NOT NULL,
        [question] nvarchar(1000) NOT NULL,
        [modified_at] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [deleted_at] datetime2 NULL,
        CONSTRAINT [pk_questions] PRIMARY KEY ([id]),
        CONSTRAINT [ck_questions_question_not_empty] CHECK (LEN([question]) > 0),
        CONSTRAINT [fk_questions_polls] FOREIGN KEY ([id_poll]) REFERENCES [polls] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105112325_InitialSchema'
)
BEGIN
    CREATE TABLE [user_poll] (
        [id_poll] int NOT NULL,
        [id_user] int NOT NULL,
        [is_owner] bit NOT NULL,
        CONSTRAINT [pk_user_poll] PRIMARY KEY ([id_poll], [id_user]),
        CONSTRAINT [fk_user_poll_polls] FOREIGN KEY ([id_poll]) REFERENCES [polls] ([id]),
        CONSTRAINT [fk_user_poll_users] FOREIGN KEY ([id_user]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105112325_InitialSchema'
)
BEGIN
    CREATE TABLE [answers] (
        [id] int NOT NULL IDENTITY,
        [id_question] int NOT NULL,
        [id_user] int NOT NULL,
        [answer] nvarchar(4000) NOT NULL,
        [modified_at] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [pk_answers] PRIMARY KEY ([id]),
        CONSTRAINT [ck_answers_answer_not_empty] CHECK (LEN([answer]) > 0),
        CONSTRAINT [fk_answers_questions] FOREIGN KEY ([id_question]) REFERENCES [questions] ([id]),
        CONSTRAINT [fk_answers_users] FOREIGN KEY ([id_user]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105112325_InitialSchema'
)
BEGIN
    CREATE INDEX [ix_answers_user] ON [answers] ([id_user]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105112325_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [ux_answers_question_user] ON [answers] ([id_question], [id_user]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105112325_InitialSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [ux_questions_poll_sort_order] ON [questions] ([id_poll], [sort_order]) WHERE [deleted_at] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105112325_InitialSchema'
)
BEGIN
    CREATE INDEX [ix_user_poll_user] ON [user_poll] ([id_user]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105112325_InitialSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [ux_user_poll_owner] ON [user_poll] ([id_poll]) WHERE [is_owner] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105112325_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [ux_users_name] ON [users] ([name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105112325_InitialSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261105112325_InitialSchema', N'10.0.14');
END;

COMMIT;
GO

