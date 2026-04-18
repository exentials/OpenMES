using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenMES.Data.SqlServer.DataMigrations
{
    /// <inheritdoc />
    public partial class INIT : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InspectionPlan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PartNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Disabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionPlan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MachineStopReason",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Category = table.Column<byte>(type: "tinyint", nullable: false),
                    Disabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineStopReason", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Plant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plant", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InspectionPoint",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InspectionPlanId = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    MeasureType = table.Column<byte>(type: "tinyint", nullable: false),
                    NominalValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    UpperTolerance = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    LowerTolerance = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionPoint", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspectionPoint_InspectionPlan_InspectionPlanId",
                        column: x => x.InspectionPlanId,
                        principalTable: "InspectionPlan",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ClientDevice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AuthToken = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientDevice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientDevice_Plant_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plant",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Operator",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Badge = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Disabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Operator", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Operator_Plant_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plant",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Warehouse",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Disabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouse", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Warehouse_Plant_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plant",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkCenter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkCenter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkCenter_Plant_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plant",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OperatorShift",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperatorId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<byte>(type: "tinyint", nullable: false),
                    EventTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorShift", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperatorShift_Operator_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Operator",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StorageLocation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Zone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Slot = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Disabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageLocation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorageLocation_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouse",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Machine",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkCenterId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Autoplacement = table.Column<bool>(type: "bit", nullable: false),
                    AllowConcurrentSessions = table.Column<bool>(type: "bit", nullable: false),
                    TimeAllocationMode = table.Column<byte>(type: "tinyint", nullable: false),
                    ClientDeviceId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Machine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Machine_ClientDevice_ClientDeviceId",
                        column: x => x.ClientDeviceId,
                        principalTable: "ClientDevice",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Machine_WorkCenter_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenter",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Material",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PartDescription = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PartType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PartGroup = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    IsConsumable = table.Column<bool>(type: "bit", nullable: false),
                    IsPhantom = table.Column<bool>(type: "bit", nullable: false),
                    AllowOverproduction = table.Column<bool>(type: "bit", nullable: false),
                    DefaultWarehouseId = table.Column<int>(type: "int", nullable: true),
                    DefaultStorageLocationId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Material", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Material_StorageLocation_DefaultStorageLocationId",
                        column: x => x.DefaultStorageLocationId,
                        principalTable: "StorageLocation",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Material_Warehouse_DefaultWarehouseId",
                        column: x => x.DefaultWarehouseId,
                        principalTable: "Warehouse",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ClientMachines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientDeviceId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    MachineId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientMachines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientMachines_ClientDevice_ClientDeviceId",
                        column: x => x.ClientDeviceId,
                        principalTable: "ClientDevice",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClientMachines_Machine_MachineId",
                        column: x => x.MachineId,
                        principalTable: "Machine",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MachineState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MachineId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    EventTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OperatorId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineState", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MachineState_Machine_MachineId",
                        column: x => x.MachineId,
                        principalTable: "Machine",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MachineState_Operator_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Operator",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MaterialStock",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    StorageLocationId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    LastMovementDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialStock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialStock_Material_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Material",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialStock_StorageLocation_StorageLocationId",
                        column: x => x.StorageLocationId,
                        principalTable: "StorageLocation",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrder",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlantId = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    OrderNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OrderType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PlannedQuantity = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    ConfirmedQuantity = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    ScrapQuantity = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrder_Material_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Material",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionOrder_Plant_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plant",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StockMovement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    StorageLocationId = table.Column<int>(type: "int", nullable: false),
                    MovementType = table.Column<byte>(type: "tinyint", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    MovementDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReferenceId = table.Column<int>(type: "int", nullable: true),
                    ReferenceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OperatorId = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovement_Material_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Material",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMovement_Operator_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Operator",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMovement_StorageLocation_StorageLocationId",
                        column: x => x.StorageLocationId,
                        principalTable: "StorageLocation",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrderPhase",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: false),
                    PhaseNumber = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    WorkCenterId = table.Column<int>(type: "int", nullable: false),
                    WorkCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PlannedQuantity = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    CounterQuantity = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    ConfirmedQuantity = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    ScrapQuantity = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrderPhase", x => x.Id);
                    table.UniqueConstraint("AK_ProductionOrderPhase_ExternalId", x => x.ExternalId);
                    table.ForeignKey(
                        name: "FK_ProductionOrderPhase_ProductionOrder_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrder",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionOrderPhase_WorkCenter_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenter",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InspectionReading",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InspectionPointId = table.Column<int>(type: "int", nullable: false),
                    ProductionOrderPhaseId = table.Column<int>(type: "int", nullable: false),
                    OperatorId = table.Column<int>(type: "int", nullable: false),
                    ReadingDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    NumericValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    BooleanValue = table.Column<bool>(type: "bit", nullable: true),
                    TextValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Result = table.Column<byte>(type: "tinyint", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionReading", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspectionReading_InspectionPoint_InspectionPointId",
                        column: x => x.InspectionPointId,
                        principalTable: "InspectionPoint",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InspectionReading_Operator_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Operator",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InspectionReading_ProductionOrderPhase_ProductionOrderPhaseId",
                        column: x => x.ProductionOrderPhaseId,
                        principalTable: "ProductionOrderPhase",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MachineStop",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MachineId = table.Column<int>(type: "int", nullable: false),
                    ProductionOrderPhaseId = table.Column<int>(type: "int", nullable: true),
                    MachineStopReasonId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineStop", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MachineStop_MachineStopReason_MachineStopReasonId",
                        column: x => x.MachineStopReasonId,
                        principalTable: "MachineStopReason",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MachineStop_Machine_MachineId",
                        column: x => x.MachineId,
                        principalTable: "Machine",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MachineStop_ProductionOrderPhase_ProductionOrderPhaseId",
                        column: x => x.ProductionOrderPhaseId,
                        principalTable: "ProductionOrderPhase",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PhasePickingList",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionOrderPhaseId = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    StorageLocationId = table.Column<int>(type: "int", nullable: true),
                    RequiredQuantity = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    PickedQuantity = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    IsAutomatic = table.Column<bool>(type: "bit", nullable: false),
                    IsConsumable = table.Column<bool>(type: "bit", nullable: false),
                    IsPhantom = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhasePickingList", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhasePickingList_Material_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Material",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PhasePickingList_ProductionOrderPhase_ProductionOrderPhaseId",
                        column: x => x.ProductionOrderPhaseId,
                        principalTable: "ProductionOrderPhase",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PhasePickingList_StorageLocation_StorageLocationId",
                        column: x => x.StorageLocationId,
                        principalTable: "StorageLocation",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductionDeclaration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionOrderPhaseId = table.Column<int>(type: "int", nullable: false),
                    OperatorId = table.Column<int>(type: "int", nullable: false),
                    MachineId = table.Column<int>(type: "int", nullable: false),
                    DeclarationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConfirmedQuantity = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    ScrapQuantity = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PhaseExternalId = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ExternalCounterId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ErpExportedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsReversal = table.Column<bool>(type: "bit", nullable: false),
                    ReversalOfId = table.Column<int>(type: "int", nullable: true),
                    ReversedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionDeclaration", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionDeclaration_Machine_MachineId",
                        column: x => x.MachineId,
                        principalTable: "Machine",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionDeclaration_Operator_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Operator",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionDeclaration_ProductionOrderPhase_ProductionOrderPhaseId",
                        column: x => x.ProductionOrderPhaseId,
                        principalTable: "ProductionOrderPhase",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductionJob",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionOrderPhaseId = table.Column<int>(type: "int", nullable: false),
                    JobId = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PlannedQuantity = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    PlannedSetupTime = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    PlannedRunTime = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionJob", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionJob_ProductionOrderPhase_ProductionOrderPhaseId",
                        column: x => x.ProductionOrderPhaseId,
                        principalTable: "ProductionOrderPhase",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkSession",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperatorId = table.Column<int>(type: "int", nullable: false),
                    ProductionOrderPhaseId = table.Column<int>(type: "int", nullable: false),
                    MachineId = table.Column<int>(type: "int", nullable: false),
                    SessionType = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AllocatedMinutes = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PhaseExternalId = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ExternalCounterId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ErpExportedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsReversal = table.Column<bool>(type: "bit", nullable: false),
                    ReversalOfId = table.Column<int>(type: "int", nullable: true),
                    ReversedById = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkSession", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkSession_Machine_MachineId",
                        column: x => x.MachineId,
                        principalTable: "Machine",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkSession_Operator_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Operator",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkSession_ProductionOrderPhase_ProductionOrderPhaseId",
                        column: x => x.ProductionOrderPhaseId,
                        principalTable: "ProductionOrderPhase",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NonConformity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProductionOrderPhaseId = table.Column<int>(type: "int", nullable: false),
                    InspectionReadingId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Type = table.Column<byte>(type: "tinyint", nullable: false),
                    Severity = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    CorrectiveAction = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OpenedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedByOperatorId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NonConformity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NonConformity_InspectionReading_InspectionReadingId",
                        column: x => x.InspectionReadingId,
                        principalTable: "InspectionReading",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NonConformity_Operator_ClosedByOperatorId",
                        column: x => x.ClosedByOperatorId,
                        principalTable: "Operator",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NonConformity_ProductionOrderPhase_ProductionOrderPhaseId",
                        column: x => x.ProductionOrderPhaseId,
                        principalTable: "ProductionOrderPhase",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PhasePickingItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhasePickingListId = table.Column<int>(type: "int", nullable: false),
                    StockMovementId = table.Column<int>(type: "int", nullable: false),
                    PickedQuantity = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    PickedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OperatorId = table.Column<int>(type: "int", nullable: false),
                    IsAutomatic = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhasePickingItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhasePickingItem_Operator_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Operator",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PhasePickingItem_PhasePickingList_PhasePickingListId",
                        column: x => x.PhasePickingListId,
                        principalTable: "PhasePickingList",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PhasePickingItem_StockMovement_StockMovementId",
                        column: x => x.StockMovementId,
                        principalTable: "StockMovement",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientDevice_Name",
                table: "ClientDevice",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientDevice_PlantId",
                table: "ClientDevice",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientMachines_ClientDeviceId",
                table: "ClientMachines",
                column: "ClientDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientMachines_MachineId",
                table: "ClientMachines",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionPlan_Code_Version",
                table: "InspectionPlan",
                columns: new[] { "Code", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InspectionPoint_InspectionPlanId",
                table: "InspectionPoint",
                column: "InspectionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionReading_InspectionPointId",
                table: "InspectionReading",
                column: "InspectionPointId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionReading_OperatorId",
                table: "InspectionReading",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionReading_ProductionOrderPhaseId",
                table: "InspectionReading",
                column: "ProductionOrderPhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Machine_ClientDeviceId",
                table: "Machine",
                column: "ClientDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Machine_Code",
                table: "Machine",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Machine_WorkCenterId",
                table: "Machine",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_MachineState_MachineId",
                table: "MachineState",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_MachineState_OperatorId",
                table: "MachineState",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_MachineStop_MachineId",
                table: "MachineStop",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_MachineStop_MachineStopReasonId",
                table: "MachineStop",
                column: "MachineStopReasonId");

            migrationBuilder.CreateIndex(
                name: "IX_MachineStop_ProductionOrderPhaseId",
                table: "MachineStop",
                column: "ProductionOrderPhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_MachineStopReason_Code",
                table: "MachineStopReason",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Material_DefaultStorageLocationId",
                table: "Material",
                column: "DefaultStorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Material_DefaultWarehouseId",
                table: "Material",
                column: "DefaultWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Material_PartNumber",
                table: "Material",
                column: "PartNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialStock_MaterialId_StorageLocationId",
                table: "MaterialStock",
                columns: new[] { "MaterialId", "StorageLocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialStock_StorageLocationId",
                table: "MaterialStock",
                column: "StorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_NonConformity_ClosedByOperatorId",
                table: "NonConformity",
                column: "ClosedByOperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_NonConformity_Code",
                table: "NonConformity",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NonConformity_InspectionReadingId",
                table: "NonConformity",
                column: "InspectionReadingId",
                unique: true,
                filter: "[InspectionReadingId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NonConformity_ProductionOrderPhaseId",
                table: "NonConformity",
                column: "ProductionOrderPhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Operator_PlantId",
                table: "Operator",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorShift_OperatorId",
                table: "OperatorShift",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_PhasePickingItem_OperatorId",
                table: "PhasePickingItem",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_PhasePickingItem_PhasePickingListId",
                table: "PhasePickingItem",
                column: "PhasePickingListId");

            migrationBuilder.CreateIndex(
                name: "IX_PhasePickingItem_StockMovementId",
                table: "PhasePickingItem",
                column: "StockMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_PhasePickingList_MaterialId",
                table: "PhasePickingList",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_PhasePickingList_ProductionOrderPhaseId",
                table: "PhasePickingList",
                column: "ProductionOrderPhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_PhasePickingList_StorageLocationId",
                table: "PhasePickingList",
                column: "StorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Plant_Code",
                table: "Plant",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionDeclaration_MachineId",
                table: "ProductionDeclaration",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionDeclaration_OperatorId",
                table: "ProductionDeclaration",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionDeclaration_ProductionOrderPhaseId",
                table: "ProductionDeclaration",
                column: "ProductionOrderPhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionJob_ProductionOrderPhaseId",
                table: "ProductionJob",
                column: "ProductionOrderPhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrder_MaterialId",
                table: "ProductionOrder",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrder_OrderNumber",
                table: "ProductionOrder",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrder_PlantId",
                table: "ProductionOrder",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderPhase_ProductionOrderId_PhaseNumber",
                table: "ProductionOrderPhase",
                columns: new[] { "ProductionOrderId", "PhaseNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderPhase_WorkCenterId",
                table: "ProductionOrderPhase",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovement_MaterialId",
                table: "StockMovement",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovement_OperatorId",
                table: "StockMovement",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovement_StorageLocationId",
                table: "StockMovement",
                column: "StorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StorageLocation_Code",
                table: "StorageLocation",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StorageLocation_WarehouseId",
                table: "StorageLocation",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouse_PlantId",
                table: "Warehouse",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenter_Code",
                table: "WorkCenter",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenter_PlantId",
                table: "WorkCenter",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSession_MachineId",
                table: "WorkSession",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSession_OperatorId",
                table: "WorkSession",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSession_ProductionOrderPhaseId",
                table: "WorkSession",
                column: "ProductionOrderPhaseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientMachines");

            migrationBuilder.DropTable(
                name: "MachineState");

            migrationBuilder.DropTable(
                name: "MachineStop");

            migrationBuilder.DropTable(
                name: "MaterialStock");

            migrationBuilder.DropTable(
                name: "NonConformity");

            migrationBuilder.DropTable(
                name: "OperatorShift");

            migrationBuilder.DropTable(
                name: "PhasePickingItem");

            migrationBuilder.DropTable(
                name: "ProductionDeclaration");

            migrationBuilder.DropTable(
                name: "ProductionJob");

            migrationBuilder.DropTable(
                name: "WorkSession");

            migrationBuilder.DropTable(
                name: "MachineStopReason");

            migrationBuilder.DropTable(
                name: "InspectionReading");

            migrationBuilder.DropTable(
                name: "PhasePickingList");

            migrationBuilder.DropTable(
                name: "StockMovement");

            migrationBuilder.DropTable(
                name: "Machine");

            migrationBuilder.DropTable(
                name: "InspectionPoint");

            migrationBuilder.DropTable(
                name: "ProductionOrderPhase");

            migrationBuilder.DropTable(
                name: "Operator");

            migrationBuilder.DropTable(
                name: "ClientDevice");

            migrationBuilder.DropTable(
                name: "InspectionPlan");

            migrationBuilder.DropTable(
                name: "ProductionOrder");

            migrationBuilder.DropTable(
                name: "WorkCenter");

            migrationBuilder.DropTable(
                name: "Material");

            migrationBuilder.DropTable(
                name: "StorageLocation");

            migrationBuilder.DropTable(
                name: "Warehouse");

            migrationBuilder.DropTable(
                name: "Plant");
        }
    }
}
