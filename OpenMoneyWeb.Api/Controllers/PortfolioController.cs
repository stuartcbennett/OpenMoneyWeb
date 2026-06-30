using Microsoft.AspNetCore.Mvc;
using OpenMoneyWeb.Api.Dtos;
using OpenMoneyWeb.Api.Services;

namespace OpenMoneyWeb.Api.Controllers;

[ApiController]
[Route("api/portfolio")]
public class PortfolioController : ControllerBase
{
    private readonly PortfolioService _service;
    public PortfolioController(PortfolioService service) => _service = service;

    [HttpGet]
    public Task<PortfolioDto> Get() => _service.GetPortfolioAsync();
}
