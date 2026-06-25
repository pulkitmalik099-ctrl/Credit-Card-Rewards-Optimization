using System.Text.Json;
using OpenAI.Chat;
using CreditCardRewards.DataRefresh.Interfaces;
using CreditCardRewards.DataRefresh.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CreditCardRewards.DataRefresh.Services
{
    public class CardLookupService : ICardLookupService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<CardLookupService> _logger;

        public CardLookupService(IConfiguration config, ILogger<CardLookupService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<CardLookupResult?> LookupCardAsync(string cardName, string issuer)
        {
            var apiKey = _config["OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("OpenAI API key not configured. Cannot perform card lookup.");
                return null;
            }

            try
            {
                var client = new ChatClient("gpt-4o", apiKey);

                var prompt = $$"""
                    You are a credit card rewards expert for Indian credit cards.
                    Return ONLY a valid JSON object (no markdown, no explanation) with the reward structure for:
                    Card: {{cardName}}
                    Issuer/Bank: {{issuer}}

                    JSON schema to follow exactly:
                    {
                      "cardName": "string",
                      "issuer": "string",
                      "baseRewardRate": number,
                      "baseRewardPointValue": number,
                      "baseRewardUnit": "Points or Cashback",
                      "annualFee": number,
                      "joiningFee": number,
                      "annualFeeWaiverSpendThreshold": number,
                      "acceleratedCategories": [
                        { "category": "string", "rewardRate": number, "monthlyCap": "string or null" }
                      ],
                      "welcomeOffer": "string or null",
                      "airportLoungeBenefits": "string or null",
                      "transferPartners": "string or null",
                      "isConfident": true or false
                    }

                    Notes:
                    - baseRewardRate: reward points earned per 100 INR spent (e.g. 3.3 = 3.3 pts per 100)
                    - baseRewardPointValue: INR value of 1 reward point (e.g. 0.50)
                    - All monetary values in INR
                    - Set isConfident to false if you are not sure about this card
                    - Return ONLY the JSON, no other text
                    """;

                var messages = new List<ChatMessage> { new UserChatMessage(prompt) };
                var completion = await client.CompleteChatAsync(messages);
                var json = completion.Value.Content[0].Text.Trim();

                // Strip markdown code fences if present
                if (json.StartsWith("```"))
                    json = string.Join("\n", json.Split('\n').Skip(1).TakeWhile(l => !l.StartsWith("```")));

                var result = JsonSerializer.Deserialize<CardLookupResult>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result != null)
                {
                    result.DataSource = "OpenAI";
                    result.FetchedAt = DateTime.UtcNow;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Card lookup failed for {CardName} / {Issuer}", cardName, issuer);
                return null;
            }
        }
    }
}
