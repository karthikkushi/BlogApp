using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Supabase;

namespace BlogApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requires JWT authentication
    public class ImageController : ControllerBase
    {
        private readonly Client _supabaseClient;
        private readonly IConfiguration _configuration;

        public ImageController(Client supabaseClient, IConfiguration configuration)
        {
            _supabaseClient = supabaseClient;
            _configuration = configuration;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file, [FromQuery] string bucket)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            if (file.Length > 10_000_000)
                return BadRequest("File size exceeds 10 MB.");

            var user = _supabaseClient.Auth.CurrentUser;
            if (user == null)
                return Unauthorized();

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var supabaseUrl = _configuration["Supabase:Url"];
            var validBuckets = new[] { "profile-pictures", "cover-photos" };
            if (!validBuckets.Contains(bucket))
                return BadRequest("Invalid bucket specified.");

            try
            {
                // Convert Stream to byte[]
                using var stream = file.OpenReadStream();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                var result = await _supabaseClient.Storage
                    .From(bucket)
                    .Upload(fileBytes, fileName, new Supabase.Storage.FileOptions
                    {
                        ContentType = file.ContentType,
                        Upsert = true
                    });

                var publicUrl = $"{supabaseUrl}/storage/v1/object/public/{bucket}/{fileName}";
                return Ok(new { Url = publicUrl });
            }
            catch (Exception ex)
            {
                return BadRequest($"Upload failed: {ex.Message}");
            }
        }
    }
}