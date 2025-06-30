using System.Net;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

var random = new Random();

// Đọc cookie và user-agent vào bộ nhớ cache
var cookies = File.ReadAllLines("instagram_cookie.txt")
    .Select(c => c.Trim())
    .Where(c => !string.IsNullOrWhiteSpace(c))
    .ToList();

var userAgents = File.ReadAllLines("user_agents.txt")
    .Select(ua => ua.Trim())
    .Where(ua => !string.IsNullOrWhiteSpace(ua))
    .ToList();

if (cookies.Count == 0 || userAgents.Count == 0)
    throw new Exception("instagram_cookie.txt hoặc user_agents.txt không có nội dung hợp lệ!");

// API kiểm tra danh sách username
app.MapGet("/api/check", async (string username) =>
{
    if (string.IsNullOrWhiteSpace(username))
        return Results.BadRequest("Username không hợp lệ");

    string cookie = cookies[random.Next(cookies.Count)];
    string userAgent = userAgents[random.Next(userAgents.Count)];

    using var http = new HttpClient(new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
    });

    var request = new HttpRequestMessage(HttpMethod.Get, $"https://www.instagram.com/{username}/?locale=en_US");
    request.Headers.Add("User-Agent", userAgent);
    request.Headers.Add("Cookie", cookie);
    request.Headers.Add("Accept-Language", "en-US,en;q=0.9");

    try
    {
        var response = await http.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == HttpStatusCode.NotFound || html.Contains("Sorry, this page isn't available"))
            return Results.Ok(new { status = "Die" });

        if (html.Contains($"(&#064;{username})"))
            return Results.Ok(new { status = "Live" });

        return Results.Ok(new { status = "Unknown" });
    }
    catch
    {
        return Results.Ok(new { status = "Unknown" });
    }
});

app.Run();
