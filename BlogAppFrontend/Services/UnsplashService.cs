using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;

namespace BlogAppFrontend.Services
{
    public class UnsplashService
    {
        private readonly HttpClient _httpClient;
        private const string AccessKey = "P6Aky7I1dGmi-gGqXgM28Uxk6CC3k8aSKDUtAq_GWrI";

        public UnsplashService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetImageUrlAsync(string title, string category, IEnumerable<string> tags)
        {
            try
            {
                // Use only cleaned tags as search keywords
                var searchTags = new List<string>();

                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        var cleanTag = tag?.TrimStart('#').Trim();
                        if (!string.IsNullOrWhiteSpace(cleanTag))
                            searchTags.Add(cleanTag);
                    }
                }

                if (!searchTags.Any())
                    return null;

                var query = string.Join(" ", searchTags);

                var url = $"/api/unsplashproxy/search?query={Uri.EscapeDataString(query)}";

                Console.WriteLine($"[UnsplashService] Querying Unsplash with: {query}");

                var response = await _httpClient.GetFromJsonAsync<UnsplashSearchResponse>(url);

                if (response?.Results?.Any() == true)
                {
                    Console.WriteLine($"[UnsplashService] Image URL found: {response.Results.First().Urls.Regular}");
                    return response.Results.First().Urls.Regular;
                }

                Console.WriteLine("[UnsplashService] No image found.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UnsplashService] Error fetching image: {ex.Message}");
                return null;
            }
        }

        private class UnsplashSearchResponse
        {
            public List<Result> Results { get; set; }

            public class Result
            {
                public Urls Urls { get; set; }
            }

            public class Urls
            {
                public string Regular { get; set; }
            }
        }
    }
}
