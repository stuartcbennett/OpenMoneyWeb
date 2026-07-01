using FluentAssertions;
using OpenMoneyWeb.Core.Models;
using OpenMoneyWeb.Data.Repositories;

namespace OpenMoneyWeb.Data.Tests;

public class TransactionRepositoryTests : RepositoryTestBase
{
    private readonly TransactionRepository _repo;

    public TransactionRepositoryTests() => _repo = new TransactionRepository(Db);

    private async Task<(Account account, Investment investment)> SeedAccountAndInvestmentAsync()
    {
        var account = new Account { Name = "Test Account", Institution = "Bank" };
        var investment = new Investment { Name = "AAPL", Type = InvestmentType.Stock, InitialPrice = 100m };
        Db.Accounts.Add(account);
        Db.Investments.Add(investment);
        await Db.SaveChangesAsync();
        return (account, investment);
    }

    [Fact]
    public async Task GetByAccountAsync_FiltersToAccountAndIncludesInvestment()
    {
        var (account, investment) = await SeedAccountAndInvestmentAsync();
        var otherAccount = new Account { Name = "Other", Institution = "Other Bank" };
        Db.Accounts.Add(otherAccount);
        await Db.SaveChangesAsync();

        Db.Transactions.AddRange(
            new Transaction { AccountId = account.Id, InvestmentId = investment.Id, Date = DateTime.Today, Activity = ActivityType.Buy, Quantity = 5, Price = 100m, Total = 500m },
            new Transaction { AccountId = otherAccount.Id, InvestmentId = investment.Id, Date = DateTime.Today, Activity = ActivityType.Buy, Quantity = 3, Price = 100m, Total = 300m });
        await Db.SaveChangesAsync();

        var result = await _repo.GetByAccountAsync(account.Id);

        result.Should().HaveCount(1);
        result[0].Investment.Should().NotBeNull();
        result[0].Investment.Name.Should().Be("AAPL");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCorrectTransaction()
    {
        var (account, investment) = await SeedAccountAndInvestmentAsync();
        Db.Transactions.Add(new Transaction { AccountId = account.Id, InvestmentId = investment.Id, Date = DateTime.Today, Activity = ActivityType.Buy, Quantity = 10, Price = 150m, Total = 1500m });
        await Db.SaveChangesAsync();
        var all = await _repo.GetAllAsync();
        var id = all[0].Id;

        var tx = await _repo.GetByIdAsync(id);

        tx.Should().NotBeNull();
        tx!.Total.Should().Be(1500m);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var result = await _repo.GetByIdAsync(99999);
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddRangeAsync_PersistsAllTransactions()
    {
        var (account, investment) = await SeedAccountAndInvestmentAsync();
        var txs = Enumerable.Range(1, 5).Select(i => new Transaction
        {
            AccountId = account.Id,
            InvestmentId = investment.Id,
            Date = DateTime.Today.AddDays(-i),
            Activity = ActivityType.Buy,
            Quantity = i,
            Price = 100m,
            Total = i * 100m
        }).ToList();

        await _repo.AddRangeAsync(txs);

        (await _repo.GetByAccountAsync(account.Id)).Should().HaveCount(5);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_IsNoOp()
    {
        var act = async () => await _repo.DeleteAsync(99999);
        await act.Should().NotThrowAsync();
    }
}
