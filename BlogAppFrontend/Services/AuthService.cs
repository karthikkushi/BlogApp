using BlogAppFrontend.Models;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Text.Json;
using System;

namespace BlogAppFrontend.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly JsonSerializerOptions _jsonOptions;

        public AuthService(
            HttpClient httpClient,
            IJSRuntime jsRuntime,
            AuthenticationStateProvider authStateProvider,
            JsonSerializerOptions jsonOptions)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
            _authStateProvider = authStateProvider;
            _jsonOptions = jsonOptions;
        }

        public async Task<AuthResponse?> SignUpAsync(string email, string password, string username)
        {
            try
            {
                var request = new AuthRequest
                {
                    Email = email,
                    Password = password,
                    Username = username
                };

                var response = await _httpClient.PostAsJsonAsync("Auth/signup", request);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Sign-up failed: {response.StatusCode}");
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
                if (result == null || string.IsNullOrEmpty(result.Token))
                {
                    Console.WriteLine("Sign-up response invalid: Token is missing.");
                    return null;
                }

                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", result.Token);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "userId", result.UserId);
                await ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(result.Token);
                Console.WriteLine("Sign-up successful, token and userId stored.");

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sign-up error: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> SignInAsync(string email, string password)
        {
            try
            {
                var request = new SignInRequest
                {
                    Email = email,
                    Password = password
                };

                var response = await _httpClient.PostAsJsonAsync("Auth/signin", request);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Sign-in failed: {response.StatusCode}");
                    return false;
                }

                var result = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
                if (result == null || string.IsNullOrEmpty(result.Token))
                {
                    Console.WriteLine("Sign-in response invalid: Token is missing.");
                    return false;
                }

                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", result.Token);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "userId", result.UserId);
                await ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(result.Token);
                Console.WriteLine("Sign-in successful, token and userId stored.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sign-in error: {ex.Message}");
                return false;
            }
        }

        public async Task SignOutAsync()
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "userId");
            await ((CustomAuthStateProvider)_authStateProvider).NotifyUserLogout();
            Console.WriteLine("Signed out successfully.");
        }

        public async Task<string?> GetTokenAsync()
        {
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
            Console.WriteLine($"Retrieved token: {token}");
            return token;
        }

        public async Task<string?> GetUserIdAsync()
        {
            var userId = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "userId");
            Console.WriteLine($"Retrieved userId: {userId}");
            return userId;
        }

        public event Action? OnProfileUpdated;

        public void NotifyProfileUpdated()
        {
            OnProfileUpdated?.Invoke();
        }
    }

    public class AuthResponse
    {
        public string? Token { get; set; }
        public string? UserId { get; set; }
    }

    public class AuthRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Username { get; set; }
    }

    public class SignInRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
