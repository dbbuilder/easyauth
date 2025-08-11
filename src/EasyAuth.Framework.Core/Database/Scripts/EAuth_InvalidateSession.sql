-- Stored procedure to invalidate user session
CREATE OR ALTER PROCEDURE [eauth].[EAuth_InvalidateSession]
    @SessionId NVARCHAR(255),
    @Reason NVARCHAR(255) = 'USER_LOGOUT'
AS
BEGIN
    SET NOCOUNT ON
    
    DECLARE @UserId UNIQUEIDENTIFIER
    DECLARE @RowsAffected INT
    
    -- Get user ID before invalidating
    SELECT @UserId = user_id
    FROM [eauth].[user_sessions]
    WHERE session_id = @SessionId AND is_active = 1
    
    -- Invalidate session
    UPDATE [eauth].[user_sessions]
    SET 
        is_active = 0,
        invalidated_date = GETUTCDATE(),
        invalidated_reason = @Reason
    WHERE session_id = @SessionId AND is_active = 1
    
    SET @RowsAffected = @@ROWCOUNT
    
    -- Log audit event if session was found and invalidated
    IF @RowsAffected > 0 AND @UserId IS NOT NULL
    BEGIN
        INSERT INTO [eauth].[audit_log] (event_type, user_id, session_id, result, event_data, created_date)
        VALUES ('SESSION_INVALIDATED', @UserId, @SessionId, 'SUCCESS', 
                JSON_OBJECT('reason', @Reason), GETUTCDATE())
    END
    
    -- Return result
    SELECT 
        @RowsAffected AS rows_affected,
        CASE WHEN @RowsAffected > 0 THEN 1 ELSE 0 END AS success
END
