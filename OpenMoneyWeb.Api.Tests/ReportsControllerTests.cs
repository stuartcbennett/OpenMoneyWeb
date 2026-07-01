using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OpenMoneyWeb.Api.Dtos;
using OpenMoneyWeb.Core.Models;

namespace OpenMoneyWeb.Api.Tests;

public class ReportsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ReportsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetNetWorthOverTime_NoTransactions_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/reports/networth");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var series = await response.Content.ReadFromJsonAsync<NetWorthPointDto[]>(JsonOpts);
        series.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNetWorthOverTime_BuildsMonthlySeries()
    {
        _factory.Seed(db =>
        {
            var account = new Account { Name = "Test", Institution = "Bank" };
            var inv = new Investment { Name = "AAPL", Type = InvestmentType.Stock, InitialPrice = 100m };
            db.Accounts.Add(account);
            db.Investments.Add(inv);
            db.SaveChanges();
            // Transaction 6 months ago
            db.Transactions.Add(new Transaction
            {
                AccountId = account.Id, InvestmentId = inv.Id,
                Date = DateTime.Today.AddMonths(-6),
                Activity = ActivityType.Buy, Quantity = 10, Price = 100m, Total = 1000m
            });
            db.PriceHistories.Add(new PriceHistory
            {
                InvestmentId = inv.Id, Date = DateTime.Today.AddMonths(-3), Price = 120m
            });
        });

        var response = await _client.GetAsync("/api/reports/networth");
        var series = await response.Content.ReadFromJsonAsync<NetWorthPointDto[]>(JsonOpts);

        // Should have at least 6 monthly data points
        series.Should().HaveCountGreaterThanOrEqualTo(6);
        // All dates should be end-of-month or clamped to today
        series!.Select(p => p.Date).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetInvestmentPerformance_SoldOutInvestment_Excluded()
    {
        _factory.Seed(db =>
        {
            var account = new Account { Name = "Test", Institution = "Bank" };
            var inv = new Investment { Name = "AAPL", Type = InvestmentType.Stock, InitialPrice = 100m };
            db.Accounts.Add(account);
            db.Investments.Add(inv);
            db.SaveChanges();
            db.Transactions.AddRange(
                new Transaction { AccountId = account.Id, InvestmentId = inv.Id, Date = DateTime.Today.AddDays(-30), Activity = ActivityType.Buy, Quantity = 10, Price = 100m, Total = 1000m },
                new Transaction { AccountId = account.Id, InvestmentId = inv.Id, Date = DateTime.Today.AddDays(-5), Activity = ActivityType.Sell, Quantity = 10, Price = 110m, Total = 1100m });
        });

        var response = await _client.GetAsync("/api/reports/investments/performance");
        var performance = await response.Content.ReadFromJsonAsync<InvestmentPerformanceDto[]>(JsonOpts);

        // Net qty = 0, so fully sold position should be excluded
        performance.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAccountValueOverTime_FilteredToSingleAccount()
    {
        int account1Id = 0, account2Id = 0;
        _factory.Seed(db =>
        {
            var acc1 = new Account { Name = "Account 1", Institution = "Bank" };
            var acc2 = new Account { Name = "Account 2", Institution = "Bank" };
            var inv = new Investment { Name = "AAPL", Type = InvestmentType.Stock, InitialPrice = 100m };
            db.Accounts.AddRange(acc1, acc2);
            db.Investments.Add(inv);
            db.SaveChanges();
            account1Id = acc1.Id;
            account2Id = acc2.Id;

            // Only account 1 has transactions
            db.Transactions.Add(new Transaction
            {
                AccountId = acc1.Id, InvestmentId = inv.Id,
                Date = DateTime.Today.AddMonths(-2),
                Activity = ActivityType.Buy, Quantity = 5, Price = 100m, Total = 500m
            });
        });

        var response1 = await _client.GetAsync($"/api/reports/account/{account1Id}/value");
        var response2 = await _client.GetAsync($"/api/reports/account/{account2Id}/value");

        var series1 = await response1.Content.ReadFromJsonAsync<NetWorthPointDto[]>(JsonOpts);
        var series2 = await response2.Content.ReadFromJsonAsync<NetWorthPointDto[]>(JsonOpts);

        series1.Should().NotBeEmpty();
        series2.Should().BeEmpty(); // account 2 has no transactions
    }
}
