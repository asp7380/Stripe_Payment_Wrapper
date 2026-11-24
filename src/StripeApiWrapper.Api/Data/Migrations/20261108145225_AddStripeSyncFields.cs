using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StripeApiWrapper.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeSyncFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StripeSyncedAt",
                table: "Customers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "Addresses",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StripeSyncedAt",
                table: "Addresses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_StripeCustomerId",
                table: "Addresses",
                column: "StripeCustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Addresses_StripeCustomerId",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "StripeSyncedAt",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "StripeSyncedAt",
                table: "Addresses");
        }
    }
}
