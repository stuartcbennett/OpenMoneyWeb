using OpenMoneyWeb.Api.Dtos;
using OpenMoneyWeb.Core.Models;
using OpenMoneyWeb.Core.Services;
using OpenMoneyWeb.Data.Repositories;

namespace OpenMoneyWeb.Api.Services;

public class ReportsService
{
    private readonly TransactionRepository _transactions;
    private readonly InvestmentRepository _investments;

    public ReportsService(TransactionRepository transactions, InvestmentRepository investments)
    {
        _transactions = transactions;
        _investments = investments;
    }

    public async Task<List<NetWorthPointDto>> GetNetWorthOverTimeAsync()
    {
        var allTransactions = await _transactions.GetAllAsync();
        if (!allTransactions.Any()) return [];

        var allPriceHistory = await _investments.GetAllPriceHistoryAsync();
        return BuildValueSeries(allTransactions, allPriceHistory, allTransactions.Min(t => t.Date), DateTime.Today);
    }

    public async Task<List<NetWorthPointDto>> GetAccountValueOverTimeAsync(int accountId)
    {
        var txs = await _transactions.GetByAccountAsync(accountId);
        if (!txs.Any()) return [];

        var priceHistory = await _investments.GetAllPriceHistoryAsync();
        var relevantInvestmentIds = txs.Select(t => t.InvestmentId).ToHashSet();
        var relevantPrices = priceHistory.Where(p => relevantInvestmentIds.Contains(p.InvestmentId)).ToList();

        return BuildValueSeries(txs, relevantPrices, txs.Min(t => t.Date), DateTime.Today);
    }

    private static List<NetWorthPointDto> BuildValueSeries(
        IReadOnlyList<Transaction> transactions,
        IReadOnlyList<PriceHistory> priceHistory,
        DateTime minDate,
        DateTime maxDate)
    {
        var result = new List<NetWorthPointDto>();

        for (var month = new DateTime(minDate.Year, minDate.Month, 1); month <= maxDate; month = month.AddMonths(1))
        {
            var endOfMonth = month.AddMonths(1).AddDays(-1);
            if (endOfMonth > maxDate) endOfMonth = maxDate;

            var holdings = transactions
                .Where(t => t.Date <= endOfMonth)
                .GroupBy(t => t.InvestmentId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(t => t.Activity == ActivityType.Sell ? -t.Quantity : t.Quantity));

            decimal totalValue = 0;
            foreach (var (investmentId, quantity) in holdings)
            {
                if (quantity <= 0) continue;
                var latestPrice = priceHistory
                    .Where(p => p.InvestmentId == investmentId && p.Date <= endOfMonth)
                    .OrderByDescending(p => p.Date)
                    .FirstOrDefault()?.Price ?? 0;
                totalValue += quantity * latestPrice;
            }

            result.Add(new NetWorthPointDto(endOfMonth, totalValue));
        }

        return result;
    }

    public async Task<List<PriceHistoryDto>> GetInvestmentPriceHistoryAsync(int investmentId)
    {
        // Combine manual PriceHistory entries with prices recorded on transactions —
        // most investments only ever get a price via imported/entered transactions.
        var manualHistory = await _investments.GetPriceHistoryAsync(investmentId);
        var txHistory = await _transactions.GetByInvestmentAsync(investmentId);

        return manualHistory
            .Select(p => new PriceHistoryDto(p.Id, p.InvestmentId, p.Date, p.Price))
            .Concat(txHistory.Where(t => t.Price > 0).Select(t => new PriceHistoryDto(0, investmentId, t.Date, t.Price)))
            .OrderBy(p => p.Date)
            .ToList();
    }

    public async Task<List<InvestmentPerformanceDto>> GetInvestmentPerformanceAsync()
    {
        var allTransactions = await _transactions.GetAllAsync();
        var allInvestments = await _investments.GetAllAsync();
        var latestPrices = await _investments.GetLatestPricesAsync();
        var today = DateTime.Today;

        var result = new List<InvestmentPerformanceDto>();
        foreach (var inv in allInvestments)
        {
            var txs = allTransactions.Where(t => t.InvestmentId == inv.Id).ToList();
            if (!txs.Any()) continue;

            decimal quantity = txs.Sum(t => t.Activity == ActivityType.Sell ? -t.Quantity : t.Quantity);
            if (quantity <= 0) continue;

            decimal price = latestPrices.TryGetValue(inv.Id, out var p) ? p : inv.InitialPrice;
            decimal marketValue = quantity * price;
            var returns = ReturnCalculator.Calculate(txs, marketValue, today);

            result.Add(new InvestmentPerformanceDto(
                new InvestmentDto(inv.Id, inv.Name, inv.Ticker, inv.Type.ToString(), inv.InitialPrice),
                quantity, price, marketValue,
                new ReturnResultDto(returns.YearToDate, returns.OneYear, returns.ThreeYear, returns.AllTime)));
        }

        return result;
    }
}
