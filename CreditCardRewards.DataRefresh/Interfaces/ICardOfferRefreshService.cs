using CreditCardRewards.DataRefresh.Models;

namespace CreditCardRewards.DataRefresh.Interfaces
{
    public interface ICardOfferRefreshService
    {
        Task<List<CardOfferRefreshResult>> BuildRefreshPlanAsync(List<Guid>? cardIds = null);
    }
}
