using CreditCardRewards.DataRefresh.Models;

namespace CreditCardRewards.DataRefresh.Interfaces
{
    public interface ICardLookupService
    {
        Task<CardLookupResult?> LookupCardAsync(string cardName, string issuer);
    }
}
