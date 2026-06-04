using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreditCardRewards.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreditCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Issuer = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalLimit = table.Column<decimal>(type: "numeric", nullable: false),
                    JoiningFee = table.Column<decimal>(type: "numeric", nullable: false),
                    AnnualFee = table.Column<decimal>(type: "numeric", nullable: false),
                    AnnualFeeWaiverSpendThreshold = table.Column<decimal>(type: "numeric", nullable: false),
                    BaseRewardRate = table.Column<decimal>(type: "numeric", nullable: false),
                    BaseRewardUnit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BaseRewardPointValue = table.Column<decimal>(type: "numeric", nullable: true),
                    TransferPartners = table.Column<string>(type: "text", nullable: true),
                    AirportLoungeBenefits = table.Column<string>(type: "text", nullable: true),
                    HotelBenefits = table.Column<string>(type: "text", nullable: true),
                    TravelBenefits = table.Column<string>(type: "text", nullable: true),
                    OtherBenefits = table.Column<string>(type: "text", nullable: true),
                    WelcomeOffer = table.Column<string>(type: "text", nullable: true),
                    WelcomeOfferValue = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataSource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Milestones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditCardId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SpendThreshold = table.Column<decimal>(type: "numeric", nullable: false),
                    RewardValue = table.Column<decimal>(type: "numeric", nullable: false),
                    RewardUnit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsAutomatic = table.Column<bool>(type: "boolean", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Milestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Milestones_CreditCards_CreditCardId",
                        column: x => x.CreditCardId,
                        principalTable: "CreditCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RewardCaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditCardId = table.Column<Guid>(type: "uuid", nullable: false),
                    CapType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MaxRewardValue = table.Column<decimal>(type: "numeric", nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardCaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RewardCaps_CreditCards_CreditCardId",
                        column: x => x.CreditCardId,
                        principalTable: "CreditCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RewardCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditCardId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RewardMultiplier = table.Column<decimal>(type: "numeric", nullable: false),
                    Cap = table.Column<decimal>(type: "numeric", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RewardCategories_CreditCards_CreditCardId",
                        column: x => x.CreditCardId,
                        principalTable: "CreditCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RewardOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditCardId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Merchant = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RewardMultiplier = table.Column<decimal>(type: "numeric", nullable: false),
                    RewardBasis = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FlatBonusAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    RewardUnit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MaxRewardPerTransaction = table.Column<decimal>(type: "numeric", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RewardOffers_CreditCards_CreditCardId",
                        column: x => x.CreditCardId,
                        principalTable: "CreditCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditCardId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Merchant = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    PointsEarned = table.Column<decimal>(type: "numeric", nullable: false),
                    CashbackEarned = table.Column<decimal>(type: "numeric", nullable: false),
                    RewardValueInRupees = table.Column<decimal>(type: "numeric", nullable: false),
                    EffectiveReturnPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    ContributedToMilestone = table.Column<bool>(type: "boolean", nullable: false),
                    MilestoneId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContributedToFeeWaiver = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_CreditCards_CreditCardId",
                        column: x => x.CreditCardId,
                        principalTable: "CreditCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_CreditCardId",
                table: "Milestones",
                column: "CreditCardId");

            migrationBuilder.CreateIndex(
                name: "IX_RewardCaps_CreditCardId",
                table: "RewardCaps",
                column: "CreditCardId");

            migrationBuilder.CreateIndex(
                name: "IX_RewardCategories_CreditCardId_Category",
                table: "RewardCategories",
                columns: new[] { "CreditCardId", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RewardOffers_CreditCardId",
                table: "RewardOffers",
                column: "CreditCardId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CreditCardId_TransactionDate",
                table: "Transactions",
                columns: new[] { "CreditCardId", "TransactionDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Milestones");

            migrationBuilder.DropTable(
                name: "RewardCaps");

            migrationBuilder.DropTable(
                name: "RewardCategories");

            migrationBuilder.DropTable(
                name: "RewardOffers");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "CreditCards");
        }
    }
}
