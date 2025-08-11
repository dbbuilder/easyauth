-- Create user accounts table for linking multiple provider accounts to a single user
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[eauth].[user_accounts]') AND type in (N'U'))
BEGIN
    CREATE TABLE [eauth].[user_accounts](
        [id] [int] IDENTITY(1,1) NOT NULL,
        [user_id] [uniqueidentifier] NOT NULL,
        [provider] [nvarchar](50) NOT NULL,
        [provider_id] [nvarchar](255) NOT NULL,
        [provider_email] [nvarchar](255) NULL,
        [provider_display_name] [nvarchar](255) NULL,
        [provider_data] [nvarchar](max) NULL, -- JSON data from provider
        [is_primary] [bit] NOT NULL DEFAULT 0,
        [is_active] [bit] NOT NULL DEFAULT 1,
        [linked_date] [datetimeoffset](7) NOT NULL DEFAULT GETUTCDATE(),
        [last_used_date] [datetimeoffset](7) NULL,
        [created_by] [nvarchar](255) NULL,
        CONSTRAINT [PK_eauth_user_accounts] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [FK_eauth_user_accounts_users] FOREIGN KEY ([user_id]) REFERENCES [eauth].[users]([user_id]),
        CONSTRAINT [IX_eauth_user_accounts_provider_id] UNIQUE NONCLUSTERED ([provider] ASC, [provider_id] ASC),
        INDEX [IX_eauth_user_accounts_user_id] NONCLUSTERED ([user_id] ASC),
        INDEX [IX_eauth_user_accounts_provider] NONCLUSTERED ([provider] ASC),
        INDEX [IX_eauth_user_accounts_is_primary] NONCLUSTERED ([is_primary] ASC),
        INDEX [IX_eauth_user_accounts_is_active] NONCLUSTERED ([is_active] ASC)
    )

    PRINT 'Created eauth.user_accounts table'
END
ELSE
BEGIN
    PRINT 'eauth.user_accounts table already exists'
END
