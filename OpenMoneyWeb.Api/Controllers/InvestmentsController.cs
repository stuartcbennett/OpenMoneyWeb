using Microsoft.AspNetCore.Mvc;
using OpenMoneyWeb.Api.Dtos;
using OpenMoneyWeb.Core.Models;
using OpenMoneyWeb.Data.Repositories;

namespace OpenMoneyWeb.Api.Controllers;

[ApiController]
[Route("api/investments")]
public class InvestmentsController : ControllerBase
{
    private readonly InvestmentRepository _repo;
    public InvestmentsController(InvestmentRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IEnumerable<InvestmentDto>> GetAll() =>
        (await _repo.GetAllAsync()).Select(Map);

    [HttpGet("{id}")]
    public async Task<ActionResult<InvestmentDto>> Get(int id)
    {
        var i = await _repo.GetByIdAsync(id);
        return i is null ? NotFound() : Map(i);
    }

    [HttpPost]
    public async Task<ActionResult<InvestmentDto>> Create(CreateInvestmentDto dto)
    {
        var inv = new Investment
        {
            Name = dto.Name,
            Ticker = dto.Ticker,
            Type = Enum.Parse<InvestmentType>(dto.Type),
            InitialPrice = dto.InitialPrice
        };
        await _repo.AddAsync(inv);
        return CreatedAtAction(nameof(Get), new { id = inv.Id }, Map(inv));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, CreateInvestmentDto dto)
    {
        var inv = await _repo.GetByIdAsync(id);
        if (inv is null) return NotFound();
        inv.Name = dto.Name;
        inv.Ticker = dto.Ticker;
        inv.Type = Enum.Parse<InvestmentType>(dto.Type);
        inv.InitialPrice = dto.InitialPrice;
        await _repo.UpdateAsync(inv);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _repo.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{id}/pricehistory")]
    public async Task<IEnumerable<PriceHistoryDto>> GetPriceHistory(int id)
    {
        var history = await _repo.GetPriceHistoryAsync(id);
        return history.Select(p => new PriceHistoryDto(p.Id, p.InvestmentId, p.Date, p.Price));
    }

    [HttpPost("{id}/price")]
    public async Task<IActionResult> AddPrice(int id, AddPriceDto dto)
    {
        await _repo.AddPriceAsync(id, dto.Date, dto.Price);
        return Created();
    }

    private static InvestmentDto Map(Investment i) =>
        new(i.Id, i.Name, i.Ticker, i.Type.ToString(), i.InitialPrice);
}
