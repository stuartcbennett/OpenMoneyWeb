using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.Json;
using FluentAssertions;
using OpenMoneyWeb.Api.Dtos;
using OpenMoneyWeb.Core.Models;

namespace OpenMoneyWeb.Api.Tests;

public class TransactionsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public TransactionsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
        _client = factory.CreateClient();
    }

    private (int accountId, int investmentId) SeedAccountAndInvestment()
    {
        int accountId = 0, investmentId = 0;
        _factory.Seed(db =>
        {
            var account = new Account { Name = "Test", Institution = "Bank" };
            var investment = new Investment { Name = "AAPL", Type = InvestmentType.Stock, InitialPrice = 100m };
            db.Accounts.Add(account);
            db.Investments.Add(investment);
            db.SaveChanges();
            accountId = account.Id;
            investmentId = investment.Id;
        });
        return (accountId, investmentId);
    }

    [Fact]
    public async Task Create_InvalidActivityType_Returns400()
    {
        var (accountId, investmentId) = SeedAccountAndInvestment();
        var dto = new CreateTransactionDto(accountId, investmentId, DateTime.Today, "NotAValidActivity", 10, 100, 1000, null);

        var response = await _client.PostAsJsonAsync("/api/transactions", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ValidTransaction_Returns201()
    {
        var (accountId, investmentId) = SeedAccountAndInvestment();
        var dto = new CreateTransactionDto(accountId, investmentId, DateTime.Today, "Buy", 10, 100, 1000, null);

        var response = await _client.PostAsJsonAsync("/api/transactions", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Update_NonExistentId_Returns404()
    {
        var (accountId, investmentId) = SeedAccountAndInvestment();
        var dto = new CreateTransactionDto(accountId, investmentId, DateTime.Today, "Buy", 10, 100, 1000, null);

        var response = await _client.PutAsJsonAsync("/api/transactions/99999", dto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_InvalidActivityType_Returns400()
    {
        var (accountId, investmentId) = SeedAccountAndInvestment();
        // Create a valid transaction first
        var create = new CreateTransactionDto(accountId, investmentId, DateTime.Today, "Buy", 10, 100, 1000, null);
        var created = await _client.PostAsJsonAsync("/api/transactions", create);
        var tx = await created.Content.ReadFromJsonAsync<TransactionDto>(JsonOpts);

        // Update with an invalid activity
        var update = new CreateTransactionDto(accountId, investmentId, DateTime.Today, "NotValid", 10, 100, 1000, null);
        var response = await _client.PutAsJsonAsync($"/api/transactions/{tx!.Id}", update);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_ValidData_Returns204()
    {
        var (accountId, investmentId) = SeedAccountAndInvestment();
        var create = new CreateTransactionDto(accountId, investmentId, DateTime.Today, "Buy", 10, 100, 1000, null);
        var created = await _client.PostAsJsonAsync("/api/transactions", create);
        var tx = await created.Content.ReadFromJsonAsync<TransactionDto>(JsonOpts);

        var update = new CreateTransactionDto(accountId, investmentId, DateTime.Today, "Sell", 5, 120, 600, null);
        var response = await _client.PutAsJsonAsync($"/api/transactions/{tx!.Id}", update);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
