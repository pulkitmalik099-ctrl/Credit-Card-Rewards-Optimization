namespace CreditCardRewards.Core.Dtos
{
  public class CardSpendEntry
  {
    public Guid CardId { get; set; }
    public decimal CurrentSpend { get; set; }
  }
}
