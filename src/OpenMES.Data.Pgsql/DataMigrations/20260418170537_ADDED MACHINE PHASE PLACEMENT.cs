using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenMES.Data.Pgsql.DataMigrations
{
    /// <inheritdoc />
    public partial class ADDEDMACHINEPHASEPLACEMENT : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MachinePhasePlacement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MachineId = table.Column<int>(type: "integer", nullable: false),
                    ProductionOrderPhaseId = table.Column<int>(type: "integer", nullable: false),
                    PlacedByOperatorId = table.Column<int>(type: "integer", nullable: false),
                    PlacedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UnplacedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<byte>(type: "smallint", nullable: false),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachinePhasePlacement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MachinePhasePlacement_Machine_MachineId",
                        column: x => x.MachineId,
                        principalTable: "Machine",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MachinePhasePlacement_Operator_PlacedByOperatorId",
                        column: x => x.PlacedByOperatorId,
                        principalTable: "Operator",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MachinePhasePlacement_ProductionOrderPhase_ProductionOrderP~",
                        column: x => x.ProductionOrderPhaseId,
                        principalTable: "ProductionOrderPhase",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MachinePhasePlacement_MachineId_ProductionOrderPhaseId_Unpl~",
                table: "MachinePhasePlacement",
                columns: new[] { "MachineId", "ProductionOrderPhaseId", "UnplacedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MachinePhasePlacement_PlacedByOperatorId",
                table: "MachinePhasePlacement",
                column: "PlacedByOperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_MachinePhasePlacement_ProductionOrderPhaseId",
                table: "MachinePhasePlacement",
                column: "ProductionOrderPhaseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MachinePhasePlacement");
        }
    }
}
