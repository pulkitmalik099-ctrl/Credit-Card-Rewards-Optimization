using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreditCardRewards.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordHashToUserProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "UserProfiles",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "UserProfiles");
        }
    }
}
