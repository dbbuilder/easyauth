-- Stored procedure to validate and update user session
CREATE OR ALTER PROCEDURE [eauth].[EAuth_ValidateSession]
    @SessionId NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON
    
    DECLARE @IsValid BIT = 0
    DECLARE @UserId UNIQUEIDENTIFIER
    DECLARE @ExpiresAt DATETIMEOFFSET
    
    -- Check if session exists and is valid
    SELECT 
        @IsValid = CASE 
            WHEN is_active = 1 AND expires_at > GETUTCDATE() THEN 1 
            ELSE 0 
        END,
        @UserId = user_id,
        @ExpiresAt = expires_at
    FROM [eauth].[user_sessions]
    WHERE session_id = @SessionId
    
    -- Update last activity if session is valid
    IF @IsValid = 1
    BEGIN
        UPDATE [eauth].[user_sessions]
        SET last_activity = GETUTCDATE()
        WHERE session_id = @SessionId
    END
    
    -- Return complete session and user information
    SELECT 
        s.session_id,
        s.user_id,
        s.auth_provider,
        s.ip_address,
        s.user_agent,
        s.created_date,
        s.expires_at,
        s.last_activity,
        s.is_active,
        @IsValid AS is_valid,
        u.email,
        u.display_name,
        u.first_name,
        u.last_name,
        u.profile_picture_url,
        u.email_verified
    FROM [eauth].[user_sessions] s
    LEFT JOIN [eauth].[users] u ON s.user_id = u.user_id
    WHERE s.session_id = @SessionId
END
