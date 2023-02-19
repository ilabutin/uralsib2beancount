using CsvHelper.Configuration.Attributes;

namespace uralsib2beancount
{
    /// <summary>
    /// Transaction entry from CSV file
    /// </summary>
    internal class TransactionEntry
    {
        // Пропустить 8 строк в начале
        // Сведения об операции;Категории;Дата и время MSK;Сумма;Валюта;MCC

        [Name("Дата и время MSK")]
        public string? Date { get; set; }
        [Name("Сумма")]
        public string? TotalValue { get; set; }
        [Name("Категории")]
        public string? Category { get; set; }
        [Name("MCC")]
        public string? Mcc { get; set; }
        [Name("Сведения об операции")]
        public string? Description { get; set; }
        [Name("Валюта")]
        public string? Currency { get; set; }
    }
}
