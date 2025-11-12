namespace BlogApp.Api.Models
{
    public class SignUpRequest
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
