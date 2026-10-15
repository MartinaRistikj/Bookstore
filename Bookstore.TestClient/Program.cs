
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .Build();

var apiUrl = configuration["BookstoreApi:BaseUrl"]!;
var tokenUrl = configuration["Keycloak:TokenUrl"]!;
var clientId = configuration["Keycloak:ClientId"]!;
var scope = configuration["Keycloak:Scope"]!;

var clientSecret =
    Environment.GetEnvironmentVariable("BOOKSTORE_CLIENT_SECRET");

if (string.IsNullOrWhiteSpace(clientSecret))
{
    Console.Write("Enter demo CRUD client secret: ");
    clientSecret = Console.ReadLine();
}

if (string.IsNullOrWhiteSpace(clientSecret))
{
    Console.WriteLine("Client secret is required.");
    return;
}

using var httpClient = new HttpClient();

try
{
    Console.WriteLine("Authenticating with Keycloak...");

    using var tokenRequest = new HttpRequestMessage(
        HttpMethod.Post,
        tokenUrl);

    tokenRequest.Content = new FormUrlEncodedContent(
        new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["scope"] = scope
        });

    using var tokenResponse = await httpClient.SendAsync(
        tokenRequest);

    tokenResponse.EnsureSuccessStatusCode();

    using var tokenJson = await JsonDocument.ParseAsync(
        await tokenResponse.Content.ReadAsStreamAsync());

    var accessToken = tokenJson.RootElement
        .GetProperty("access_token")
        .GetString();

    if (string.IsNullOrWhiteSpace(accessToken))
    {
        Console.WriteLine("No access token returned.");
        return;
    }

    Console.WriteLine("Authentication successful!");
    Console.WriteLine();

    httpClient.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", accessToken);

    Console.WriteLine("GET /api/books/1");

    using var response = await httpClient.GetAsync(
        $"{apiUrl}/api/books/1");

    Console.WriteLine(
        $"Status: {(int)response.StatusCode} {response.ReasonPhrase}");

    var responseBody =
        await response.Content.ReadAsStringAsync();

    if (response.IsSuccessStatusCode)
    {
        using var bookJson = JsonDocument.Parse(responseBody);

        Console.WriteLine(
            $"Title: {bookJson.RootElement.GetProperty("title").GetString()}");

        Console.WriteLine(
           $"Author: {bookJson.RootElement
               .GetProperty("author")
               .GetProperty("name")
               .GetString()}");
    }
    else
    {
        Console.WriteLine(responseBody);
    }
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"HTTP request failed: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
