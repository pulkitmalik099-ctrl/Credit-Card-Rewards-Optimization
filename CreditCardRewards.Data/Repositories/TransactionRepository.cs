using Microsoft.EntityFrameworkCore;
using CreditCardRewards.Data.Context;
using CreditCardRewards.Data.Models;

namespace CreditCardRewards.Data.Repositories
{
    public interface ITransactionRepository : IRepository<Transaction>
    {
        Task<List<Transaction>> GetCardTransactionsAsync(Guid cardId, int? year = null, int? month = null);
        Task<decimal> GetCardMonthlySpendAsync(Guid cardId, int month, int year);
        Task<decimal> GetCardAnnualSpendAsync(Guid cardId, int year);
    }

    public class TransactionRepository : GenericRepository<Transaction>, ITransactionRepository
    {
        public TransactionRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<Transaction>> GetCardTransactionsAsync(Guid cardId, int? year = null, int? month = null)
        {
            var query = _dbSet.Where(t => t.CreditCardId == cardId);

            if (year.HasValue)
                query = query.Where(t => t.TransactionDate.Year == year.Value);

            if (month.HasValue)
                query = query.Where(t => t.TransactionDate.Month == month.Value);

            return await query.OrderByDescending(t => t.TransactionDate).ToListAsync();
        }

        public async Task<decimal> GetCardMonthlySpendAsync(Guid cardId, int month, int year)
        {
            return await _dbSet
                .Where(t => t.CreditCardId == cardId &&
                           t.TransactionDate.Year == year &&
                           t.TransactionDate.Month == month)
                .SumAsync(t => t.Amount);
        }

        public async Task<decimal> GetCardAnnualSpendAsync(Guid cardId, int year)
        {
            return await _dbSet
                .Where(t => t.CreditCardId == cardId && t.TransactionDate.Year == year)
                .SumAsync(t => t.Amount);
        }
    }
}
