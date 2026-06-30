using Microsoft.EntityFrameworkCore;
using OpenMoneyWeb.Core.Models;

namespace OpenMoneyWeb.Data.Repositories;

public class AccountRepository
{
    private readonly AppDbContext _db;
    public AccountRepository(AppDbContext db) => _db = db;

    public Task<List<Account>> GetAllAsync() =>
        _db.Accounts.OrderBy(a => a.Name).ToListAsync();

    public Task<Account?> GetByIdAsync(int id) =>
        _db.Accounts.FindAsync(id).AsTask();

    public async Task<Account> AddAsync(Account account)
    {
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();
        return account;
    }

    public async Task UpdateAsync(Account account)
    {
        _db.Accounts.Update(account);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var a = await _db.Accounts.FindAsync(id);
        if (a != null) { _db.Accounts.Remove(a); await _db.SaveChangesAsync(); }
    }
}
