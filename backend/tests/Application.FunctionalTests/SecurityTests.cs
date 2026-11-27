using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using KaizenBoard.Application.Common.Models;
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

    [Test]
    public async Task Create_replays_the_original_id_for_parallel_requests_and_rejects_changed_content()
    {
        var department = await CreateDepartment("Idempotency");
        await TestApp.RunAsUserAsync("author@example.test", FixturePassword, [Roles.User]);
        await TestApp.RunAsUserAsync("other@example.test", FixturePassword, [Roles.User]);
        using var author = await Login("author@example.test");
        using var other = await Login("other@example.test");
        var input = new SuggestionInput(
            "Original title", "Problem", "Solution", "Process", department.Id);
        const string key = "attempt-1";

        var responses = await Task.WhenAll(Create(author, input, key), Create(author, input, key));
        var ids = new List<Guid>();
        foreach (var response in responses)
        {
            using (response)
            {
                response.StatusCode.ShouldBe(HttpStatusCode.Created);
                var id = await response.Content.ReadFromJsonAsync<Guid>();
                response.Headers.Location!.OriginalString.ShouldBe($"/api/suggestions/{id}");
                ids.Add(id);
            }
        }
        ids[1].ShouldBe(ids[0]);
        (await TestApp.CountAsync<Suggestion>()).ShouldBe(1);

        (await Create(author, input with { Title = "" }, key)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await Create(author, input, "invalid key")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var duplicateHeader = new HttpRequestMessage(HttpMethod.Post, Suggestions + "/")
        {
            Content = JsonContent.Create(input)
        };
        duplicateHeader.Headers.Add("Idempotency-Key", new[] { "key-1", "key-2" });
        (await author.SendAsync(duplicateHeader)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var otherResponse = await Create(other, input, key);
        otherResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await otherResponse.Content.ReadFromJsonAsync<Guid>()).ShouldNotBe(ids[0]);
        (await TestApp.CountAsync<Suggestion>()).ShouldBe(2);
    }

    [Test]
    public async Task Archived_template_can_be_kept_but_cannot_be_selected_for_a_new_binding()
    {
        var department = await CreateDepartment("Templates");
        var template = new Template { Title = "Original template", IsActive = true };
        await TestApp.AddAsync(template);
        await TestApp.RunAsUserAsync("author@example.test", FixturePassword, [Roles.User]);
        var unbound = await CreateIdea(department.Id, SuggestionStatus.Draft);
        await TestApp.RunAsUserAsync("leader@example.test", FixturePassword, [Roles.AdminL1]);
        using var author = await Login("author@example.test");
        using var leader = await Login("leader@example.test");
        var input = new SuggestionInput(
            "Original title", "Problem", "Solution", "Process", department.Id, template.Id);
        var created = await Create(author, input, "archived-replay");
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = await created.Content.ReadFromJsonAsync<Guid>();
        var route = $"{Suggestions}/{id}";
        (await leader.PatchAsync($"/api/Templates/{template.Id}/archive", null))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var card = await ReadCard(author, id);
        card.TemplateId.ShouldBe(template.Id);
        card.TemplateName.ShouldBe(template.Title);
        card.TemplateIsActive.ShouldBe(false);
        (await Create(author, input)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var unboundCard = await ReadCard(author, unbound.Id);
        (await author.PutAsJsonAsync($"{Suggestions}/{unbound.Id}", new
        {
            input.Title, input.CurrentFlaws, input.ProposedSolution, input.ImprovementObject,
            input.TargetDepartmentId, input.TemplateId, unboundCard.Version
        })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        (await author.PutAsJsonAsync(route, new
        {
            Title = "Edited title", input.CurrentFlaws, input.ProposedSolution, input.ImprovementObject,
            input.TargetDepartmentId, input.TemplateId, card.Version
        })).StatusCode.ShouldBe(HttpStatusCode.OK);
        var replay = await Create(author, input, "archived-replay");
        replay.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await replay.Content.ReadFromJsonAsync<Guid>()).ShouldBe(id);
        (await author.PostAsync(route + "/submit", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ReadCard(author, id)).TemplateId.ShouldBe(template.Id);
    }

    [Test]
    public async Task Mine_edit_and_resubmit_follow_the_card_contract()
    {
        var department = await CreateDepartment("Workflow");
        await TestApp.RunAsUserAsync("author@example.test", FixturePassword, [Roles.User]);
        var draft = await CreateIdea(department.Id, SuggestionStatus.Draft);
        var revision = await CreateIdea(department.Id, SuggestionStatus.RevisionRequired);
        await TestApp.RunAsUserAsync("moderator@example.test", FixturePassword, [Roles.AdminL2], department.Id);
        await TestApp.RunAsUserAsync("observer@example.test", FixturePassword, [Roles.User]);
        await TestApp.RunAsUserAsync("leader@example.test", FixturePassword, [Roles.AdminL1]);
        using var author = await Login("author@example.test");
        using var moderator = await Login("moderator@example.test");
        using var observer = await Login("observer@example.test");
        using var leader = await Login("leader@example.test");
        var route = $"{Suggestions}/{draft.Id}";

        var mine = await author.GetFromJsonAsync<PagedResult<SuggestionDto>>(Suggestions + "/search?scope=mine", JsonOptions);
        mine!.TotalCount.ShouldBe(2);
        mine.Items.Single(s => s.Id == draft.Id).AuthorName.ShouldBe("author@example.test");
        mine.Items.Single(s => s.Id == draft.Id).AllowedActions.ShouldBe(new[] { "edit", "submit" });
        mine.Items.Single(s => s.Id == revision.Id).AllowedActions.ShouldBe(new[] { "edit", "submit" });
        (await ReadCard(author, revision.Id)).AllowedActions.ShouldBe(new[] { "edit", "submit" });
        (await author.PostAsync($"{Suggestions}/{revision.Id}/submit", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await observer.GetAsync(route)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await observer.PostAsync(route + "/submit", null)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var observerMine = await observer.GetFromJsonAsync<PagedResult<SuggestionDto>>(Suggestions + "/search?scope=mine", JsonOptions);
        observerMine!.TotalCount.ShouldBe(0);

        var card = await ReadCard(author, draft.Id);
        card.IsMine.ShouldBeTrue();
        card.DepartmentName.ShouldBe(department.Name);
        Task<HttpResponseMessage> Edit(string title) => author.PutAsJsonAsync(route, new
        {
            title, card.CurrentFlaws, card.ProposedSolution, card.ImprovementObject, card.TargetDepartmentId, card.Version
        });

        (await observer.PutAsJsonAsync(route, new
        {
            card.Title, card.CurrentFlaws, card.ProposedSolution, card.ImprovementObject, card.TargetDepartmentId, card.Version
        })).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var edits = await Task.WhenAll(Edit("First edit"), Edit("Second edit"));
        edits.Select(result => result.StatusCode).ShouldBe(new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }, ignoreOrder: true);
        (await ReadCard(author, draft.Id)).Status.ShouldBe(SuggestionStatus.Draft);

        (await author.PostAsync(route + "/submit", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await author.PostAsync(route + "/submit", null)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ReadCard(moderator, draft.Id)).AllowedActions.ShouldContain("moderate");
        (await moderator.PostAsJsonAsync(route + "/return", new { reason = "Clarify the solution" }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var returned = await ReadCard(author, draft.Id);
        returned.Status.ShouldBe(SuggestionStatus.ReturnedByManager);
        returned.AllowedActions.ShouldBe(new[] { "edit", "submit" });
        returned.SubmissionCount.ShouldBe(card.SubmissionCount + 1);
        returned.CommissionNote.ShouldBe("Clarify the solution");
        (await ReadCard(moderator, draft.Id)).CommissionNote.ShouldBe(returned.CommissionNote);
        (await ReadCard(leader, draft.Id)).CommissionNote.ShouldBe(returned.CommissionNote);
        (await ReadCard(observer, draft.Id)).CommissionNote.ShouldBeNull();

        (await author.PutAsJsonAsync(route, new
        {
            returned.Title, returned.CurrentFlaws, ProposedSolution = "Clarified solution",
            returned.ImprovementObject, returned.TargetDepartmentId, returned.Version
        })).StatusCode.ShouldBe(HttpStatusCode.OK);
        var revised = await ReadCard(author, draft.Id);
        revised.Status.ShouldBe(SuggestionStatus.ReturnedByManager);
        revised.SubmissionCount.ShouldBe(returned.SubmissionCount);

        (await author.PostAsync(route + "/submit", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await moderator.PostAsJsonAsync(route + "/return", new { reason = "Clarify the object" }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var returnedAgain = await ReadCard(author, draft.Id);
        returnedAgain.Status.ShouldBe(returned.Status);
        returnedAgain.SubmissionCount.ShouldBe(returned.SubmissionCount + 1);

        (await author.PostAsync(route + "/submit", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await moderator.PostAsJsonAsync(route + "/approve", new { regNumber = "MVP-01-1" }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var inCommission = await ReadCard(leader, draft.Id);
        inCommission.RegistrationNumber.ShouldBe("MVP-01-1");
        inCommission.AllowedActions.ShouldContain("commission");
    }

    [Test]
    public async Task Commission_actions_obey_department_version_and_history()
    {
        var department = await CreateDepartment("Commission");
        var outside = await CreateDepartment("Outside");
        await TestApp.RunAsUserAsync("author@example.test", FixturePassword, [Roles.User]);
        var idea = await CreateIdea(department.Id, SuggestionStatus.Draft);
        var second = await CreateIdea(department.Id, SuggestionStatus.InCommission);
        await TestApp.RunAsUserAsync("moderator@example.test", FixturePassword, [Roles.AdminL2], department.Id);
        await TestApp.RunAsUserAsync("outside@example.test", FixturePassword, [Roles.AdminL2], outside.Id);
        await TestApp.RunAsUserAsync("leader@example.test", FixturePassword, [Roles.AdminL1]);
        using var author = await Login("author@example.test");
        using var moderator = await Login("moderator@example.test");
        using var outsider = await Login("outside@example.test");
        using var leader = await Login("leader@example.test");
        var route = $"{Suggestions}/{idea.Id}";

        (await author.PostAsync(route + "/submit", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await moderator.PostAsJsonAsync(route + "/approve", new { regNumber = "COM-1" }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var card = await ReadCard(moderator, idea.Id);
        card.AllowedActions.ShouldContain("commission");
        var decision = new { card.Version, note = "Уточните результат" };
        (await author.PostAsJsonAsync(route + "/commission/return", decision)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await outsider.PostAsJsonAsync(route + "/commission/return", decision)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await moderator.PostAsJsonAsync(route + "/commission/return", decision)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await leader.PostAsJsonAsync(route + "/commission/reject", decision)).StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var returned = await ReadCard(author, idea.Id);
        returned.Status.ShouldBe(SuggestionStatus.RevisionRequired);
        returned.History.Select(h => h.ToStatus).ShouldBe(new[]
        {
            SuggestionStatus.RevisionRequired, SuggestionStatus.InCommission, SuggestionStatus.Submitted
        });
        returned.History[0].Note.ShouldBe(decision.note);
        var notices = await author.GetFromJsonAsync<TestNotice[]>("/api/Users/notifications", JsonOptions);
        notices!.Length.ShouldBe(2);
        (await author.PostAsync(route + "/submit", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await moderator.PostAsJsonAsync(route + "/approve", new { regNumber = "COM-2" }))
            .StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await moderator.PostAsJsonAsync(route + "/approve", new { regNumber = "COM-1" }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var latest = await ReadCard(leader, idea.Id);
        (await leader.PostAsJsonAsync(route + "/commission/approve", new { latest.Version }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ReadCard(author, idea.Id)).Status.ShouldBe(SuggestionStatus.ApprovedPlanning);

        var secondCard = await ReadCard(moderator, second.Id);
        var rejection = $"{Suggestions}/{second.Id}/commission/reject";
        (await moderator.PostAsJsonAsync(rejection, new { secondCard.Version, note = "" }))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await moderator.PostAsJsonAsync(rejection, new { secondCard.Version, note = "Уже внедрено" }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ReadCard(author, second.Id)).Status.ShouldBe(SuggestionStatus.Rejected);
    }

    [Test]
    public async Task Service_settings_control_support_anonymity_board_and_notifications()
    {
        var department = await CreateDepartment("Settings");
        await TestApp.RunAsUserAsync("author@example.test", FixturePassword, [Roles.User]);
        var idea = await CreateIdea(department.Id, SuggestionStatus.Submitted);
        await TestApp.RunAsUserAsync("reader@example.test", FixturePassword, [Roles.User]);
        await TestApp.RunAsUserAsync("leader@example.test", FixturePassword, [Roles.AdminL1]);
        using var author = await Login("author@example.test");
        using var reader = await Login("reader@example.test");
        using var leader = await Login("leader@example.test");
        var route = $"{Suggestions}/{idea.Id}";
        const string settingsPath = "/api/ServiceSettings/";
        var original = (await leader.GetFromJsonAsync<TestSettings>(settingsPath))!;

        var responses = await Task.WhenAll(reader.PutAsync(route + "/support", null), reader.PutAsync(route + "/support", null));
        responses.Select(r => r.StatusCode).ShouldAllBe(status => status == HttpStatusCode.NoContent);
        var supported = await ReadCard(reader, idea.Id);
        supported.SupportCount.ShouldBe(1);
        supported.SupportedByMe.ShouldBeTrue();
        (await author.PutAsync(route + "/support", null)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await reader.DeleteAsync(route + "/support")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ReadCard(reader, idea.Id)).SupportCount.ShouldBe(0);

        var changes = original with { SupportEnabled = false, AnonymousEnabled = true, NotificationsEnabled = false };
        (await reader.PutAsJsonAsync(settingsPath, changes)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var updated = await leader.PutAsJsonAsync(settingsPath, changes);
        updated.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await leader.PutAsJsonAsync(settingsPath, changes)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await reader.PutAsync(route + "/support", null)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var created = await author.PostAsJsonAsync(Suggestions, new
        {
            title = "Anonymous", improvementObject = "Object", currentFlaws = "Problem", proposedSolution = "Solution",
            targetDepartmentId = department.Id, isAnonymous = true
        });
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = await created.Content.ReadFromJsonAsync<Guid>();
        (await author.PostAsync($"{Suggestions}/{id}/submit", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var anonymous = await ReadCard(reader, id);
        anonymous.AuthorName.ShouldBeNull();
        anonymous.CreatedBy.ShouldBeNull();
        var listing = await reader.GetFromJsonAsync<SuggestionDto[]>(Suggestions, JsonOptions);
        listing!.Single(s => s.Id == id).CreatedBy.ShouldBeNull();
        (await leader.PostAsJsonAsync(route + "/return", new { reason = "Измените" })).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await author.GetFromJsonAsync<TestNotice[]>("/api/Users/notifications", JsonOptions))!.ShouldBeEmpty();

        var latest = (await updated.Content.ReadFromJsonAsync<TestSettings>())!;
        (await leader.PutAsJsonAsync(settingsPath, latest with { BoardEnabled = false })).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await reader.GetAsync($"{Suggestions}/{id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ReadCard(author, id)).IsMine.ShouldBeTrue();
        (await leader.GetAsync($"{Suggestions}/{id}")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Directory_exposes_only_requested_users_to_leader()
    {
        var department = await CreateDepartment("Directory");
        await TestApp.RunAsUserAsync("special@example.test", FixturePassword, [Roles.User], department.Id);
        await TestApp.RunAsUserAsync("other@example.test", FixturePassword, [Roles.User]);
        await TestApp.RunAsUserAsync("leader@example.test", FixturePassword, [Roles.AdminL1]);
        using var employee = await Login("special@example.test");
        using var leader = await Login("leader@example.test");
        const string directory = "/api/Users/directory";
        (await employee.GetAsync(directory)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var matching = await leader.GetFromJsonAsync<PagedResult<TestDirectoryEntry>>(
            $"{directory}?search=SPECIAL&role=User&departmentId={department.Id}");
        matching!.TotalCount.ShouldBe(1);
        var item = matching.Items.Single();
        item.Email.ShouldBe("special@example.test");
        item.DepartmentName.ShouldBe("Directory");
        item.Roles.ShouldBe(new[] { Roles.User });

        var response = await leader.GetStringAsync(directory);
        response.ShouldNotContain("passwordHash");
        response.ShouldNotContain("securityStamp");
    }

    private static async Task<HttpResponseMessage> Create(HttpClient client, SuggestionInput input, string? key = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Suggestions + "/")
        {
            Content = JsonContent.Create(input)
        };
        if (key is not null)
            request.Headers.Add("Idempotency-Key", key);

        return await client.SendAsync(request);
    }

    private static async Task<SuggestionDetailsDto> ReadCard(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<SuggestionDetailsDto>($"{Suggestions}/{id}", JsonOptions))!;

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
    private sealed record TestSettings(bool SupportEnabled, bool AnonymousEnabled, bool NotificationsEnabled, bool BoardEnabled, Guid Version);
    private sealed record TestNotice(Guid SuggestionId, SuggestionStatus Status);
    private sealed record TestDirectoryEntry(string Email, string DepartmentName, string[] Roles);
    private sealed record SuggestionInput(
        string Title, string CurrentFlaws, string ProposedSolution, string ImprovementObject,
        Guid TargetDepartmentId, Guid? TemplateId = null);
}
