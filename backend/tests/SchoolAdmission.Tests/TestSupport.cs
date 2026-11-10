using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Tests;

/// <summary>
/// Boots the real API in-process against a real PostgreSQL database.
/// (The concurrency guarantee relies on PostgreSQL row locks, so an in-memory fake would prove nothing.)
/// Set TEST_CONNECTION_STRING, or start the compose db service and use the default below.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private const string DefaultConnection =
        "Host=localhost;Port=5433;Database=school_admission_test;Username=postgres;Password=postgres;Maximum Pool Size=50";

    private readonly int? _holdSeconds;

    static ApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Default",
            Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING") ?? DefaultConnection);
    }

    public ApiFactory(int? holdSeconds = null) => _holdSeconds = holdSeconds;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            if (_holdSeconds is int seconds)
            {
                var existing = services.Single(d => d.ServiceType == typeof(AdmissionOptions));
                services.Remove(existing);
                services.AddSingleton(new AdmissionOptions(seconds));
            }
        });
    }
}

public sealed record ClassView(Guid Id, string Name, int TotalSeats, int HeldSeats, int ConfirmedSeats, int AvailableSeats);
public sealed record HoldView(Guid Id, Guid ClassId, string ClassName, string StudentId, string Status, int RemainingSeconds);

public static class Api
{
    public static string Student(int n) => (10_000_000_000_000_000L + n).ToString();

    public static async Task<ClassView> CreateClass(this HttpClient c, int seats)
    {
        var res = await c.PostAsJsonAsync("/classes", new { name = $"Class-{Guid.NewGuid():N}", totalSeats = seats });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<ClassView>())!;
    }

    public static async Task<ClassView> GetClass(this HttpClient c, Guid id) =>
        (await c.GetFromJsonAsync<ClassView>($"/classes/{id}"))!;

    public static Task<HttpResponseMessage> Send(this HttpClient c, HttpMethod method, string url, string? student)
    {
        var req = new HttpRequestMessage(method, url);
        if (student is not null) req.Headers.Add("X-Student-Id", student);
        return c.SendAsync(req);
    }

    public static Task<HttpResponseMessage> Hold(this HttpClient c, Guid classId, string? student) =>
        c.Send(HttpMethod.Post, $"/classes/{classId}/holds", student);

    public static async Task<HoldView> ReadHold(this HttpResponseMessage res) =>
        (await res.Content.ReadFromJsonAsync<HoldView>())!;
}

// All test classes share one collection so they run sequentially against the shared database.
[CollectionDefinition("db", DisableParallelization = true)]
public class DbCollection { }