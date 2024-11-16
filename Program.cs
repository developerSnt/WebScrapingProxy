using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Net; 
using System.Net.Http;
var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "Hello World!");
app.MapGet("/scrape", async (string urlList) =>
{
    // Hardcoded list of proxies
    string[] proxies = new[]
    {
        "https://101.255.94.161:8080", 
        "http://130.162.148.105:8080"
    };

    if (proxies.Length == 0)
    {
        return Results.BadRequest(new { Error = "No proxies available." });
    }

    // Split the input string into an array of URLs
    var urls = urlList.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                      .Select(u => u.Trim())
                      .ToArray();

    var results = new List<object>();

    foreach (var url in urls)
    {
        foreach (var selectedProxy in proxies)
        {
            var proxy = new WebProxy(selectedProxy);

            var handler = new HttpClientHandler
            {
                Proxy = proxy,
                UseProxy = true
            };

            using var clientWithProxy = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(150)
            };

            clientWithProxy.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");

            try
            {
                var response = await clientWithProxy.GetStringAsync(url);
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(response);

                var titleNode = htmlDocument.DocumentNode.SelectSingleNode("//title");
                var title = titleNode?.InnerText ?? "No title found";

                var descriptionNode = htmlDocument.DocumentNode.SelectSingleNode("//meta[@name='description']");
                var description = descriptionNode?.GetAttributeValue("content", "No description found") ?? "No description found";

                results.Add(new 
                { 
                    Title = title,
                    Description = description,
                    // Url = url,
                    // ProxyUrl = selectedProxy 
                });

                break; // Break out of the proxy loop if successful
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Request failed using proxy {selectedProxy} for URL {url}: {ex.Message}");
                continue;
            }
        }
    }

    if (results.Count == 0)
    {
        return Results.BadRequest(new { Error = "All proxies failed for all URLs." });
    }

    return Results.Ok(results);
});

app.Run();
