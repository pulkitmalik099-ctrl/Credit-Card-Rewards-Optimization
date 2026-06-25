using CreditCardRewards.DataRefresh.Models;

namespace CreditCardRewards.DataRefresh.Interfaces
{
    public interface IStatementParserService
    {
        Task<ParsedStatement> ParseAsync(string filePath);
        IReadOnlyList<ParsedStatement> GetPending();
        void MarkConfirmed(string statementId);
        void Remove(string statementId);
    }
}
