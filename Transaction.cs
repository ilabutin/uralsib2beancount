namespace uralsib2beancount
{
    internal record class Transaction(DateOnly Date, string? CardNumber, decimal TotalValue, string Category, string? Mcc, string Description, string? Currency);
}
