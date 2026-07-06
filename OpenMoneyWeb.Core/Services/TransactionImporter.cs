using System.Globalization;
using System.Text;
using OpenMoneyWeb.Core.Models;

namespace OpenMoneyWeb.Core.Services;

public class ImportResult
{
    public List<Transaction> Transactions { get; } = new();
    public List<string> Errors { get; } = new();
    public string? AccountName { get; set; }
    public Dictionary<string, decimal> NewInvestments { get; } = new(StringComparer.OrdinalIgnoreCase);
}

public static class TransactionImporter
{
    public static ImportResult ImportFromText(
        string text,
        IReadOnlyDictionary<string, Investment> investmentsByName,
        IReadOnlyDictionary<string, Account> accountsByName)
    {
        var result = new ImportResult();
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        bool inDataSection = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("Investment Transactions", StringComparison.OrdinalIgnoreCase))
                continue;

            if (result.AccountName is null)
            {
                result.AccountName = line;
                continue;
            }

            if (!inDataSection)
            {
                if (line.StartsWith("Date,", StringComparison.OrdinalIgnoreCase))
                    inDataSection = true;
                continue;
            }

            if (!char.IsDigit(line[0])) continue;

            var tx = ParseCsvLine(line, investmentsByName, accountsByName, result.AccountName, result);
            if (tx != null)
                result.Transactions.Add(tx);
        }

        return result;
    }

    private static Transaction? ParseCsvLine(
        string line,
        IReadOnlyDictionary<string, Investment> investmentsByName,
        IReadOnlyDictionary<string, Account> accountsByName,
        string? accountName,
        ImportResult result)
    {
        try
        {
            //Format: Date,Investment,Activity,Quantity,Price,Commission,Total
            var fields = SplitCsvLine(line);
            if (fields.Length < 5) return null;

            if (!DateTime.TryParse(fields[0].Trim(), out var date)) return null;

            string investmentName = fields[1].Trim();
            string activityStr    = fields[2].Trim();

            if (!TryParseDecimal(fields[3], out var qty))
                return null;

            if (!TryParseDecimal(fields[4], out var price))
                return null;

            // fields[5] is commission (may be empty) — skip it
            decimal total = 0;
            if (fields.Length >= 7)
            {
                TryParseDecimal(fields[6], out total);
            }

            if (total == 0 && qty != 0 && price != 0) total = qty * price;
            if (price == 0 && qty != 0 && total != 0) price = total / qty;
            if (qty   == 0 && price != 0 && total != 0) qty = total / price;

            ActivityType activity = activityStr switch
            {
                "Reinvest Dividend" => ActivityType.ReinvestDividend,
                "Reinvest Interest" => ActivityType.ReinvestInterest,
                "Sell"              => ActivityType.Sell,
                _                   => ActivityType.Buy
            };

            if (!investmentsByName.TryGetValue(investmentName, out var investment))
            {
                if (!result.NewInvestments.ContainsKey(investmentName))
                    result.NewInvestments[investmentName] = price;
                return null;
            }

            if (accountName is null || !accountsByName.TryGetValue(accountName, out var account))
            {
                result.Errors.Add($"Unknown account '{accountName}'");
                return null;
            }

            return new Transaction
            {
                Date         = date,
                InvestmentId = investment.Id,
                AccountId    = account.Id,
                Activity     = activity,
                Quantity     = qty,
                Price        = price,
                Total        = total
            };
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Parse error: {ex.Message} — line: {line}");
            return null;
        }
    }

    private static string[] SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString().Trim());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        fields.Add(current.ToString().Trim());
        return fields.ToArray();
    }

    private static bool TryParseDecimal(string value, out decimal result)
    {
        var normalized = value.Trim().Trim('"').TrimStart('$');
        normalized = normalized.Replace(",", string.Empty);
        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }
}
