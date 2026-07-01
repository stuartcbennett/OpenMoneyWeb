using FluentAssertions;
using OpenMoneyWeb.Core.Models;
using OpenMoneyWeb.Data.Repositories;

namespace OpenMoneyWeb.Data.Tests;

public class AccountRepositoryTests : RepositoryTestBase
{
    private readonly AccountRepository _repo;

    public AccountRepositoryTests() => _repo = new AccountRepository(Db);

    [Fact]
    public async Task GetAllAsync_ReturnsAccountsOrderedByName()
    {
        Db.Accounts.AddRange(
            new Account { Name = "Zed Bank", Institution = "Z" },
            new Account { Name = "Alpha Bank", Institution = "A" },
            new Account { Name = "Mid Bank", Institution = "M" });
        await Db.SaveChangesAsync();

        var result = await _repo.GetAllAsync();

        result.Select(a => a.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task AddAsync_PersistsAccountAndPopulatesId()
    {
        var account = new Account { Name = "Test", Institution = "Bank" };

        var added = await _repo.AddAsync(account);

        added.Id.Should().BeGreaterThan(0);
        (await _repo.GetByIdAsync(added.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ModifiesExistingAccount()
    {
        var account = await _repo.AddAsync(new Account { Name = "Old Name", Institution = "Bank" });
        account.Name = "New Name";

        await _repo.UpdateAsync(account);

        var updated = await _repo.GetByIdAsync(account.Id);
        updated!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task DeleteAsync_RemovesAccount()
    {
        var account = await _repo.AddAsync(new Account { Name = "To Delete", Institution = "Bank" });

        await _repo.DeleteAsync(account.Id);

        (await _repo.GetByIdAsync(account.Id)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_IsNoOp()
    {
        var act = async () => await _repo.DeleteAsync(99999);
        await act.Should().NotThrowAsync();
    }
}
