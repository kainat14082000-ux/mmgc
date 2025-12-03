using MMGC.Models;

namespace MMGC.Services;

public interface ITransactionService
{
    Task<IEnumerable<Transaction>> GetAllTransactionsAsync();
    Task<Transaction?> GetTransactionByIdAsync(int id);
    Task<Transaction> CreateTransactionAsync(Transaction transaction);
    Task UpdateTransactionAsync(Transaction transaction);
    Task DeleteTransactionAsync(int id);
    Task<string> GenerateInvoiceAsync(int transactionId);
}
