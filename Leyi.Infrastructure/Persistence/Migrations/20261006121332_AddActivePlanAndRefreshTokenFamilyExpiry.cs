using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leyi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddActivePlanAndRefreshTokenFamilyExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Added nullable, backfilled, then made NOT NULL: a token issued before family expiry existed is treated as
            // a family that ends when the token itself expires (no row gets a fake 0001-01-01 expiry).
            migrationBuilder.AddColumn<DateTime>(
                name: "FamilyExpiresAt",
                table: "RefreshTokens",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("UPDATE [RefreshTokens] SET [FamilyExpiresAt] = [ExpiresAt] WHERE [FamilyExpiresAt] IS NULL;");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FamilyExpiresAt",
                table: "RefreshTokens",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_UserLearningPlans_User_Active",
                table: "UserLearningPlans",
                column: "UserId",
                unique: true,
                filter: "[Status] = 'ACTIVE'");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_FamilyExpiresAt",
                table: "RefreshTokens",
                column: "FamilyExpiresAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_UserLearningPlans_User_Active",
                table: "UserLearningPlans");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_FamilyExpiresAt",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "FamilyExpiresAt",
                table: "RefreshTokens");
        }
    }
}
