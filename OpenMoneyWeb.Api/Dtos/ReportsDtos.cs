namespace OpenMoneyWeb.Api.Dtos;

public record NetWorthPointDto(DateTime Date, decimal Value);

public record PriceHistoryDto(int Id, int InvestmentId, DateTime Date, decimal Price);

public record InvestmentPerformanceDto(
    InvestmentDto Investment,
    decimal Quantity,
    decimal CurrentPrice,
    decimal MarketValue,
    ReturnResultDto Returns);

public record ImportRequestDto(string Text);

public record ImportResultDto(int TransactionCount, List<string> Errors, Dictionary<string, decimal> NewInvestments);
