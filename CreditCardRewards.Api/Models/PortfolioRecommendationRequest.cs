namespace CreditCardRewards.Api.Models
{
  public class PortfolioRecommendationRequest
  {
    public decimal Amount { get; set; }
    public string Merchant { get; set; } = null!;
    public string Category { get; set; } = null!;
    public bool EnableMilestoneMode { get; set; } = false;
  }
}
