namespace OpenMoneyWeb.Api.Dtos;

public record InvestmentDto(int Id, string Name, string? Ticker, string Type, decimal InitialPrice);
public record CreateInvestmentDto(string Name, string? Ticker, string Type, decimal InitialPrice);
public record AddPriceDto(DateTime Date, decimal Price);
