using System.Threading.Tasks;

namespace BlogAppFrontend.Services
{
    public interface IAuthService
    {
        Task<string> GetUserIdAsync();
        Task<bool> IsAuthenticatedAsync();
        Task SignOutAsync();
    }
} 