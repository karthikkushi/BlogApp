using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;

namespace BlogAppBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UnsplashProxyController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private const string AccessKey = "P6Aky7I1dGmi-gGqXgM28Uxk6CC3k8aSKDUtAq_GWrI";

        public UnsplashProxyController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            var url = $"https://api.unsplash.com/search/photos?query={Uri.EscapeDataString(query)}&client_id={AccessKey}&orientation=landscape&per_page=1";

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, content);

            return Content(content, "application/json");
        }
    }
}
