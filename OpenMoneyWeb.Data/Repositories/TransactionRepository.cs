using Microsoft.EntityFrameworkCore;
using OpenMoneyWeb.Core.Models;

namespace OpenMoneyWeb.Data.Repositories;

public class TransactionRepository
{
    private readonly AppDbContext _db;
    public TransactionRepository(AppDbContext db) => _db = db;

    public Task<List<Transaction>> GetByAccountAsync(int accountId) =>
        _db.Transactions
           .Include(t => t.Investment)
           .Where(t => t.AccountId == accountId)
           .OrderByDescending(t => t.Date)
           .ToListAsync();

    public Task<List<Transaction>> GetAllAsync() =>
        _db.Transactions
           .Include(t => t.Account)
           .Include(t => t.Investment)
           .OrderByDescending(t => t.Date)
           .ToListAsync();

    public async Task<Transaction> AddAsync(Transaction tx)
    {
        _db.Transactions.Add(tx);
        await _db.SaveChangesAsync();
        return tx;
    }

    public async Task UpdateAsync(Transaction tx)
    {
        _db.Transactions.Update(tx);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var t = await _db.Transactions.FindAsync(id);
        if (t != null) { _db.Transactions.Remove(t); await _db.SaveChangesAsync(); }
    }

    public async Task AddRangeAsync(IEnumerable<Transaction> transactions)
    {
        _db.Transactions.AddRange(transactions);
        await _db.SaveChangesAsync();
    }
}
