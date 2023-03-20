using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;
using Tomlyn;
using Tomlyn.Model;
using uralsib2beancount;

string configFile = args[0];
string outputFile = args[1];

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var config = Toml.ToModel(File.ReadAllText(configFile, Encoding.UTF8));
TomlTable cardsTable = (TomlTable)config["cards"];
TomlTable categoriesTable = (TomlTable)config["categories"];
TomlTable csvLastNonHeaderTable = (TomlTable)config["uralsib_csv_last_nonheader"];

NumberFormatInfo numberFormatInfo = new NumberFormatInfo();
numberFormatInfo.NumberDecimalSeparator = ",";
var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    Delimiter = ";",
};
List<Transaction> transactions = new List<Transaction>();
for (int argN = 2; argN < args.Length; argN++)
{
    using (StreamReader reader = new StreamReader(args[argN], System.Text.Encoding.GetEncoding("windows-1251")))
    {
        // Пропустить 8 строк в начале - они не CSV. Но там есть номер счёта - запомнить его
        string accountNumber = "";
        string? lastNonHeader = null;
        for (int i = 0; i < 8; i++)
        {
            string? line = reader.ReadLine();
            if (line?.StartsWith("Cчет: ") ?? false)
            {
                accountNumber = line.Substring(6);
                lastNonHeader = (string)csvLastNonHeaderTable[accountNumber];
            }
            if (lastNonHeader != null && (line?.StartsWith(lastNonHeader) ?? false))
            {
                break;
            }
        }
        using (CsvReader csvReader = new CsvReader(reader, csvConfig))
        {
            var entries = csvReader.GetRecords<TransactionEntry>().ToList();
            Console.WriteLine($"{args[argN]}: {entries.Count} entries read");
            transactions.AddRange(entries.Select(e => ParseCsvEntry(accountNumber, e)));
        }
    }
}



using (StreamWriter writer = new StreamWriter(outputFile, false, Encoding.UTF8))
{
    foreach (var t in transactions.OrderBy(t => t.Date))
    {
        if (t.Description.Contains("ЭНЕРДЖИНС с кредитной линией"))
        {
            continue;
        }
        // Write header line
        writer.WriteLine($"{t.Date.ToString("yyyy-MM-dd")} * \"{t.Description}\"");
        // Write MCC if exists, otherwise 0
        writer.WriteLine($"  mcc: {t.Mcc ?? "0"}");
        // Write main expense account
        string? cardNumber = t.CardNumber?.TrimStart('*');
        if (cardNumber == null || !cardsTable.TryGetValue(cardNumber, out object account))
        {
            account = "XX";
        }
        writer.WriteLine($"  {account}     {t.TotalValue.ToString("F2", CultureInfo.InvariantCulture)} {t.Currency ?? "XXX"}");

        // Write category
        if (t.Description == "Перевод между счетами")
        {
            writer.WriteLine($"  YY");
        }
        else if (t.Description.Contains("Кешбэк по карте Мир"))
        {
            writer.WriteLine($"  {categoriesTable["uralsib_cashback_mir"]}");
        }
        else if (t.TotalValue < 0 && categoriesTable.ContainsKey(t.Description))
        {
            writer.WriteLine($"  {categoriesTable[t.Description]}");
        }
        else if (t.TotalValue > 0)
        {
            writer.WriteLine($"  Income:");
        }
        else
        {
            writer.WriteLine($"  Expenses:");
        }
        writer.WriteLine();
    }
}

Transaction ParseCsvEntry(string cardName, TransactionEntry transactionEntry)
{
    DateOnly date = DateOnly.FromDateTime(DateTime.Now);
    if (transactionEntry.Date is string d)
    {
        date = DateOnly.FromDateTime(DateTime.Parse(d));
    }

    decimal totalValue = 0.0M;
    if (transactionEntry.TotalValue is string v)
    {
        totalValue = Convert.ToDecimal(v, numberFormatInfo);
    }

    string category = "";
    if (transactionEntry.Category is string c)
    {
        category = c;
    }

    return new Transaction(date, cardName, totalValue, category, transactionEntry.Mcc == "" ? null : transactionEntry.Mcc, transactionEntry.Description ?? "", transactionEntry.Currency);
}