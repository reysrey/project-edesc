using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using KaizenBoard.Application.Features.Suggestions.Queries.GetSuggestions;
using KaizenBoard.Domain.Constants;
using KaizenBoard.Domain.Entities;
using KaizenBoard.Domain.Enums;
using KaizenBoard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KaizenBoard.Application.FunctionalTests;

public class SecurityTests : TestBase
{
    private const string FixturePassword = "FunctionalTests1234!";
    private const string Suggestions = "/api/Suggestions";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Test]
    public async Task Detail_list_and_search_enforce_the_same_visibility()
    {
        var ownDepartment = await CreateDepartment("Internal");
        var otherDepartment = await CreateDepartment("Other");
        var owner = await TestApp.RunAsUserAsync("owner@example.test", FixturePassword, [Roles.User]);
        var draft = await CreateIdea(ownDepartment.Id, SuggestionStatus.Draft);
        var inScope = await CreateIdea(ownDepartment.Id, SuggestionStatus.Submitted);
        var elsewhere = await CreateIdea(otherDepartment.Id, SuggestionStatus.Submitted);
        var rejected = await CreateIdea(ownDepartment.Id, SuggestionStatus.Rejected);

        await TestApp.RunAsUserAsync("moderator@example.test", FixturePassword, [Roles.AdminL2], ownDepartment.Id);
        var ownCrossDepartment = await CreateIdea(otherDepartment.Id, SuggestionStatus.Submitted);
        using var moderator = await Login("moderator@example.test");

        var list = await moderator.GetFromJsonAsync<SuggestionDto[]>(Suggestions, JsonOptions);
        list!.Select(s => s.Id).ShouldBe(new[] { inScope.Id, rejected.Id, ownCrossDepartment.Id }, ignoreOrder: true);
        list!.Single(s => s.Id == ownCrossDepartment.Id).AllowedActions.ShouldNotContain("moderate");
        var search = await moderator.GetFromJsonAsync<SearchResult>(Suggestions + "/search?scope=moderation", JsonOptions);
        search!.Items.Select(s => s.Id).ShouldBe(new[] { inScope.Id });
        (await moderator.GetAsync($"{Suggestions}/{draft.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await moderator.GetAsync($"{Suggestions}/{elsewhere.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await moderator.GetAsync($"{Suggestions}/{inScope.Id}")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await moderator.PostAsJsonAsync($"{Suggestions}/{elsewhere.Id}/approve", new { regNumber = "M-1" }))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        await TestApp.RunAsUserAsync("employee@example.test", FixturePassword, [Roles.User]);
        using var employee = await Login("employee@example.test");
        (await employee.GetAsync($"{Suggestions}/{rejected.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await employee.GetAsync($"{Suggestions}/{elsewhere.Id}")).StatusCode.ShouldBe(HttpStatusCode.OK);

        await TestApp.RunAsUserAsync("leader@example.test", FixturePassword, [Roles.AdminL1]);
        using var leader = await Login("leader@example.test");
        (await leader.GetAsync($"{Suggestions}/{draft.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        using var author = await Login("owner@example.test");
        var details = await author.GetFromJsonAsync<SuggestionDetailsDto>($"{Suggestions}/{draft.Id}", JsonOptions);
        details!.CreatedBy.ShouldBe(owner);
    }

    [TestCase("https://untrusted.localhost")]
    [TestCase("null")]
    public async Task Foreign_and_opaque_origins_cannot_submit_a_cookie_authenticated_draft(string origin)
    {
        var department = await CreateDepartment("Submission");
        await TestApp.RunAsUserAsync("author@example.test", FixturePassword, [Roles.User]);
        var draft = await CreateIdea(department.Id, SuggestionStatus.Draft);
        using var client = await Login("author@example.test");
        var route = $"{Suggestions}/{draft.Id}/submit";
        using var blocked = new HttpRequestMessage(HttpMethod.Post, route);
        blocked.Headers.TryAddWithoutValidation("Origin", origin);
        blocked.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "same-site");
        (await client.SendAsync(blocked)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await TestApp.FindAsync<Suggestion>(draft.Id))!.Status.ShouldBe(SuggestionStatus.Draft);

        using var allowed = new HttpRequestMessage(HttpMethod.Post, route);
        allowed.Headers.TryAddWithoutValidation("Origin", "https://localhost");
        (await client.SendAsync(allowed)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task Create_and_update_enforce_content_limits_and_return_a_reusable_version()
    {
        var department = await CreateDepartment("Editing");
        await TestApp.RunAsUserAsync("editor@example.test", FixturePassword, [Roles.User]);
        using var client = await Login("editor@example.test");
        var bad = new
        {
            title = new string('x', 201), currentFlaws = new string('x', 10_001),
            proposedSolution = "Improve it", improvementObject = "Process", targetDepartmentId = department.Id
        };
        (await client.PostAsJsonAsync(Suggestions, bad)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await TestApp.CountAsync<Suggestion>()).ShouldBe(0);

        var draft = await CreateIdea(department.Id, SuggestionStatus.Draft);
        var route = $"{Suggestions}/{draft.Id}";
        var details = await client.GetFromJsonAsync<SuggestionDetailsDto>(route, JsonOptions);
        (await client.PutAsJsonAsync(route, new
        {
            bad.title, bad.currentFlaws, bad.proposedSolution, bad.improvementObject, bad.targetDepartmentId,
            details!.Version
        })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var update = await client.PutAsJsonAsync(route, new
        {
            title = "Valid edited title", currentFlaws = "Problem", bad.proposedSolution,
            bad.improvementObject, bad.targetDepartmentId, details!.Version
        });
        update.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await update.Content.ReadFromJsonAsync<VersionResult>();
        var saved = await client.GetFromJsonAsync<SuggestionDetailsDto>(route, JsonOptions);
        result!.Version.ShouldBe(saved!.Version);
        saved.Title.ShouldBe("Valid edited title");
    }

    [Test]
    public async Task Postgres_rejects_an_edit_based_on_state_before_submission()
    {
        var department = await CreateDepartment("Concurrency");
        await TestApp.RunAsUserAsync("writer@example.test", FixturePassword, [Roles.User]);
        var idea = await CreateIdea(department.Id, SuggestionStatus.Draft);
        using var firstScope = FunctionalTestSetup.ScopeFactory.CreateScope();
        using var staleScope = FunctionalTestSetup.ScopeFactory.CreateScope();
        var first = firstScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stale = staleScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var submitted = await first.Suggestions.SingleAsync(s => s.Id == idea.Id);
        var edit = await stale.Suggestions.SingleAsync(s => s.Id == idea.Id);

        submitted.Submit();
        await first.SaveChangesAsync();
        edit.Title = "Stale change";
        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => stale.SaveChangesAsync());
        var saved = await TestApp.FindAsync<Suggestion>(idea.Id);
        saved!.Status.ShouldBe(SuggestionStatus.Submitted);
        saved.Title.ShouldBe(idea.Title);
    }

    private static async Task<Department> CreateDepartment(string name)
    {
        var department = new Department { Name = name };
        await TestApp.AddAsync(department);
        return department;
    }

    private static async Task<Suggestion> CreateIdea(Guid departmentId, SuggestionStatus status)
    {
        var idea = new Suggestion
        {
            Title = "Original title", ImprovementObject = "Process", CurrentFlaws = "Problem",
            ProposedSolution = "Solution", TargetDepartmentId = departmentId, Status = status
        };
        await TestApp.AddAsync(idea);
        return idea;
    }

    private static async Task<HttpClient> Login(string email)
    {
        var client = FunctionalTestSetup.CreateClient();
        var response = await client.PostAsJsonAsync("/api/Users/login?useCookies=true", new { email, password = FixturePassword });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return client;
    }

    private sealed record SearchResult(SuggestionDto[] Items);
    private sealed record VersionResult(string Version);
}
