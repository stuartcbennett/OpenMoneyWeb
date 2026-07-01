using FluentAssertions;
using OpenMoneyWeb.Core.Models;
using OpenMoneyWeb.Core.Services;

namespace OpenMoneyWeb.Core.Tests;

public class TransactionImporterTests
{
    private static readonly IReadOnlyDictionary<string, Investment> Investments =
        new Dictionary<string, Investment>(StringComparer.OrdinalIgnoreCase)
        {
            ["AAPL"] = new Investment { Id = 1, Name = "AAPL", Type = InvestmentType.Stock, InitialPrice = 100m },
            ["MSFT"] = new Investment { Id = 2, Name = "MSFT", Type = InvestmentType.Stock, InitialPrice = 200m }
        };

    private static readonly IReadOnlyDictionary<string, Account> Accounts =
        new Dictionary<string, Account>(StringComparer.OrdinalIgnoreCase)
        {
            ["My Account"] = new Account { Id = 1, Name = "My Account", Institution = "TestBank" }
        };

    private static ImportResult Import(string text) =>
        TransactionImporter.ImportFromText(text, Investments, Accounts);

    private static string Csv(params string[] dataRows)
    {
        var rows = string.Join("\n", dataRows);
        return $"Investment Transactions\nMy Account\nSome Header\nDate,Investment,Activity,Quantity,Price,Extra,Total\n{rows}";
    }

    // --- Happy path ---

    [Fact]
    public void Import_WellFormedRow_ParsesCorrectly()
    {
        var result = Import(Csv("01/15/2024,AAPL,Buy,10,150.00,,1500.00"));

        result.Transactions.Should().HaveCount(1);
        var tx = result.Transactions[0];
        tx.Date.Should().Be(new DateTime(2024, 1, 15));
        tx.InvestmentId.Should().Be(1);
        tx.AccountId.Should().Be(1);
        tx.Activity.Should().Be(ActivityType.Buy);
        tx.Quantity.Should().Be(10m);
        tx.Price.Should().Be(150m);
        tx.Total.Should().Be(1500m);
        result.Errors.Should().BeEmpty();
    }

    // --- Header parsing ---

    [Fact]
    public void Import_AccountNameExtractedFromFirstNonSkippedLine()
    {
        var result = Import(Csv("01/15/2024,AAPL,Buy,10,150.00,,1500.00"));
        result.AccountName.Should().Be("My Account");
    }

    [Fact]
    public void Import_RowsBeforeDataSectionHeader_AreIgnored()
    {
        // Extra lines between account name and Date, header should be silently skipped
        var text = "Investment Transactions\nMy Account\nLine 2\nLine 3\nDate,Investment,Activity,Quantity,Price,Extra,Total\n01/15/2024,AAPL,Buy,10,150.00,,1500.00";
        var result = Import(text);
        result.Transactions.Should().HaveCount(1);
    }

    [Fact]
    public void Import_NonDigitRowAfterHeader_IsSkipped()
    {
        // Footer or summary lines that don't start with a digit are silently ignored
        var result = Import(Csv("Total,,,,,,9999.00", "01/15/2024,AAPL,Buy,10,150.00,,1500.00"));
        result.Transactions.Should().HaveCount(1);
    }

    // --- Dollar sign stripping ---

    [Fact]
    public void Import_DollarSignInPriceAndTotal_IsStripped()
    {
        var result = Import(Csv("01/15/2024,AAPL,Buy,10,$150.00,,$1500.00"));

        result.Transactions.Should().HaveCount(1);
        result.Transactions[0].Price.Should().Be(150m);
        result.Transactions[0].Total.Should().Be(1500m);
    }

    // --- Derived fields ---

    [Fact]
    public void Import_TotalIsZero_DerivedFromQtyTimesPrice()
    {
        var result = Import(Csv("01/15/2024,AAPL,Buy,10,150.00,,0"));

        result.Transactions[0].Total.Should().Be(1500m);
    }

    [Fact]
    public void Import_PriceIsZero_DerivedFromTotalDividedByQty()
    {
        var result = Import(Csv("01/15/2024,AAPL,Buy,10,0,,1500.00"));

        result.Transactions[0].Price.Should().Be(150m);
    }

    [Fact]
    public void Import_QtyIsZero_DerivedFromTotalDividedByPrice()
    {
        var result = Import(Csv("01/15/2024,AAPL,Buy,0,150.00,,1500.00"));

        result.Transactions[0].Quantity.Should().Be(10m);
    }

    // --- Activity mapping ---

    [Theory]
    [InlineData("Reinvest Dividend", ActivityType.ReinvestDividend)]
    [InlineData("Reinvest Interest", ActivityType.ReinvestInterest)]
    [InlineData("Sell", ActivityType.Sell)]
    [InlineData("Buy", ActivityType.Buy)]
    [InlineData("Unknown Activity", ActivityType.Buy)]
    public void Import_ActivityStrings_MappedCorrectly(string activityStr, ActivityType expected)
    {
        var result = Import(Csv($"01/15/2024,AAPL,{activityStr},10,150.00,,1500.00"));

        result.Transactions.Should().HaveCount(1);
        result.Transactions[0].Activity.Should().Be(expected);
    }

    // --- Error paths ---

    [Fact]
    public void Import_UnknownInvestment_AddedToNewInvestments_NotTransactions()
    {
        var result = Import(Csv("01/15/2024,TSLA,Buy,10,250.00,,2500.00"));

        result.Transactions.Should().BeEmpty();
        result.NewInvestments.Should().ContainKey("TSLA");
        result.NewInvestments["TSLA"].Should().Be(250m);
    }

    [Fact]
    public void Import_DuplicateUnknownInvestment_RecordedOnlyOnce()
    {
        var result = Import(Csv(
            "01/15/2024,TSLA,Buy,10,250.00,,2500.00",
            "01/20/2024,TSLA,Buy,5,260.00,,1300.00"));

        result.NewInvestments.Should().HaveCount(1);
        result.NewInvestments.Should().ContainKey("TSLA");
    }

    [Fact]
    public void Import_UnknownAccount_AddedToErrors_NotTransactions()
    {
        var result = TransactionImporter.ImportFromText(
            Csv("01/15/2024,AAPL,Buy,10,150.00,,1500.00"),
            Investments,
            new Dictionary<string, Account>());  // no accounts

        result.Transactions.Should().BeEmpty();
        result.Errors.Should().ContainMatch("*My Account*");
    }

    [Fact]
    public void Import_FewerThanFiveFields_RowSkipped()
    {
        var result = Import(Csv("01/15/2024,AAPL,Buy,10"));

        result.Transactions.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Import_InvalidDate_RowSkipped()
    {
        var result = Import(Csv("99/99/9999,AAPL,Buy,10,150.00,,1500.00"));

        result.Transactions.Should().BeEmpty();
    }

    // --- Mixed rows ---

    [Fact]
    public void Import_MixedValidAndInvalidRows_OnlyValidTransactionsImported()
    {
        var result = Import(Csv(
            "01/15/2024,AAPL,Buy,10,150.00,,1500.00",    // valid
            "01/16/2024,TSLA,Buy,5,250.00,,1250.00",     // unknown investment → NewInvestments
            "01/17/2024,MSFT,Sell,2,200.00,,400.00"));   // valid

        result.Transactions.Should().HaveCount(2);
        result.NewInvestments.Should().ContainKey("TSLA");
        result.Errors.Should().BeEmpty();
    }
}
