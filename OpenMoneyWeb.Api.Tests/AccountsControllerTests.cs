using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OpenMoneyWeb.Api.Dtos;
using OpenMoneyWeb.Core.Models;

namespace OpenMoneyWeb.Api.Tests;

public class AccountsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public AccountsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_WhenNoAccounts_ReturnsEmptyArray()
    {
        var response = await _client.GetAsync("/api/accounts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var accounts = await response.Content.ReadFromJsonAsync<AccountDto[]>(JsonOpts);
        accounts.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_ValidAccount_Returns201WithId()
    {
        var response = await _client.PostAsJsonAsync("/api/accounts",
            new CreateAccountDto("Savings", "My Bank"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var account = await response.Content.ReadFromJsonAsync<AccountDto>(JsonOpts);
        account!.Id.Should().BeGreaterThan(0);
        account.Name.Should().Be("Savings");
    }

    [Fact]
    public async Task Get_ExistingAccount_ReturnsAccount()
    {
        _factory.Seed(db => db.Accounts.Add(new Account { Name = "Checking", Institution = "Bank" }));

        var all = await _client.GetFromJsonAsync<AccountDto[]>("/api/accounts", JsonOpts);
        var id = all![0].Id;
        var response = await _client.GetAsync($"/api/accounts/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync("/api/accounts/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_NonExistentId_Returns404()
    {
        var response = await _client.PutAsJsonAsync("/api/accounts/99999",
            new CreateAccountDto("Name", "Bank"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_AlwaysReturns204()
    {
        // Even for a non-existent id the repo is a silent no-op
        var response = await _client.DeleteAsync("/api/accounts/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
