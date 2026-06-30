using Microsoft.AspNetCore.Mvc;
using OpenMoneyWeb.Api.Dtos;
using OpenMoneyWeb.Core.Models;
using OpenMoneyWeb.Data.Repositories;

namespace OpenMoneyWeb.Api.Controllers;

[ApiController]
[Route("api/accounts")]
public class AccountsController : ControllerBase
{
    private readonly AccountRepository _repo;
    public AccountsController(AccountRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IEnumerable<AccountDto>> GetAll() =>
        (await _repo.GetAllAsync()).Select(a => new AccountDto(a.Id, a.Name, a.Institution));

    [HttpGet("{id}")]
    public async Task<ActionResult<AccountDto>> Get(int id)
    {
        var a = await _repo.GetByIdAsync(id);
        return a is null ? NotFound() : new AccountDto(a.Id, a.Name, a.Institution);
    }

    [HttpPost]
    public async Task<ActionResult<AccountDto>> Create(CreateAccountDto dto)
    {
        var account = new Account { Name = dto.Name, Institution = dto.Institution };
        await _repo.AddAsync(account);
        return CreatedAtAction(nameof(Get), new { id = account.Id }, new AccountDto(account.Id, account.Name, account.Institution));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, CreateAccountDto dto)
    {
        var account = await _repo.GetByIdAsync(id);
        if (account is null) return NotFound();
        account.Name = dto.Name;
        account.Institution = dto.Institution;
        await _repo.UpdateAsync(account);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _repo.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{accountId}/transactions")]
    public async Task<IEnumerable<TransactionDto>> GetTransactions(int accountId, [FromServices] TransactionRepository txRepo)
    {
        var txs = await txRepo.GetByAccountAsync(accountId);
        return txs.Select(t => new TransactionDto(
            t.Id, t.AccountId, t.InvestmentId, t.Investment.Name,
            t.Date, t.Activity.ToString(), t.Quantity, t.Price, t.Total, t.Memo));
    }
}
