namespace OpenMoneyWeb.Api.Dtos;

public record AccountDto(int Id, string Name, string Institution);
public record CreateAccountDto(string Name, string Institution);
