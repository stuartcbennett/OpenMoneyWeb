namespace OpenMoneyWeb.Core.Models;

public class Transaction
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public int InvestmentId { get; set; }
    public DateTime Date { get; set; }
    public ActivityType Activity { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Total { get; set; }
    public string? Memo { get; set; }

    public Account Account { get; set; } = null!;
    public Investment Investment { get; set; } = null!;
}
