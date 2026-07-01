using Microsoft.AspNetCore.Mvc;
using OpenMoneyWeb.Api.Dtos;
using OpenMoneyWeb.Core.Services;
using OpenMoneyWeb.Data.Repositories;

namespace OpenMoneyWeb.Api.Controllers;

[ApiController]
[Route("api/import")]
public class ImportController : ControllerBase
{
    private readonly InvestmentRepository _investments;
    private readonly AccountRepository _accounts;
    private readonly TransactionRepository _transactions;

    public ImportController(InvestmentRepository investments, AccountRepository accounts, TransactionRepository transactions)
    {
        _investments = investments;
        _accounts = accounts;
        _transactions = transactions;
    }

    [HttpPost]
    public async Task<ActionResult<ImportResultDto>> Import(ImportRequestDto dto)
    {
        var investmentsByName = (await _investments.GetAllAsync())
            .ToDictionary(i => i.Name, i => i, StringComparer.OrdinalIgnoreCase);
        var accountsByName = (await _accounts.GetAllAsync())
            .ToDictionary(a => a.Name, a => a, StringComparer.OrdinalIgnoreCase);

        var result = TransactionImporter.ImportFromText(dto.Text, investmentsByName, accountsByName);

        if (result.Transactions.Any())
            await _transactions.AddRangeAsync(result.Transactions);

        var resultDto = new ImportResultDto(result.Transactions.Count, result.Errors, result.NewInvestments);

        if (result.Errors.Any() && !result.Transactions.Any())
            return UnprocessableEntity(resultDto);
        if (result.Errors.Any())
            return StatusCode(207, resultDto);
        return Ok(resultDto);
    }
}
