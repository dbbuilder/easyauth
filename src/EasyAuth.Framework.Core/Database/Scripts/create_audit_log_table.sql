-- Create audit log table for security and compliance tracking
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[eauth].[audit_log]') AND type in (N'U'))
BEGIN
    CREATE TABLE [eauth].[audit_log](
        [id] [bigint] IDENTITY(1,1) NOT NULL,
        [event_type] [nvarchar](100) NOT NULL,
        [user_id] [uniqueidentifier] NULL,
        [session_id] [nvarchar](255) NULL,
        [provider] [nvarchar](50) NULL,
        [ip_address] [nvarchar](45) NULL,
        [user_agent] [nvarchar](500) NULL,
        [event_data] [nvarchar](max) NULL, -- JSON data
        [result] [nvarchar](50) NULL,
        [error_message] [nvarchar](1000) NULL,
        [created_date] [datetimeoffset](7) NOT NULL DEFAULT GETUTCDATE(),
        [correlation_id] [nvarchar](100) NULL,
        CONSTRAINT [PK_eauth_audit_log] PRIMARY KEY CLUSTERED ([id] ASC),
        INDEX [IX_eauth_audit_log_event_type] NONCLUSTERED ([event_type] ASC),
        INDEX [IX_eauth_audit_log_user_id] NONCLUSTERED ([user_id] ASC),
        INDEX [IX_eauth_audit_log_created_date] NONCLUSTERED ([created_date] ASC),
        INDEX [IX_eauth_audit_log_correlation_id] NONCLUSTERED ([correlation_id] ASC),
        INDEX [IX_eauth_audit_log_provider] NONCLUSTERED ([provider] ASC),
        INDEX [IX_eauth_audit_log_result] NONCLUSTERED ([result] ASC)
    )
    
    PRINT 'Created eauth.audit_log table'
END
ELSE
BEGIN
    PRINT 'eauth.audit_log table already exists'
END
