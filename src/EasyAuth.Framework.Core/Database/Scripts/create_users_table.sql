-- Create users table for unified user management across providers
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[eauth].[users]') AND type in (N'U'))
BEGIN
    CREATE TABLE [eauth].[users](
        [id] [int] IDENTITY(1,1) NOT NULL,
        [user_id] [uniqueidentifier] NOT NULL DEFAULT NEWID(),
        [email] [nvarchar](255) NOT NULL,
        [email_verified] [bit] NOT NULL DEFAULT 0,
        [display_name] [nvarchar](255) NULL,
        [first_name] [nvarchar](255) NULL,
        [last_name] [nvarchar](255) NULL,
        [profile_picture_url] [nvarchar](1000) NULL,
        [primary_provider] [nvarchar](50) NOT NULL,
        [is_active] [bit] NOT NULL DEFAULT 1,
        [created_date] [datetimeoffset](7) NOT NULL DEFAULT GETUTCDATE(),
        [modified_date] [datetimeoffset](7) NULL,
        [last_login_date] [datetimeoffset](7) NULL,
        [created_by] [nvarchar](255) NULL,
        [modified_by] [nvarchar](255) NULL,
        CONSTRAINT [PK_eauth_users] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [IX_eauth_users_user_id] UNIQUE NONCLUSTERED ([user_id] ASC),
        CONSTRAINT [IX_eauth_users_email] UNIQUE NONCLUSTERED ([email] ASC),
        INDEX [IX_eauth_users_primary_provider] NONCLUSTERED ([primary_provider] ASC),
        INDEX [IX_eauth_users_is_active] NONCLUSTERED ([is_active] ASC),
        INDEX [IX_eauth_users_last_login] NONCLUSTERED ([last_login_date] ASC)
    )
    
    PRINT 'Created eauth.users table'
END
ELSE
BEGIN
    PRINT 'eauth.users table already exists'
END
