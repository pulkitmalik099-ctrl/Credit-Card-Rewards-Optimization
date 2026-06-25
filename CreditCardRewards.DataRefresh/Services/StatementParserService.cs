using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;
using OpenAI.Chat;
using CreditCardRewards.DataRefresh.Interfaces;
using CreditCardRewards.DataRefresh.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CreditCardRewards.DataRefresh.Services
{
    public class StatementParserService : IStatementParserService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<StatementParserService> _logger;
        private readonly ConcurrentDictionary<string, ParsedStatement> _pending = new();

        public StatementParserService(IConfiguration config, ILogger<StatementParserService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<ParsedStatement> ParseAsync(string filePath)
        {
            var statement = new ParsedStatement
            {
                FileName = Path.GetFileName(filePath),
                FilePath = filePath
            };

            try
            {
                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                string rawText = ext == ".csv"
                    ? await File.ReadAllTextAsync(filePath)
                    : ExtractPdfText(filePath);

                if (string.IsNullOrWhiteSpace(rawText))
                {
                    statement.ParseError = "Could not extract text from file.";
                    statement.Status = ParsedStatementStatus.Failed;
                    _pending[statement.Id] = statement;
                    return statement;
                }

                var parsed = await CallOpenAiAsync(rawText, statement.FileName);
                if (parsed != null)
                {
                    statement.DetectedIssuer = parsed.DetectedIssuer;
                    statement.DetectedCardName = parsed.DetectedCardName;
                    statement.StatementPeriodStart = parsed.StatementPeriodStart;
                    statement.StatementPeriodEnd = parsed.StatementPeriodEnd;
                    statement.TotalSpend = parsed.TotalSpend;
                    statement.RewardPointsEarned = parsed.RewardPointsEarned;
                    statement.Transactions = parsed.Transactions;
                }
                else
                {
                    statement.ParseError = "OpenAI could not parse this statement format.";
                    statement.Status = ParsedStatementStatus.Failed;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse statement {File}", filePath);
                statement.ParseError = ex.Message;
                statement.Status = ParsedStatementStatus.Failed;
            }

            _pending[statement.Id] = statement;
            return statement;
        }

        public IReadOnlyList<ParsedStatement> GetPending() =>
            _pending.Values.Where(s => s.Status == ParsedStatementStatus.Pending).ToList();

        public void MarkConfirmed(string statementId)
        {
            if (_pending.TryGetValue(statementId, out var s))
                s.Status = ParsedStatementStatus.Confirmed;
        }

        public void Remove(string statementId) => _pending.TryRemove(statementId, out _);

        private static string ExtractPdfText(string filePath)
        {
            var sb = new StringBuilder();
            using var doc = PdfDocument.Open(filePath);
            foreach (var page in doc.GetPages())
                sb.AppendLine(page.Text);
            return sb.ToString();
        }

        private async Task<ParsedStatement?> CallOpenAiAsync(string rawText, string fileName)
        {
            var apiKey = _config["OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("OpenAI API key not configured. Cannot parse statement.");
                return null;
            }

            var truncatedText = rawText.Length > 12000 ? rawText[..12000] : rawText;

            var prompt = $$"""
                You are a financial data extractor for Indian credit card statements.
                Parse the statement text and return ONLY a valid JSON object.

                Statement file: {{fileName}}
                Statement text:
                ---
                {{truncatedText}}
                ---

                Return ONLY this JSON (no markdown, no explanation):
                {
                  "detectedIssuer": "bank name e.g. HDFC, ICICI, SBI, Axis",
                  "detectedCardName": "card name if found, else null",
                  "statementPeriodStart": "YYYY-MM-DD or null",
                  "statementPeriodEnd": "YYYY-MM-DD or null",
                  "totalSpend": 0,
                  "rewardPointsEarned": 0,
                  "transactions": [
                    {
                      "date": "YYYY-MM-DD",
                      "merchant": "merchant name",
                      "amount": 0,
                      "rewardPoints": 0,
                      "category": "Dining|Online Shopping|Travel|Fuel|Groceries|Entertainment|Healthcare|Utilities|Shopping|Other or null"
                    }
                  ]
                }

                Rules:
                - Only include debit/purchase transactions (exclude payments, refunds, credits)
                - If a field cannot be determined, use null or 0
                - Return ONLY the JSON, no other text
                """;

            try
            {
                var client = new ChatClient("gpt-4o", apiKey);
                var messages = new List<ChatMessage> { new UserChatMessage(prompt) };
                var completion = await client.CompleteChatAsync(messages);
                var json = completion.Value.Content[0].Text.Trim();

                if (json.StartsWith("```"))
                    json = string.Join("\n", json.Split('\n').Skip(1).TakeWhile(l => !l.StartsWith("```")));

                return JsonSerializer.Deserialize<ParsedStatement>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI statement parse failed");
                return null;
            }
        }
    }
}
