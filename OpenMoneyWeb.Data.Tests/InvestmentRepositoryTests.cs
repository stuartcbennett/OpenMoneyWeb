using FluentAssertions;
using OpenMoneyWeb.Core.Models;
using OpenMoneyWeb.Data.Repositories;

namespace OpenMoneyWeb.Data.Tests;

public class InvestmentRepositoryTests : RepositoryTestBase
{
    private readonly InvestmentRepository _repo;

    public InvestmentRepositoryTests() => _repo = new InvestmentRepository(Db);

    private async Task<Investment> AddInvestmentAsync(string name = "AAPL")
    {
        var inv = new Investment { Name = name, Type = InvestmentType.Stock, InitialPrice = 100m };
        Db.Investments.Add(inv);
        await Db.SaveChangesAsync();
        return inv;
    }

    [Fact]
    public async Task GetAllAsync_ReturnsInvestmentsOrderedByName()
    {
        Db.Investments.AddRange(
            new Investment { Name = "ZZZZ", Type = InvestmentType.Stock, InitialPrice = 1m },
            new Investment { Name = "AAAA", Type = InvestmentType.Stock, InitialPrice = 1m });
        await Db.SaveChangesAsync();

        var result = await _repo.GetAllAsync();

        result.Select(i => i.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetLatestPriceAsync_NoHistory_ReturnsNull()
    {
        var inv = await AddInvestmentAsync();

        var price = await _repo.GetLatestPriceAsync(inv.Id);

        price.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestPriceAsync_ReturnsMostRecentPrice()
    {
        var inv = await AddInvestmentAsync();
        await _repo.AddPriceAsync(inv.Id, new DateTime(2024, 1, 1), 100m);
        await _repo.AddPriceAsync(inv.Id, new DateTime(2024, 6, 1), 150m);
        await _repo.AddPriceAsync(inv.Id, new DateTime(2024, 3, 1), 120m);

        var price = await _repo.GetLatestPriceAsync(inv.Id);

        price.Should().Be(150m);
    }

    [Fact]
    public async Task GetLatestPricesAsync_ReturnsMostRecentPricePerInvestment()
    {
        var aapl = await AddInvestmentAsync("AAPL");
        var msft = await AddInvestmentAsync("MSFT");

        await _repo.AddPriceAsync(aapl.Id, new DateTime(2024, 1, 1), 100m);
        await _repo.AddPriceAsync(aapl.Id, new DateTime(2024, 6, 1), 180m); // latest for AAPL
        await _repo.AddPriceAsync(msft.Id, new DateTime(2024, 3, 1), 300m); // latest for MSFT
        await _repo.AddPriceAsync(msft.Id, new DateTime(2024, 1, 1), 250m);

        var prices = await _repo.GetLatestPricesAsync();

        prices.Should().HaveCount(2);
        prices[aapl.Id].Should().Be(180m);
        prices[msft.Id].Should().Be(300m);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_IsNoOp()
    {
        var act = async () => await _repo.DeleteAsync(99999);
        await act.Should().NotThrowAsync();
    }
}
