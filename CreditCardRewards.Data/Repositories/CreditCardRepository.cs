using Microsoft.EntityFrameworkCore;
using CreditCardRewards.Data.Context;
using CreditCardRewards.Data.Models;

namespace CreditCardRewards.Data.Repositories
{
    public interface ICreditCardRepository : IRepository<CreditCard>
    {
        Task<CreditCard?> GetCardWithDetailsAsync(Guid id);
        Task<List<CreditCard>> GetAllCardsWithDetailsAsync();
    }

    public class CreditCardRepository : GenericRepository<CreditCard>, ICreditCardRepository
    {
        public CreditCardRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<CreditCard?> GetCardWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(c => c.AcceleratedCategories)
                .Include(c => c.RewardCaps)
                .Include(c => c.Milestones)
                .Include(c => c.Offers)
                .Include(c => c.Transactions)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<List<CreditCard>> GetAllCardsWithDetailsAsync()
        {
            return await _dbSet
                .Include(c => c.AcceleratedCategories)
                .Include(c => c.RewardCaps)
                .Include(c => c.Milestones)
                .Include(c => c.Offers)
                .Include(c => c.Transactions)
                .ToListAsync();
        }
    }
}
