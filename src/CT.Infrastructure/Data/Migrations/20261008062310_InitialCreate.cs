using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateTable(
                name: "Proprietaire",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Telephone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Adresse = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proprietaire", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Utilisateur",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NomComplet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MotDePasseHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utilisateur", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PointControle",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategorieId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Gravite = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointControle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PointControle_CategoriePoint_CategorieId",
                        column: x => x.CategorieId,
                        principalTable: "CategoriePoint",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vehicule",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Immatriculation = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NumeroChassis = table.Column<string>(type: "nvarchar(17)", maxLength: 17, nullable: false),
                    Marque = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Modele = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Annee = table.Column<int>(type: "int", nullable: false),
                    TypeVehicule = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Energie = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProprietaireId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vehicule_Proprietaire_ProprietaireId",
                        column: x => x.ProprietaireId,
                        principalTable: "Proprietaire",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Controle",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehiculeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InspecteurId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateControle = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Kilometrage = table.Column<int>(type: "int", nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Resultat = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Observations = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DateFinValidite = table.Column<DateOnly>(type: "date", nullable: true),
                    CreeLe = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClotureLe = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SynchroniseLe = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Controle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Controle_Utilisateur_InspecteurId",
                        column: x => x.InspecteurId,
                        principalTable: "Utilisateur",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Controle_Vehicule_VehiculeId",
                        column: x => x.VehiculeId,
                        principalTable: "Vehicule",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResultatPoint",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ControleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PointControleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Etat = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Commentaire = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultatPoint", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResultatPoint_Controle_ControleId",
                        column: x => x.ControleId,
                        principalTable: "Controle",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResultatPoint_PointControle_PointControleId",
                        column: x => x.PointControleId,
                        principalTable: "PointControle",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoriePoint_Libelle",
                table: "CategoriePoint",
                column: "Libelle",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Controle_DateControle",
                table: "Controle",
                column: "DateControle");

            migrationBuilder.CreateIndex(
                name: "IX_Controle_InspecteurId",
                table: "Controle",
                column: "InspecteurId");

            migrationBuilder.CreateIndex(
                name: "IX_Controle_VehiculeId",
                table: "Controle",
                column: "VehiculeId");

            migrationBuilder.CreateIndex(
                name: "IX_PointControle_CategorieId",
                table: "PointControle",
                column: "CategorieId");

            migrationBuilder.CreateIndex(
                name: "IX_Proprietaire_Nom",
                table: "Proprietaire",
                column: "Nom");

            migrationBuilder.CreateIndex(
                name: "IX_ResultatPoint_ControleId_PointControleId",
                table: "ResultatPoint",
                columns: new[] { "ControleId", "PointControleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResultatPoint_PointControleId",
                table: "ResultatPoint",
                column: "PointControleId");

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateur_Email",
                table: "Utilisateur",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicule_Immatriculation",
                table: "Vehicule",
                column: "Immatriculation",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicule_NumeroChassis",
                table: "Vehicule",
                column: "NumeroChassis",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicule_ProprietaireId",
                table: "Vehicule",
                column: "ProprietaireId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResultatPoint");

            migrationBuilder.DropTable(
                name: "Controle");

            migrationBuilder.DropTable(
                name: "PointControle");

            migrationBuilder.DropTable(
                name: "Utilisateur");

            migrationBuilder.DropTable(
                name: "Vehicule");

            migrationBuilder.DropTable(
                name: "CategoriePoint");

            migrationBuilder.DropTable(
                name: "Proprietaire");
        }
    }
}
