using OpenMoneyWeb.Core.Models;

namespace OpenMoneyWeb.Core.Services;

public class ReturnResult
{
    public decimal? YearToDate { get; set; }
    public decimal? OneYear { get; set; }
    public decimal? ThreeYear { get; set; }
    public decimal? AllTime { get; set; }
}

public class PortfolioSnapshot
{
    public string Name { get; set; } = string.Empty;
    public decimal MarketValue { get; set; }
    public decimal Quantity { get; set; }
    public decimal CurrentPrice { get; set; }
    public ReturnResult Returns { get; set; } = new();
}

public static class ReturnCalculator
{
    // Modified Dietz return over [start, end]. Externally-weighted to account for mid-period cash flows.
    public static decimal? ModifiedDietz(
        decimal startValue,
        decimal endValue,
        IEnumerable<(DateTime date, decimal cashFlow)> cashFlows,
        DateTime start,
        DateTime end)
    {
        double totalDays = (end - start).TotalDays;
        if (totalDays <= 0) return null;

        double weightedFlows = 0;
        double sumFlows = 0;
        foreach (var (date, flow) in cashFlows)
        {
            double w = (totalDays - (date - start).TotalDays) / totalDays;
            weightedFlows += (double)flow * w;
            sumFlows += (double)flow;
        }

        double denominator = (double)startValue + weightedFlows;
        if (denominator == 0) return null;

        double r = ((double)endValue - (double)startValue - sumFlows) / denominator;
        return (decimal)r;
    }

    public static decimal? Annualise(decimal? periodReturn, double years)
    {
        if (periodReturn is null || years <= 0) return null;
        if (years < 1)
            return periodReturn.Value / (decimal)years;
        double result = Math.Pow(1 + (double)periodReturn.Value, 1.0 / years) - 1;
        if (!double.IsFinite(result) || result > (double)decimal.MaxValue || result < (double)decimal.MinValue)
            return null;
        return (decimal)result;
    }

    public static ReturnResult Calculate(
        IReadOnlyList<Transaction> transactions,
        decimal currentMarketValue,
        DateTime today)
    {
        var result = new ReturnResult();
        if (!transactions.Any()) return result;

        var periods = new (string name, DateTime start, DateTime end)[]
        {
            ("ytd",     new DateTime(today.Year, 1, 1), today),
            ("1yr",     today.AddYears(-1),             today),
            ("3yr",     today.AddYears(-3),             today),
            ("alltime", transactions.Min(t => t.Date),  today),
        };

        foreach (var (name, start, end) in periods)
        {
            var flows = transactions
                .Where(t => t.Date > start && t.Date <= end)
                .Select(t => (t.Date, CashFlow(t)));

            decimal startVal = EstimateValueAt(transactions, start);
            double years = (end - start).TotalDays / 365.25;
            var r = ModifiedDietz(startVal, currentMarketValue, flows, start, end);
            var annualised = Annualise(r, years);

            switch (name)
            {
                case "ytd":     result.YearToDate = r; break;
                case "1yr":     result.OneYear    = annualised; break;
                case "3yr":     result.ThreeYear  = annualised; break;
                case "alltime": result.AllTime    = annualised; break;
            }
        }

        return result;
    }

    private static decimal CashFlow(Transaction t) =>
        t.Activity switch
        {
            ActivityType.Buy              => t.Total,
            ActivityType.ReinvestDividend => 0,
            ActivityType.ReinvestInterest => 0,
            ActivityType.Sell             => -t.Total,
            _                             => 0
        };

    private static decimal EstimateValueAt(IReadOnlyList<Transaction> allTx, DateTime date) =>
        Math.Max(0, allTx.Where(t => t.Date <= date).Sum(CashFlow));
}
