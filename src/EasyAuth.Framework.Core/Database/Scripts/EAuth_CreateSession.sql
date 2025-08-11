-- Stored procedure to create a new user session
CREATE OR ALTER PROCEDURE [eauth].[EAuth_CreateSession]
    @SessionId NVARCHAR(255),
    @UserId UNIQUEIDENTIFIER,
    @AuthProvider NVARCHAR(50),
    @IpAddress NVARCHAR(45) = NULL,
    @UserAgent NVARCHAR(500) = NULL,
    @ExpiresAt DATETIMEOFFSET,
    @SessionData NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON

    BEGIN TRY
        -- Invalidate any existing active sessions for this user (optional - for single session per user)
        -- Uncomment the following lines if you want to enforce single session per user
        /*
        UPDATE [eauth].[user_sessions]
        SET is_active = 0, invalidated_date = GETUTCDATE(), invalidated_reason = 'NEW_SESSION_CREATED'
        WHERE user_id = @UserId AND is_active = 1
        */

        -- Insert new session
        INSERT INTO [eauth].[user_sessions] (
            session_id, user_id, auth_provider, ip_address, user_agent,
            created_date, expires_at, last_activity, is_active, session_data
        )
        VALUES (
            @SessionId, @UserId, @AuthProvider, @IpAddress, @UserAgent,
            GETUTCDATE(), @ExpiresAt, GETUTCDATE(), 1, @SessionData
        )

        -- Log audit event
        INSERT INTO [eauth].[audit_log] (event_type, user_id, session_id, provider, ip_address, user_agent, result, created_date)
        VALUES ('SESSION_CREATED', @UserId, @SessionId, @AuthProvider, @IpAddress, @UserAgent, 'SUCCESS', GETUTCDATE())

        -- Return session information
        SELECT
            session_id,
            user_id,
            auth_provider,
            created_date,
            expires_at,
            last_activity,
            is_active
        FROM [eauth].[user_sessions]
        WHERE session_id = @SessionId

    END TRY
    BEGIN CATCH
        -- Log error
        INSERT INTO [eauth].[audit_log] (event_type, user_id, session_id, provider, result, error_message, created_date)
        VALUES ('SESSION_CREATE_ERROR', @UserId, @SessionId, @AuthProvider, 'ERROR', ERROR_MESSAGE(), GETUTCDATE())

        THROW
    END CATCH
END
