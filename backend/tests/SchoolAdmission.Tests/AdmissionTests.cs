using System.Net.Http.Json;
using System.Net;
using Xunit;

namespace SchoolAdmission.Tests;

[Collection("db")]
public class AdmissionTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public AdmissionTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Concurrent_holds_never_exceed_total_seats()
    {
        const int seats = 10, requests = 100;
        var cls = await _client.CreateClass(seats);

        var responses = await Task.WhenAll(Enumerable.Range(0, requests)
            .Select(i => Task.Run(() => _client.Hold(cls.Id, Api.Student(i)))));

        var created = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var conflicts = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(seats, created);
        Assert.Equal(requests - seats, conflicts);

        var stats = await _client.GetClass(cls.Id);
        Assert.Equal(seats, stats.HeldSeats);
        Assert.Equal(0, stats.ConfirmedSeats);
        Assert.Equal(0, stats.AvailableSeats);
        Assert.True(stats.HeldSeats + stats.ConfirmedSeats <= stats.TotalSeats);
    }

    [Fact]
    public async Task Concurrent_mix_of_confirm_cancel_and_new_holds_respects_limit()
    {
        const int seats = 10;
        var cls = await _client.CreateClass(seats);

        var initial = await Task.WhenAll(Enumerable.Range(0, seats)
            .Select(i => _client.Hold(cls.Id, Api.Student(i))));
        var holds = await Task.WhenAll(initial.Select(r => r.ReadHold()));

        // Confirm 5 and cancel 5 while 30 other students race for the 5 freed seats.
        var confirms = holds.Take(5).Select(h => Task.Run(() => _client.Send(HttpMethod.Post, $"/holds/{h.Id}/confirm", h.StudentId)));
        var cancels = holds.Skip(5).Select(h => Task.Run(() => _client.Send(HttpMethod.Delete, $"/holds/{h.Id}", h.StudentId)));
        var newcomers = Enumerable.Range(100, 30).Select(i => Task.Run(() => _client.Hold(cls.Id, Api.Student(i))));

        await Task.WhenAll(confirms.Concat(cancels));
        var newResults = await Task.WhenAll(newcomers);

        var stats = await _client.GetClass(cls.Id);
        Assert.Equal(5, stats.ConfirmedSeats);
        Assert.True(stats.HeldSeats + stats.ConfirmedSeats <= seats);
        // Newcomers may have been admitted only into seats that were genuinely free at the time.
        Assert.True(newResults.Count(r => r.StatusCode == HttpStatusCode.Created) <= 5);
    }

    [Fact]
    public async Task Same_student_cannot_hold_the_same_class_twice_even_in_parallel()
    {
        var cls = await _client.CreateClass(20);
        var student = Api.Student(777);

        var responses = await Task.WhenAll(Enumerable.Range(0, 15)
            .Select(_ => Task.Run(() => _client.Hold(cls.Id, student))));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(14, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        Assert.Equal(1, (await _client.GetClass(cls.Id)).HeldSeats);
    }

    [Fact]
    public async Task Hold_then_confirm_marks_seat_confirmed_and_is_idempotent()
    {
        var cls = await _client.CreateClass(3);
        var student = Api.Student(1);

        var hold = await (await _client.Hold(cls.Id, student)).ReadHold();
        Assert.Equal("HELD", hold.Status);

        var confirm = await _client.Send(HttpMethod.Post, $"/holds/{hold.Id}/confirm", student);
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        Assert.Equal("CONFIRMED", (await confirm.ReadHold()).Status);

        var again = await _client.Send(HttpMethod.Post, $"/holds/{hold.Id}/confirm", student);
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);

        var stats = await _client.GetClass(cls.Id);
        Assert.Equal(0, stats.HeldSeats);
        Assert.Equal(1, stats.ConfirmedSeats);
        Assert.Equal(2, stats.AvailableSeats);

        // A confirmed application can no longer be cancelled.
        var cancel = await _client.Send(HttpMethod.Delete, $"/holds/{hold.Id}", student);
        Assert.Equal(HttpStatusCode.Conflict, cancel.StatusCode);
    }

    [Fact]
    public async Task Cancel_frees_the_seat_for_someone_else()
    {
        var cls = await _client.CreateClass(1);

        var first = await (await _client.Hold(cls.Id, Api.Student(1))).ReadHold();
        var blocked = await _client.Hold(cls.Id, Api.Student(2));
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);

        var cancel = await _client.Send(HttpMethod.Delete, $"/holds/{first.Id}", Api.Student(1));
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.Equal("CANCELLED", (await cancel.ReadHold()).Status);

        var second = await _client.Hold(cls.Id, Api.Student(2));
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }

    [Fact]
    public async Task Validation_and_listing_endpoints_behave()
    {
        var cls = await _client.CreateClass(2);

        Assert.Equal(HttpStatusCode.BadRequest, (await _client.Hold(cls.Id, null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.Hold(cls.Id, "123")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.Hold(Guid.NewGuid(), Api.Student(1))).StatusCode);

        var bad = await _client.PostAsJsonAsync("/classes", new { name = "x", totalSeats = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

        var student = Api.Student(424242);
        await _client.Hold(cls.Id, student);
        var mine = await _client.Send(HttpMethod.Get, "/me/holds", student);
        var list = await mine.Content.ReadFromJsonAsync<List<HoldView>>();
        Assert.Single(list!, h => h.ClassId == cls.Id);
    }
}

[Collection("db")]
public class ExpiryTests : IClassFixture<ExpiryTests.ShortHoldFactory>
{
    public class ShortHoldFactory : ApiFactory
    {
        public ShortHoldFactory() : base(holdSeconds: 1) { }
    }

    private readonly HttpClient _client;

    public ExpiryTests(ShortHoldFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Expired_hold_reopens_the_seat_and_cannot_be_confirmed()
    {
        var cls = await _client.CreateClass(1);

        var hold = await (await _client.Hold(cls.Id, Api.Student(1))).ReadHold();
        Assert.Equal(HttpStatusCode.Conflict, (await _client.Hold(cls.Id, Api.Student(2))).StatusCode);

        await Task.Delay(TimeSpan.FromSeconds(2.5));

        var status = await (await _client.Send(HttpMethod.Get, $"/holds/{hold.Id}", null)).ReadHold();
        Assert.Equal("EXPIRED", status.Status);
        Assert.Equal(1, (await _client.GetClass(cls.Id)).AvailableSeats);

        var late = await _client.Send(HttpMethod.Post, $"/holds/{hold.Id}/confirm", Api.Student(1));
        Assert.Equal(HttpStatusCode.Conflict, late.StatusCode);

        var next = await _client.Hold(cls.Id, Api.Student(2));
        Assert.Equal(HttpStatusCode.Created, next.StatusCode);
    }
}