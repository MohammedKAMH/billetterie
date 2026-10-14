using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Billetterie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjoutExpirationReservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateExpiration",
                table: "Reservations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddCheckConstraint(
                name: "CK_TypeBillet_QuantiteVendue_Positive",
                table: "TypeBillets",
                sql: "\"QuantiteVendue\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_StatutPaiement_DateExpiration",
                table: "Reservations",
                columns: new[] { "StatutPaiement", "DateExpiration" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TypeBillet_QuantiteVendue_Positive",
                table: "TypeBillets");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_StatutPaiement_DateExpiration",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "DateExpiration",
                table: "Reservations");
        }
    }
}
