using Microsoft.AspNetCore.Mvc;
using OpenMoneyWeb.Api.Dtos;
using OpenMoneyWeb.Core.Models;
using OpenMoneyWeb.Data.Repositories;

namespace OpenMoneyWeb.Api.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController : ControllerBase
{
    private readonly TransactionRepository _repo;
    public TransactionsController(TransactionRepository repo) => _repo = repo;

    [HttpPost]
    public async Task<ActionResult<TransactionDto>> Create(CreateTransactionDto dto, [FromServices] InvestmentRepository invRepo)
    {
        var inv = await invRepo.GetByIdAsync(dto.InvestmentId);
        var tx = new Transaction
        {
            AccountId    = dto.AccountId,
            InvestmentId = dto.InvestmentId,
            Date         = dto.Date,
            Activity     = Enum.Parse<ActivityType>(dto.Activity),
            Quantity     = dto.Quantity,
            Price        = dto.Price,
            Total        = dto.Total,
            Memo         = dto.Memo
        };
        await _repo.AddAsync(tx);
        return CreatedAtAction(nameof(Create), new TransactionDto(
            tx.Id, tx.AccountId, tx.InvestmentId, inv?.Name ?? string.Empty,
            tx.Date, tx.Activity.ToString(), tx.Quantity, tx.Price, tx.Total, tx.Memo));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, CreateTransactionDto dto)
    {
        var all = await _repo.GetAllAsync();
        var tx = all.FirstOrDefault(t => t.Id == id);
        if (tx is null) return NotFound();
        tx.AccountId    = dto.AccountId;
        tx.InvestmentId = dto.InvestmentId;
        tx.Date         = dto.Date;
        tx.Activity     = Enum.Parse<ActivityType>(dto.Activity);
        tx.Quantity     = dto.Quantity;
        tx.Price        = dto.Price;
        tx.Total        = dto.Total;
        tx.Memo         = dto.Memo;
        await _repo.UpdateAsync(tx);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _repo.DeleteAsync(id);
        return NoContent();
    }
}
