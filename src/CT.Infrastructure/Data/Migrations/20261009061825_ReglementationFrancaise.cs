using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReglementationFrancaise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Les données de démonstration de l'ancien modèle ne sont pas convertibles : elles sont régénérées par le seed.
            migrationBuilder.Sql("UPDATE Controle SET ControleInitialId = NULL");
            migrationBuilder.Sql("DELETE FROM ResultatPoint");
            migrationBuilder.Sql("DELETE FROM Controle");
            migrationBuilder.Sql("DELETE FROM Vehicule");
            migrationBuilder.Sql("DELETE FROM Proprietaire");
            migrationBuilder.Sql("DELETE FROM PointControle");

            migrationBuilder.DropForeignKey(
                name: "FK_PointControle_CategoriePoint_CategorieId",
                table: "PointControle");

            migrationBuilder.DropTable(
                name: "CategoriePoint");

            migrationBuilder.DropColumn(
                name: "Annee",
                table: "Vehicule");

            migrationBuilder.DropColumn(
                name: "Gravite",
                table: "PointControle");

            migrationBuilder.DropColumn(
                name: "Observations",
                table: "Controle");

            migrationBuilder.RenameColumn(
                name: "CategorieId",
                table: "PointControle",
                newName: "FonctionId");

            migrationBuilder.RenameIndex(
                name: "IX_PointControle_CategorieId",
                table: "PointControle",
                newName: "IX_PointControle_FonctionId");

            migrationBuilder.AddColumn<DateOnly>(
                name: "DatePremiereImmatriculation",
                table: "Vehicule",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "PointControle",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateControlePeriodique",
                table: "Controle",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateLimiteContreVisite",
                table: "Controle",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Defaillance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PointControleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Niveau = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Defaillance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Defaillance_PointControle_PointControleId",
                        column: x => x.PointControleId,
                        principalTable: "PointControle",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Fonction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fonction", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DefaillanceConstatee",
                columns: table => new
                {
                    DefaillanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResultatPointId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefaillanceConstatee", x => new { x.DefaillanceId, x.ResultatPointId });
                    table.ForeignKey(
                        name: "FK_DefaillanceConstatee_Defaillance_DefaillanceId",
                        column: x => x.DefaillanceId,
                        principalTable: "Defaillance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefaillanceConstatee_ResultatPoint_ResultatPointId",
                        column: x => x.ResultatPointId,
                        principalTable: "ResultatPoint",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PointControle_Code",
                table: "PointControle",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Defaillance_Code",
                table: "Defaillance",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Defaillance_PointControleId",
                table: "Defaillance",
                column: "PointControleId");

            migrationBuilder.CreateIndex(
                name: "IX_DefaillanceConstatee_ResultatPointId",
                table: "DefaillanceConstatee",
                column: "ResultatPointId");

            migrationBuilder.CreateIndex(
                name: "IX_Fonction_Numero",
                table: "Fonction",
                column: "Numero",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PointControle_Fonction_FonctionId",
                table: "PointControle",
                column: "FonctionId",
                principalTable: "Fonction",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PointControle_Fonction_FonctionId",
                table: "PointControle");

            migrationBuilder.DropTable(
                name: "DefaillanceConstatee");

            migrationBuilder.DropTable(
                name: "Fonction");

            migrationBuilder.DropTable(
                name: "Defaillance");

            migrationBuilder.DropIndex(
                name: "IX_PointControle_Code",
                table: "PointControle");

            migrationBuilder.DropColumn(
                name: "DatePremiereImmatriculation",
                table: "Vehicule");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "PointControle");

            migrationBuilder.DropColumn(
                name: "DateControlePeriodique",
                table: "Controle");

            migrationBuilder.DropColumn(
                name: "DateLimiteContreVisite",
                table: "Controle");

            migrationBuilder.RenameColumn(
                name: "FonctionId",
                table: "PointControle",
                newName: "CategorieId");

            migrationBuilder.RenameIndex(
                name: "IX_PointControle_FonctionId",
                table: "PointControle",
                newName: "IX_PointControle_CategorieId");

            migrationBuilder.AddColumn<int>(
                name: "Annee",
                table: "Vehicule",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Gravite",
                table: "PointControle",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Observations",
                table: "Controle",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CategoriePoint",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriePoint", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoriePoint_Libelle",
                table: "CategoriePoint",
                column: "Libelle",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PointControle_CategoriePoint_CategorieId",
                table: "PointControle",
                column: "CategorieId",
                principalTable: "CategoriePoint",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
