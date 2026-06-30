namespace OpenMoneyWeb.Core.Models;

public class Investment
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Ticker { get; set; }
    public InvestmentType Type { get; set; }
    public decimal InitialPrice { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();
}
