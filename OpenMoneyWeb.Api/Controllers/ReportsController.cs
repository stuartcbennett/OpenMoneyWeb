using Microsoft.AspNetCore.Mvc;
using OpenMoneyWeb.Api.Dtos;
using OpenMoneyWeb.Api.Services;

namespace OpenMoneyWeb.Api.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly ReportsService _service;
    public ReportsController(ReportsService service) => _service = service;

    [HttpGet("networth")]
    public Task<List<NetWorthPointDto>> GetNetWorth() =>
        _service.GetNetWorthOverTimeAsync();

    [HttpGet("account/{accountId}/value")]
    public Task<List<NetWorthPointDto>> GetAccountValue(int accountId) =>
        _service.GetAccountValueOverTimeAsync(accountId);

    [HttpGet("investments/{investmentId}/pricehistory")]
    public Task<List<PriceHistoryDto>> GetInvestmentPriceHistory(int investmentId) =>
        _service.GetInvestmentPriceHistoryAsync(investmentId);

    [HttpGet("investments/performance")]
    public Task<List<InvestmentPerformanceDto>> GetPerformance() =>
        _service.GetInvestmentPerformanceAsync();
}
