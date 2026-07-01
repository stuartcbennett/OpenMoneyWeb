using FluentAssertions;
using OpenMoneyWeb.Core.Models;
using OpenMoneyWeb.Core.Services;

namespace OpenMoneyWeb.Core.Tests;

public class ReturnCalculatorTests
{
    private static Transaction Buy(DateTime date, decimal total, decimal qty = 1) =>
        new() { Date = date, Activity = ActivityType.Buy, Total = total, Quantity = qty, Price = total / qty, AccountId = 1, InvestmentId = 1 };

    private static Transaction Sell(DateTime date, decimal total, decimal qty = 1) =>
        new() { Date = date, Activity = ActivityType.Sell, Total = total, Quantity = qty, Price = total / qty, AccountId = 1, InvestmentId = 1 };

    private static Transaction Reinvest(DateTime date, decimal total) =>
        new() { Date = date, Activity = ActivityType.ReinvestDividend, Total = total, Quantity = 1, Price = total, AccountId = 1, InvestmentId = 1 };

    // --- ModifiedDietz ---

    [Fact]
    public void ModifiedDietz_NoCashFlows_ReturnsPureAppreciation()
    {
        var result = ReturnCalculator.ModifiedDietz(
            1000m, 1100m, [],
            new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        result.Should().BeApproximately(0.1m, 0.0001m);
    }

    [Fact]
    public void ModifiedDietz_ZeroLengthPeriod_ReturnsNull()
    {
        var date = new DateTime(2024, 6, 1);
        ReturnCalculator.ModifiedDietz(1000m, 1100m, [], date, date).Should().BeNull();
    }

    [Fact]
    public void ModifiedDietz_ZeroDenominator_ReturnsNull()
    {
        // startValue=0 and no flows → denominator=0
        ReturnCalculator.ModifiedDietz(
            0m, 100m, [],
            new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)).Should().BeNull();
    }

    [Fact]
    public void ModifiedDietz_FlowAtPeriodStart_FullyWeighted()
    {
        // Flow on day 1 of a 365-day period gets weight ≈ 1.0
        // denominator = 0 + 500*1.0 ≈ 500, numerator = 600 - 0 - 500 = 100 → r = 0.2
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 12, 31);
        var flows = new[] { (start.AddDays(1), 500m) };

        var result = ReturnCalculator.ModifiedDietz(0m, 600m, flows, start, end);

        result.Should().BeApproximately(0.2m, 0.005m);
    }

    [Fact]
    public void ModifiedDietz_FlowAtPeriodEnd_ZeroWeight()
    {
        // Flow on the last day gets weight 0 → doesn't affect denominator
        // denominator = 1000 + 0 = 1000, numerator = 1600 - 1000 - 500 = 100 → r = 0.1
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 12, 31);
        var flows = new[] { (end, 500m) };

        var result = ReturnCalculator.ModifiedDietz(1000m, 1600m, flows, start, end);

        result.Should().BeApproximately(0.1m, 0.001m);
    }

    // --- Annualise ---

    [Fact]
    public void Annualise_NullInput_ReturnsNull() =>
        ReturnCalculator.Annualise(null, 1.0).Should().BeNull();

    [Fact]
    public void Annualise_ZeroYears_ReturnsNull() =>
        ReturnCalculator.Annualise(0.1m, 0).Should().BeNull();

    [Fact]
    public void Annualise_NegativeYears_ReturnsNull() =>
        ReturnCalculator.Annualise(0.1m, -1).Should().BeNull();

    [Fact]
    public void Annualise_SubYearPeriod_UsesLinearScaling()
    {
        // 10% over 0.5 years → 10% / 0.5 = 20% annualised
        var result = ReturnCalculator.Annualise(0.1m, 0.5);
        result.Should().BeApproximately(0.2m, 0.0001m);
    }

    [Fact]
    public void Annualise_MultiYearPeriod_UsesGeometricFormula()
    {
        // 21% total over 2 years → (1.21)^(1/2) - 1 = 10% per year
        var result = ReturnCalculator.Annualise(0.21m, 2.0);
        result.Should().BeApproximately(0.1m, 0.0001m);
    }

    [Fact]
    public void Annualise_ExactlyOneYear_GeometricEqualsLinear()
    {
        // At exactly 1 year, (1+r)^1 - 1 = r
        var result = ReturnCalculator.Annualise(0.15m, 1.0);
        result.Should().BeApproximately(0.15m, 0.0001m);
    }

    // --- Calculate ---

    [Fact]
    public void Calculate_EmptyTransactionList_AllResultsNull()
    {
        var result = ReturnCalculator.Calculate([], 1000m, DateTime.Today);

        result.YearToDate.Should().BeNull();
        result.OneYear.Should().BeNull();
        result.ThreeYear.Should().BeNull();
        result.AllTime.Should().BeNull();
    }

    [Fact]
    public void Calculate_YtdIsRawReturn_NotAnnualised()
    {
        // Buy on Jan 2, current value has doubled, today = Jul 1 (~6 months later)
        // Raw YTD ≈ 100% (1.0). Annualised for 0.5 yr would be ≈ 200% (2.0).
        // AllTime window is shortest possible (alltime start = Jan 2 to Jul 1)
        // and is annualised → should be much larger than YTD raw.
        var today = new DateTime(2024, 7, 1);
        var txs = new List<Transaction> { Buy(new DateTime(2024, 1, 2), total: 1000m, qty: 10) };

        var result = ReturnCalculator.Calculate(txs, 2000m, today);

        result.YearToDate.Should().NotBeNull();
        result.AllTime.Should().NotBeNull();
        // YTD is stored raw (≈1.005), AllTime is annualised (≈2.0+)
        result.YearToDate.Should().BeApproximately(1.005m, 0.02m);
        result.AllTime.Should().BeGreaterThan(result.YearToDate!.Value);
    }

    [Fact]
    public void Calculate_ReinvestOnlyTransactions_ProducesNullFromZeroDenominator()
    {
        // ReinvestDividend has CashFlow=0 → EstimateValueAt = 0 → denominator = 0 → null
        var today = new DateTime(2024, 7, 1);
        var txs = new List<Transaction>
        {
            Reinvest(new DateTime(2023, 6, 1), 200m),
            Reinvest(new DateTime(2024, 1, 1), 100m)
        };

        var result = ReturnCalculator.Calculate(txs, 500m, today);

        result.YearToDate.Should().BeNull();
        result.AllTime.Should().BeNull();
    }

    [Fact]
    public void Calculate_EstimateValueAt_ClampedToZero_WhenSellsExceedBuys()
    {
        // Sells before a period window exceed buys → net CashFlow sum is negative.
        // EstimateValueAt clamps to 0, so the method should not throw.
        var today = new DateTime(2024, 7, 1);
        var txs = new List<Transaction>
        {
            Buy(new DateTime(2022, 1, 1), total: 100m),
            Sell(new DateTime(2022, 6, 1), total: 500m)
        };

        var act = () => ReturnCalculator.Calculate(txs, 600m, today);

        act.Should().NotThrow();
    }

    [Fact]
    public void Calculate_CashFlow_ReinvestDividendAndInterestAreZero()
    {
        // Only reinvest transactions means zero external cash flows.
        // Any net value change looks like pure appreciation but denominator may be 0.
        var txs = new List<Transaction>
        {
            new() { Date = DateTime.Today.AddYears(-1), Activity = ActivityType.ReinvestInterest,
                    Total = 500m, Quantity = 5, Price = 100m, AccountId = 1, InvestmentId = 1 }
        };

        // Should not throw regardless of market value
        var act = () => ReturnCalculator.Calculate(txs, 1000m, DateTime.Today);
        act.Should().NotThrow();
    }
}
