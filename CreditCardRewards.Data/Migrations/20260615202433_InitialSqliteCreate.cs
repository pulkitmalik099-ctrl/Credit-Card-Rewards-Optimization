using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreditCardRewards.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqliteCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreditCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Issuer = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TotalLimit = table.Column<decimal>(type: "TEXT", nullable: false),
                    JoiningFee = table.Column<decimal>(type: "TEXT", nullable: false),
                    AnnualFee = table.Column<decimal>(type: "TEXT", nullable: false),
                    AnnualFeeWaiverSpendThreshold = table.Column<decimal>(type: "TEXT", nullable: false),
                    AccumulatedSpend = table.Column<decimal>(type: "TEXT", nullable: false),
                    AccumulatedRewardPoints = table.Column<decimal>(type: "TEXT", nullable: false),
                    BaseRewardRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    BaseRewardUnit = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    BaseRewardPointValue = table.Column<decimal>(type: "TEXT", nullable: true),
                    TransferPartners = table.Column<string>(type: "TEXT", nullable: true),
                    AirportLoungeBenefits = table.Column<string>(type: "TEXT", nullable: true),
                    HotelBenefits = table.Column<string>(type: "TEXT", nullable: true),
                    TravelBenefits = table.Column<string>(type: "TEXT", nullable: true),
                    OtherBenefits = table.Column<string>(type: "TEXT", nullable: true),
                    WelcomeOffer = table.Column<string>(type: "TEXT", nullable: true),
                    WelcomeOfferValue = table.Column<decimal>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DataSource = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    UserProfileId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditCards_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Milestones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreditCardId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SpendThreshold = table.Column<decimal>(type: "TEXT", nullable: false),
                    RewardValue = table.Column<decimal>(type: "TEXT", nullable: false),
                    RewardUnit = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsAutomatic = table.Column<bool>(type: "INTEGER", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EffectiveUntil = table.Column<DateTime>(type: "TEXT", nullable: true)
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
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreditCardId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CapType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MaxRewardValue = table.Column<decimal>(type: "TEXT", nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EffectiveUntil = table.Column<DateTime>(type: "TEXT", nullable: true)
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
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreditCardId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RewardMultiplier = table.Column<decimal>(type: "TEXT", nullable: false),
                    Cap = table.Column<decimal>(type: "TEXT", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EffectiveUntil = table.Column<DateTime>(type: "TEXT", nullable: true)
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
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreditCardId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Merchant = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RewardMultiplier = table.Column<decimal>(type: "TEXT", nullable: false),
                    RewardBasis = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FlatBonusAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    RewardUnit = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    MaxRewardPerTransaction = table.Column<decimal>(type: "TEXT", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true)
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
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreditCardId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Merchant = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    PointsEarned = table.Column<decimal>(type: "TEXT", nullable: false),
                    CashbackEarned = table.Column<decimal>(type: "TEXT", nullable: false),
                    RewardValueInRupees = table.Column<decimal>(type: "TEXT", nullable: false),
                    EffectiveReturnPercentage = table.Column<decimal>(type: "TEXT", nullable: false),
                    ContributedToMilestone = table.Column<bool>(type: "INTEGER", nullable: false),
                    MilestoneId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ContributedToFeeWaiver = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                name: "IX_CreditCards_UserProfileId",
                table: "CreditCards",
                column: "UserProfileId");

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

            migrationBuilder.DropTable(
                name: "UserProfiles");
        }
    }
}
