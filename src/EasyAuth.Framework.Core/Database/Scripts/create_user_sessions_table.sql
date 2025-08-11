-- Create user sessions table for session management
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[eauth].[user_sessions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [eauth].[user_sessions](
        [id] [int] IDENTITY(1,1) NOT NULL,
        [session_id] [nvarchar](255) NOT NULL,
        [user_id] [uniqueidentifier] NOT NULL,
        [auth_provider] [nvarchar](50) NOT NULL,
        [ip_address] [nvarchar](45) NULL,
        [user_agent] [nvarchar](500) NULL,
        [created_date] [datetimeoffset](7) NOT NULL DEFAULT GETUTCDATE(),
        [expires_at] [datetimeoffset](7) NOT NULL,
        [last_activity] [datetimeoffset](7) NOT NULL DEFAULT GETUTCDATE(),
        [is_active] [bit] NOT NULL DEFAULT 1,
        [invalidated_date] [datetimeoffset](7) NULL,
        [invalidated_reason] [nvarchar](255) NULL,
        [session_data] [nvarchar](max) NULL, -- JSON data for session state
        CONSTRAINT [PK_eauth_user_sessions] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [FK_eauth_user_sessions_users] FOREIGN KEY ([user_id]) REFERENCES [eauth].[users]([user_id]),
        CONSTRAINT [IX_eauth_user_sessions_session_id] UNIQUE NONCLUSTERED ([session_id] ASC),
        INDEX [IX_eauth_user_sessions_user_id] NONCLUSTERED ([user_id] ASC),
        INDEX [IX_eauth_user_sessions_expires_at] NONCLUSTERED ([expires_at] ASC),
        INDEX [IX_eauth_user_sessions_is_active] NONCLUSTERED ([is_active] ASC),
        INDEX [IX_eauth_user_sessions_last_activity] NONCLUSTERED ([last_activity] ASC)
    )

    PRINT 'Created eauth.user_sessions table'
END
ELSE
BEGIN
    PRINT 'eauth.user_sessions table already exists'
END
