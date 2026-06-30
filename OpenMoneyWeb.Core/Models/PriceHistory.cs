namespace OpenMoneyWeb.Core.Models;

public class PriceHistory
{
    public int Id { get; set; }
    public int InvestmentId { get; set; }
    public DateTime Date { get; set; }
    public decimal Price { get; set; }

    public Investment Investment { get; set; } = null!;
}
