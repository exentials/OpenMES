using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenMES.Data.SqlServer.DataMigrations
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MachineId = table.Column<int>(type: "int", nullable: false),
                    ProductionOrderPhaseId = table.Column<int>(type: "int", nullable: false),
                    PlacedByOperatorId = table.Column<int>(type: "int", nullable: false),
                    PlacedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UnplacedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
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
                        name: "FK_MachinePhasePlacement_ProductionOrderPhase_ProductionOrderPhaseId",
                        column: x => x.ProductionOrderPhaseId,
                        principalTable: "ProductionOrderPhase",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MachinePhasePlacement_MachineId_ProductionOrderPhaseId_UnplacedAt",
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
