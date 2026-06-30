namespace OpenMoneyWeb.Api.Dtos;

public record TransactionDto(
    int Id,
    int AccountId,
    int InvestmentId,
    string InvestmentName,
    DateTime Date,
    string Activity,
    decimal Quantity,
    decimal Price,
    decimal Total,
    string? Memo);

public record CreateTransactionDto(
    int AccountId,
    int InvestmentId,
    DateTime Date,
    string Activity,
    decimal Quantity,
    decimal Price,
    decimal Total,
    string? Memo);
