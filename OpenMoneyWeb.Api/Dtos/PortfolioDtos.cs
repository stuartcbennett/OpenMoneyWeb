namespace OpenMoneyWeb.Api.Dtos;

public record ReturnResultDto(decimal? YearToDate, decimal? OneYear, decimal? ThreeYear, decimal? AllTime);

public record PortfolioInvestmentDto(
    InvestmentDto Investment,
    decimal Quantity,
    decimal CurrentPrice,
    decimal MarketValue,
    ReturnResultDto Returns);

public record PortfolioAccountDto(
    AccountDto Account,
    List<PortfolioInvestmentDto> Investments,
    decimal TotalMarketValue,
    ReturnResultDto Returns);

public record PortfolioDto(
    List<PortfolioAccountDto> Accounts,
    decimal TotalMarketValue,
    ReturnResultDto Returns);
