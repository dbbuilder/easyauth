using EasyAuth.Framework.Core.Models;
using Microsoft.Extensions.Logging;

namespace EasyAuth.Framework.Core.Services
{
    /// <summary>
    /// Main authentication service implementation
    /// This is a minimal stub implementation following TDD approach:
    /// Tests are written first, then this stub allows compilation,
    /// then proper implementation follows in GREEN phase
    /// </summary>
    public class EAuthService : IEAuthService
    {
        private readonly IEAuthDatabaseService _databaseService;
        private readonly ILogger<EAuthService> _logger;

        public EAuthService(IEAuthDatabaseService databaseService, ILogger<EAuthService> logger)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<EAuthResponse<IEnumerable<ProviderInfo>>> GetProvidersAsync()
        {
            await Task.CompletedTask;
            throw new NotImplementedException("TDD RED Phase - Test first, implement later");
        }

        public async Task<EAuthResponse<string>> InitiateLoginAsync(LoginRequest request)
        {
            await Task.CompletedTask;
            throw new NotImplementedException("TDD RED Phase - Test first, implement later");
        }

        public async Task<EAuthResponse<UserInfo>> HandleAuthCallbackAsync(string provider, string code, string? state = null)
        {
            await Task.CompletedTask;
            throw new NotImplementedException("TDD RED Phase - Test first, implement later");
        }

        public async Task<EAuthResponse<bool>> SignOutAsync(string? sessionId = null)
        {
            await Task.CompletedTask;
            throw new NotImplementedException("TDD RED Phase - Test first, implement later");
        }

        public async Task<EAuthResponse<UserInfo>> GetCurrentUserAsync()
        {
            await Task.CompletedTask;
            throw new NotImplementedException("TDD RED Phase - Test first, implement later");
        }

        public async Task<EAuthResponse<SessionInfo>> ValidateSessionAsync(string sessionId)
        {
            await Task.CompletedTask;
            throw new NotImplementedException("TDD RED Phase - Test first, implement later");
        }

        public async Task<EAuthResponse<UserInfo>> LinkAccountAsync(string provider, string code, string state)
        {
            await Task.CompletedTask;
            throw new NotImplementedException("TDD RED Phase - Test first, implement later");
        }

        public async Task<EAuthResponse<bool>> UnlinkAccountAsync(string provider)
        {
            await Task.CompletedTask;
            throw new NotImplementedException("TDD RED Phase - Test first, implement later");
        }

        public async Task<EAuthResponse<string>> InitiatePasswordResetAsync(PasswordResetRequest request)
        {
            await Task.CompletedTask;
            throw new NotImplementedException("TDD RED Phase - Test first, implement later");
        }
    }
}