using Supabase;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using BlogApp.Api.Models;

namespace BlogApp.Api.Services
{
    public class AuthService
    {
        private readonly Client _supabase;
        private readonly MongoDbService _mongoDbService;

        public AuthService(IConfiguration configuration, MongoDbService mongoDbService)
        {
            var supabaseUrl = configuration["Supabase:Url"];
            var supabaseKey = configuration["Supabase:AnonKey"];
            _supabase = new Client(supabaseUrl, supabaseKey);
            _mongoDbService = mongoDbService;
        }

        public async Task<Supabase.Gotrue.Session> SignUpAsync(string email, string password, string username)
        {
            await _supabase.InitializeAsync();

            var session = await _supabase.Auth.SignUp(email, password);

            if (session?.User != null)
            {
                var userId = session.User.Id;

                var newProfile = new UserProfile
                {
                    Id = userId,
                    Username = username,
                    Bio = "",
                    ProfilePictureUrl = "",
                    CoverPhotoUrl = "",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _mongoDbService.CreateUserProfileAsync(newProfile);
            }

            return session;
        }

        public async Task<Supabase.Gotrue.Session> SignInAsync(string email, string password)
        {
            await _supabase.InitializeAsync();
            return await _supabase.Auth.SignIn(email, password);
        }
    }
}
