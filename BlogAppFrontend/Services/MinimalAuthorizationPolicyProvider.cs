using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace BlogAppFrontend.Services
{
    public class MinimalAuthorizationPolicyProvider : IAuthorizationPolicyProvider
    {
        private static readonly AuthorizationPolicy _defaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        {
            return Task.FromResult(_defaultPolicy);
        }

        public Task<AuthorizationPolicy> GetFallbackPolicyAsync()
        {
            return Task.FromResult<AuthorizationPolicy>(null);
        }

        public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
        {
            // If you later need custom policies, you can implement them here.
            return Task.FromResult<AuthorizationPolicy>(null);
        }
    }
}