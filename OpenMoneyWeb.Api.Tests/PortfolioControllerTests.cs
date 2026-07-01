using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OpenMoneyWeb.Api.Dtos;
using OpenMoneyWeb.Core.Models;

namespace OpenMoneyWeb.Api.Tests;

public class PortfolioControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public PortfolioControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPortfolio_EmptyDatabase_ReturnsZeroTotals()
    {
        var response = await _client.GetAsync("/api/portfolio");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var portfolio = await response.Content.ReadFromJsonAsync<PortfolioDto>(JsonOpts);
        portfolio!.TotalMarketValue.Should().Be(0);
        portfolio.Accounts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPortfolio_FullySoldPosition_ExcludedFromOutput()
    {
        _factory.Seed(db =>
        {
            var account = new Account { Name = "Test", Institution = "Bank" };
            var inv = new Investment { Name = "AAPL", Type = InvestmentType.Stock, InitialPrice = 100m };
            db.Accounts.Add(account);
            db.Investments.Add(inv);
            db.SaveChanges();
            // Buy 10, then sell all 10 → net quantity = 0 → should be excluded
            db.Transactions.AddRange(
                new Transaction { AccountId = account.Id, InvestmentId = inv.Id, Date = DateTime.Today.AddDays(-10), Activity = ActivityType.Buy, Quantity = 10, Price = 100m, Total = 1000m },
                new Transaction { AccountId = account.Id, InvestmentId = inv.Id, Date = DateTime.Today.AddDays(-5), Activity = ActivityType.Sell, Quantity = 10, Price = 110m, Total = 1100m });
        });

        var response = await _client.GetAsync("/api/portfolio");
        var portfolio = await response.Content.ReadFromJsonAsync<PortfolioDto>(JsonOpts);

        portfolio!.Accounts.Should().HaveCount(1);
        portfolio.Accounts[0].Investments.Should().BeEmpty();
        portfolio.TotalMarketValue.Should().Be(0);
    }

    [Fact]
    public async Task GetPortfolio_NoPriceHistory_FallsBackToInitialPrice()
    {
        _factory.Seed(db =>
        {
            var account = new Account { Name = "Test", Institution = "Bank" };
            var inv = new Investment { Name = "AAPL", Type = InvestmentType.Stock, InitialPrice = 150m };
            db.Accounts.Add(account);
            db.Investments.Add(inv);
            db.SaveChanges();
            db.Transactions.Add(new Transaction
            {
                AccountId = account.Id, InvestmentId = inv.Id, Date = DateTime.Today.AddDays(-30),
                Activity = ActivityType.Buy, Quantity = 10, Price = 100m, Total = 1000m
            });
            // No PriceHistory records → service should use InitialPrice = 150
        });

        var response = await _client.GetAsync("/api/portfolio");
        var portfolio = await response.Content.ReadFromJsonAsync<PortfolioDto>(JsonOpts);

        portfolio!.Accounts[0].Investments[0].CurrentPrice.Should().Be(150m);
        portfolio.TotalMarketValue.Should().Be(1500m); // 10 * 150
    }

    [Fact]
    public async Task GetPortfolio_WithPriceHistory_UsesMostRecentPrice()
    {
        _factory.Seed(db =>
        {
            var account = new Account { Name = "Test", Institution = "Bank" };
            var inv = new Investment { Name = "AAPL", Type = InvestmentType.Stock, InitialPrice = 100m };
            db.Accounts.Add(account);
            db.Investments.Add(inv);
            db.SaveChanges();
            db.Transactions.Add(new Transaction
            {
                AccountId = account.Id, InvestmentId = inv.Id, Date = DateTime.Today.AddDays(-30),
                Activity = ActivityType.Buy, Quantity = 5, Price = 100m, Total = 500m
            });
            db.PriceHistories.AddRange(
                new PriceHistory { InvestmentId = inv.Id, Date = DateTime.Today.AddDays(-10), Price = 120m },
                new PriceHistory { InvestmentId = inv.Id, Date = DateTime.Today.AddDays(-1), Price = 200m });
        });

        var response = await _client.GetAsync("/api/portfolio");
        var portfolio = await response.Content.ReadFromJsonAsync<PortfolioDto>(JsonOpts);

        portfolio!.Accounts[0].Investments[0].CurrentPrice.Should().Be(200m);
        portfolio.TotalMarketValue.Should().Be(1000m); // 5 * 200
    }
}
