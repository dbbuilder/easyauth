-- Create EAuth schema for all framework objects
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'eauth')
BEGIN
    EXEC('CREATE SCHEMA [eauth]')
    PRINT 'Created eauth schema'
END
ELSE
BEGIN
    PRINT 'eauth schema already exists'
END

-- Create framework metadata table for tracking versions and configuration
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[eauth].[framework_metadata]') AND type in (N'U'))
BEGIN
    CREATE TABLE [eauth].[framework_metadata](
        [id] [int] IDENTITY(1,1) NOT NULL,
        [key] [nvarchar](255) NOT NULL,
        [value] [nvarchar](max) NULL,
        [version] [nvarchar](50) NOT NULL,
        [created_date] [datetimeoffset](7) NOT NULL,
        [created_by] [nvarchar](255) NULL,
        CONSTRAINT [PK_eauth_framework_metadata] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [IX_eauth_framework_metadata_key] UNIQUE NONCLUSTERED ([key] ASC, [version] ASC)
    )

    PRINT 'Created eauth.framework_metadata table'
END
ELSE
BEGIN
    PRINT 'eauth.framework_metadata table already exists'
END
