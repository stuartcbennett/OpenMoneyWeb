using Microsoft.EntityFrameworkCore;
using OpenMoneyWeb.Core.Models;

namespace OpenMoneyWeb.Data.Repositories;

public class InvestmentRepository
{
    private readonly AppDbContext _db;
    public InvestmentRepository(AppDbContext db) => _db = db;

    public Task<List<Investment>> GetAllAsync() =>
        _db.Investments.OrderBy(i => i.Name).ToListAsync();

    public Task<Investment?> GetByIdAsync(int id) =>
        _db.Investments.FindAsync(id).AsTask();

    public Task<List<PriceHistory>> GetPriceHistoryAsync(int investmentId) =>
        _db.PriceHistories
           .Where(p => p.InvestmentId == investmentId)
           .OrderBy(p => p.Date)
           .ToListAsync();

    public Task<List<PriceHistory>> GetAllPriceHistoryAsync() =>
        _db.PriceHistories.OrderBy(p => p.Date).ToListAsync();

    public Task<Dictionary<int, decimal>> GetLatestPricesAsync() =>
        _db.PriceHistories
           .GroupBy(p => p.InvestmentId)
           .Select(g => new { InvestmentId = g.Key, Price = g.OrderByDescending(p => p.Date).First().Price })
           .ToDictionaryAsync(x => x.InvestmentId, x => x.Price);

    public async Task<decimal?> GetLatestPriceAsync(int investmentId)
    {
        var latest = await _db.PriceHistories
            .Where(p => p.InvestmentId == investmentId)
            .OrderByDescending(p => p.Date)
            .FirstOrDefaultAsync();
        return latest?.Price;
    }

    public async Task<Investment> AddAsync(Investment investment)
    {
        _db.Investments.Add(investment);
        await _db.SaveChangesAsync();
        return investment;
    }

    public async Task UpdateAsync(Investment investment)
    {
        _db.Investments.Update(investment);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var i = await _db.Investments.FindAsync(id);
        if (i != null) { _db.Investments.Remove(i); await _db.SaveChangesAsync(); }
    }

    public async Task AddPriceAsync(int investmentId, DateTime date, decimal price)
    {
        _db.PriceHistories.Add(new PriceHistory { InvestmentId = investmentId, Date = date, Price = price });
        await _db.SaveChangesAsync();
    }
}
