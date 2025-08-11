-- Stored procedure to get user with all linked accounts and roles
CREATE OR ALTER PROCEDURE [eauth].[EAuth_GetUserProfile]
    @UserId UNIQUEIDENTIFIER = NULL,
    @Email NVARCHAR(255) = NULL,
    @ProviderId NVARCHAR(255) = NULL,
    @Provider NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON

    DECLARE @TargetUserId UNIQUEIDENTIFIER

    -- Determine user ID from various inputs
    IF @UserId IS NOT NULL
    BEGIN
        SET @TargetUserId = @UserId
    END
    ELSE IF @Email IS NOT NULL
    BEGIN
        SELECT @TargetUserId = user_id
        FROM [eauth].[users]
        WHERE email = @Email AND is_active = 1
    END
    ELSE IF @ProviderId IS NOT NULL AND @Provider IS NOT NULL
    BEGIN
        SELECT @TargetUserId = u.user_id
        FROM [eauth].[users] u
        INNER JOIN [eauth].[user_accounts] ua ON u.user_id = ua.user_id
        WHERE ua.provider = @Provider AND ua.provider_id = @ProviderId AND ua.is_active = 1
    END

    IF @TargetUserId IS NULL
    BEGIN
        -- Return empty result set with same structure
        SELECT
            CAST(NULL AS UNIQUEIDENTIFIER) AS user_id,
            CAST(NULL AS NVARCHAR(255)) AS email,
            CAST(NULL AS NVARCHAR(255)) AS display_name,
            CAST(NULL AS NVARCHAR(255)) AS first_name,
            CAST(NULL AS NVARCHAR(255)) AS last_name,
            CAST(NULL AS NVARCHAR(1000)) AS profile_picture_url,
            CAST(NULL AS NVARCHAR(50)) AS primary_provider,
            CAST(NULL AS DATETIMEOFFSET) AS last_login_date,
            CAST(0 AS BIT) AS email_verified,
            CAST(0 AS BIT) AS is_active
        WHERE 1 = 0

        RETURN
    END

    -- Return user profile
    SELECT
        u.user_id,
        u.email,
        u.display_name,
        u.first_name,
        u.last_name,
        u.profile_picture_url,
        u.primary_provider,
        u.last_login_date,
        u.email_verified,
        u.is_active
    FROM [eauth].[users] u
    WHERE u.user_id = @TargetUserId

    -- Return linked accounts
    SELECT
        ua.provider,
        ua.provider_id,
        ua.provider_email,
        ua.provider_display_name,
        ua.is_primary,
        ua.linked_date,
        ua.last_used_date
    FROM [eauth].[user_accounts] ua
    WHERE ua.user_id = @TargetUserId AND ua.is_active = 1
    ORDER BY ua.is_primary DESC, ua.linked_date ASC

    -- Return user roles
    SELECT
        ur.role_name,
        ur.granted_date,
        ur.expires_at,
        ur.is_active,
        gu.display_name AS granted_by_name
    FROM [eauth].[user_roles] ur
    LEFT JOIN [eauth].[users] gu ON ur.granted_by = gu.user_id
    WHERE ur.user_id = @TargetUserId
      AND ur.is_active = 1
      AND (ur.expires_at IS NULL OR ur.expires_at > GETUTCDATE())
    ORDER BY ur.role_name
END
