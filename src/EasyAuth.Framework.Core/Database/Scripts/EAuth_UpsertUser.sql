-- Stored procedure to create or update user from authentication provider
CREATE OR ALTER PROCEDURE [eauth].[EAuth_UpsertUser]
    @ProviderId NVARCHAR(255),
    @Provider NVARCHAR(50),
    @Email NVARCHAR(255),
    @EmailVerified BIT = 0,
    @DisplayName NVARCHAR(255) = NULL,
    @FirstName NVARCHAR(255) = NULL,
    @LastName NVARCHAR(255) = NULL,
    @ProfilePictureUrl NVARCHAR(1000) = NULL,
    @ProviderData NVARCHAR(MAX) = NULL,
    @CreatedBy NVARCHAR(255) = 'SYSTEM'
AS
BEGIN
    SET NOCOUNT ON

    DECLARE @UserId UNIQUEIDENTIFIER
    DECLARE @IsNewUser BIT = 0

    BEGIN TRY
        BEGIN TRANSACTION

        -- Check if user account already exists for this provider
        SELECT @UserId = u.user_id
        FROM [eauth].[users] u
        INNER JOIN [eauth].[user_accounts] ua ON u.user_id = ua.user_id
        WHERE ua.provider = @Provider AND ua.provider_id = @ProviderId

        -- If no existing account, check if user exists by email for account linking
        IF @UserId IS NULL AND @Email IS NOT NULL
        BEGIN
            SELECT @UserId = user_id
            FROM [eauth].[users]
            WHERE email = @Email AND is_active = 1
        END

        -- Create new user if none exists
        IF @UserId IS NULL
        BEGIN
            SET @UserId = NEWID()
            SET @IsNewUser = 1

            INSERT INTO [eauth].[users] (
                user_id, email, email_verified, display_name, first_name, last_name,
                profile_picture_url, primary_provider, created_date, created_by, last_login_date
            )
            VALUES (
                @UserId, @Email, @EmailVerified, @DisplayName, @FirstName, @LastName,
                @ProfilePictureUrl, @Provider, GETUTCDATE(), @CreatedBy, GETUTCDATE()
            )
        END
        ELSE
        BEGIN
            -- Update existing user information
            UPDATE [eauth].[users]
            SET
                email = COALESCE(@Email, email),
                email_verified = CASE WHEN @EmailVerified = 1 THEN 1 ELSE email_verified END,
                display_name = COALESCE(@DisplayName, display_name),
                first_name = COALESCE(@FirstName, first_name),
                last_name = COALESCE(@LastName, last_name),
                profile_picture_url = COALESCE(@ProfilePictureUrl, profile_picture_url),
                modified_date = GETUTCDATE(),
                modified_by = @CreatedBy,
                last_login_date = GETUTCDATE()
            WHERE user_id = @UserId
        END

        -- Upsert user account for provider
        MERGE [eauth].[user_accounts] AS target
        USING (SELECT @UserId AS user_id, @Provider AS provider, @ProviderId AS provider_id) AS source
        ON target.user_id = source.user_id AND target.provider = source.provider
        WHEN MATCHED THEN
            UPDATE SET
                provider_email = @Email,
                provider_display_name = @DisplayName,
                provider_data = @ProviderData,
                last_used_date = GETUTCDATE(),
                is_active = 1
        WHEN NOT MATCHED THEN
            INSERT (user_id, provider, provider_id, provider_email, provider_display_name,
                    provider_data, is_primary, linked_date, last_used_date, created_by)
            VALUES (@UserId, @Provider, @ProviderId, @Email, @DisplayName,
                    @ProviderData, @IsNewUser, GETUTCDATE(), GETUTCDATE(), @CreatedBy);

        -- Set as primary provider if this is a new user
        IF @IsNewUser = 1
        BEGIN
            UPDATE [eauth].[user_accounts]
            SET is_primary = 1
            WHERE user_id = @UserId AND provider = @Provider AND provider_id = @ProviderId
        END

        COMMIT TRANSACTION

        -- Return user information
        SELECT
            u.user_id,
            u.email,
            u.display_name,
            u.first_name,
            u.last_name,
            u.profile_picture_url,
            u.primary_provider,
            u.last_login_date,
            @IsNewUser AS is_new_user
        FROM [eauth].[users] u
        WHERE u.user_id = @UserId

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION

        -- Log error to audit log
        INSERT INTO [eauth].[audit_log] (event_type, provider, event_data, result, error_message, created_date)
        VALUES ('USER_UPSERT_ERROR', @Provider,
                JSON_OBJECT('provider_id', @ProviderId, 'email', @Email),
                'ERROR', ERROR_MESSAGE(), GETUTCDATE())

        THROW
    END CATCH
END
