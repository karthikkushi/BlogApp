using Microsoft.AspNetCore.Components.Forms;
using Supabase;
using Supabase.Storage;
using System.Net.Http.Headers;

public class SupabaseImageUploader
{
    private readonly HttpClient _httpClient;
    private readonly string _supabaseUrl = "https://ajngmefbyzzsaqeobehj.supabase.co";
    private readonly string _bucket = "profile-pictures";
    private readonly string _serviceKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImFqbmdtZWZieXp6c2FxZW9iZWhqIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc0OTQ0MzI1MywiZXhwIjoyMDY1MDE5MjUzfQ.A-JCGxPl-es2d2I9xlB2zW59SsqpN9HDspKboddwVEc";



    public SupabaseImageUploader(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> UploadImageAsync(IBrowserFile file, string bucket = null)
    {
        var useBucket = bucket ?? _bucket;
        var fileName = $"{Guid.NewGuid()}_{file.Name}";
        var uploadUrl = $"{_supabaseUrl}/storage/v1/object/{useBucket}/{fileName}";

        using var fileStream = file.OpenReadStream(10_000_000);
        using var content = new StreamContent(fileStream);
        content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

        var request = new HttpRequestMessage(HttpMethod.Put, uploadUrl)
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceKey);
        request.Headers.Add("x-upsert", "true");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Upload failed: {response.StatusCode}\n{error}");
        }

        return $"{_supabaseUrl}/storage/v1/object/public/{useBucket}/{fileName}";
    }
}
