using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AjoutContreVisite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ControleInitialId",
                table: "Controle",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Controle_ControleInitialId",
                table: "Controle",
                column: "ControleInitialId",
                unique: true,
                filter: "[ControleInitialId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Controle_Controle_ControleInitialId",
                table: "Controle",
                column: "ControleInitialId",
                principalTable: "Controle",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Controle_Controle_ControleInitialId",
                table: "Controle");

            migrationBuilder.DropIndex(
                name: "IX_Controle_ControleInitialId",
                table: "Controle");

            migrationBuilder.DropColumn(
                name: "ControleInitialId",
                table: "Controle");
        }
    }
}
