using OpenMoneyWeb.Api.Dtos;
using OpenMoneyWeb.Core.Models;
using OpenMoneyWeb.Core.Services;
using OpenMoneyWeb.Data.Repositories;

namespace OpenMoneyWeb.Api.Services;

public class PortfolioService
{
    private readonly AccountRepository _accounts;
    private readonly TransactionRepository _transactions;
    private readonly InvestmentRepository _investments;

    public PortfolioService(AccountRepository accounts, TransactionRepository transactions, InvestmentRepository investments)
    {
        _accounts = accounts;
        _transactions = transactions;
        _investments = investments;
    }

    public async Task<PortfolioDto> GetPortfolioAsync()
    {
        var accounts = await _accounts.GetAllAsync();
        var allTransactions = await _transactions.GetAllAsync();
        var allInvestments = await _investments.GetAllAsync();
        var latestPrices = await _investments.GetLatestPricesAsync();
        var today = DateTime.Today;

        decimal effectivePrice(Investment inv) =>
            latestPrices.TryGetValue(inv.Id, out var p) ? p : inv.InitialPrice;

        var portfolioAccounts = new List<PortfolioAccountDto>();
        decimal portfolioTotal = 0;

        foreach (var account in accounts)
        {
            var accountTxs = allTransactions.Where(t => t.AccountId == account.Id).ToList();
            var portfolioInvestments = new List<PortfolioInvestmentDto>();

            foreach (var group in accountTxs.GroupBy(t => t.InvestmentId))
            {
                var inv = allInvestments.FirstOrDefault(i => i.Id == group.Key);
                if (inv == null) continue;

                var txList = group.ToList();
                decimal quantity = txList.Sum(t => t.Activity == ActivityType.Sell ? -t.Quantity : t.Quantity);
                if (quantity <= 0) continue;

                decimal price = effectivePrice(inv);
                decimal marketValue = quantity * price;
                var returns = ReturnCalculator.Calculate(txList, marketValue, today);

                portfolioInvestments.Add(new PortfolioInvestmentDto(
                    MapInvestment(inv), quantity, price, marketValue, MapReturns(returns)));
            }

            decimal accountTotal = portfolioInvestments.Sum(i => i.MarketValue);
            portfolioTotal += accountTotal;
            var accountReturns = ReturnCalculator.Calculate(accountTxs, accountTotal, today);

            portfolioAccounts.Add(new PortfolioAccountDto(
                MapAccount(account),
                portfolioInvestments,
                accountTotal,
                MapReturns(accountReturns)));
        }

        var portfolioReturns = ReturnCalculator.Calculate(allTransactions, portfolioTotal, today);
        return new PortfolioDto(portfolioAccounts, portfolioTotal, MapReturns(portfolioReturns));
    }

    private static AccountDto MapAccount(Account a) =>
        new(a.Id, a.Name, a.Institution);

    private static InvestmentDto MapInvestment(Investment i) =>
        new(i.Id, i.Name, i.Ticker, i.Type.ToString(), i.InitialPrice);

    private static ReturnResultDto MapReturns(ReturnResult r) =>
        new(r.YearToDate, r.OneYear, r.ThreeYear, r.AllTime);
}
