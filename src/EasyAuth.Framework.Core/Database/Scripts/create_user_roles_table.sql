-- Create user roles table for role-based access control
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[eauth].[user_roles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [eauth].[user_roles](
        [id] [int] IDENTITY(1,1) NOT NULL,
        [user_id] [uniqueidentifier] NOT NULL,
        [role_name] [nvarchar](100) NOT NULL,
        [granted_by] [uniqueidentifier] NULL,
        [granted_date] [datetimeoffset](7) NOT NULL DEFAULT GETUTCDATE(),
        [expires_at] [datetimeoffset](7) NULL,
        [is_active] [bit] NOT NULL DEFAULT 1,
        CONSTRAINT [PK_eauth_user_roles] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [FK_eauth_user_roles_users] FOREIGN KEY ([user_id]) REFERENCES [eauth].[users]([user_id]),
        CONSTRAINT [FK_eauth_user_roles_granted_by] FOREIGN KEY ([granted_by]) REFERENCES [eauth].[users]([user_id]),
        CONSTRAINT [IX_eauth_user_roles_user_role] UNIQUE NONCLUSTERED ([user_id] ASC, [role_name] ASC),
        INDEX [IX_eauth_user_roles_role_name] NONCLUSTERED ([role_name] ASC),
        INDEX [IX_eauth_user_roles_is_active] NONCLUSTERED ([is_active] ASC),
        INDEX [IX_eauth_user_roles_expires_at] NONCLUSTERED ([expires_at] ASC)
    )
    
    PRINT 'Created eauth.user_roles table'
END
ELSE
BEGIN
    PRINT 'eauth.user_roles table already exists'
END
