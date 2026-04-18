using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OpenMES.Data.Contexts;
using OpenMES.Data.Entities;
using OpenMES.Data.Common;

namespace OpenMES.Tests;

/// <summary>
/// Creates a fresh in-memory DbContext for each test.
/// </summary>
public static class TestDbFactory
{
	public static OpenMESDbContext Create(string? dbName = null)
	{
		var options = new DbContextOptionsBuilder<OpenMESDbContext>()
			.UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
			.Options;
		return new OpenMESDbContext(options);
	}

	/// <summary>
	/// Seeds the minimum data needed for work session time allocation tests:
	/// - 1 Plant, 1 WorkCenter, 1 Machine (configurable AllowConcurrentSessions + TimeAllocationMode)
	/// - 2 Operators
	/// - 1 Material, 1 ProductionOrder, 1 ProductionOrderPhase (PlannedQty = 100)
	/// </summary>
	public static async Task<(
		OpenMESDbContext db,
		Machine machine,
		Operator op1, Operator op2,
		ProductionOrderPhase phase)> SeedForWorkSessionTests(
			MachineTimeAllocationMode allocationMode = MachineTimeAllocationMode.Uniform,
			bool allowConcurrent = true)
	{
		var db = Create();

		var plant = new Plant { Code = "P1", Description = "Plant 1" };
		db.Plants.Add(plant);

		var wc = new WorkCenter { Code = "WC1", Description = "Work Center 1", Plant = plant };
		db.WorkCenters.Add(wc);

		var machine = new Machine
		{
			Code = "M1", Description = "Machine 1", Type = "CNC",
			Status = MachineStatus.Running,
			WorkCenter = wc,
			AllowConcurrentSessions = allowConcurrent,
			TimeAllocationMode = allocationMode,
		};
		db.Machines.Add(machine);

		var op1 = new Operator { Name = "Operator A", EmployeeNumber = "001", Badge = "B001", Plant = plant };
		var op2 = new Operator { Name = "Operator B", EmployeeNumber = "002", Badge = "B002", Plant = plant };
		db.Operators.AddRange(op1, op2);

		var material = new Material
		{
			PartNumber = "PART001", PartDescription = "Test Part",
			UnitOfMeasure = "pcs"
		};
		db.Materials.Add(material);

		var order = new ProductionOrder
		{
			PlantId = plant.Id, Material = material,
			OrderNumber = "ORD001", OrderType = "STD",
			PlannedQuantity = 100
		};
		db.ProductionOrders.Add(order);

		var phase = new ProductionOrderPhase
		{
			ProductionOrder = order, PhaseNumber = "0010",
			WorkCenter = wc, ExternalId = "EXT-0010",
			PlannedQuantity = 100, Status = OrderStatus.Released,
			WorkCode = "TURN", Description = "Turning"
		};
		db.ProductionOrderPhases.Add(phase);

		await db.SaveChangesAsync();
		return (db, machine, op1, op2, phase);
	}

	public static async Task<(
		OpenMESDbContext db,
		Machine machine,
		Operator op1,
		ProductionOrderPhase phase,
		Material material,
		StorageLocation storageLocation)> SeedForProductionDeclarationTests()
	{
		var db = Create();
		var now = DateTimeOffset.UtcNow;

		var plant = new Plant
		{
			Code = "P1",
			Description = "Plant 1",
			CreatedAt = now,
			UpdatedAt = now,
		};
		db.Plants.Add(plant);

		var wc = new WorkCenter
		{
			Code = "WC1",
			Description = "Work Center 1",
			Plant = plant,
			CreatedAt = now,
			UpdatedAt = now,
		};
		db.WorkCenters.Add(wc);

		var machine = new Machine
		{
			Code = "M1",
			Description = "Machine 1",
			Type = "CNC",
			Status = MachineStatus.Running,
			WorkCenter = wc,
			AllowConcurrentSessions = true,
			TimeAllocationMode = MachineTimeAllocationMode.Uniform,
			CreatedAt = now,
			UpdatedAt = now,
		};
		db.Machines.Add(machine);

		var op1 = new Operator
		{
			Name = "Operator A",
			EmployeeNumber = "001",
			Badge = "B001",
			Plant = plant,
			CreatedAt = now,
			UpdatedAt = now,
		};
		db.Operators.Add(op1);

		var material = new Material
		{
			PartNumber = "PART-A",
			PartDescription = "Part A",
			UnitOfMeasure = "pcs",
			CreatedAt = now,
			UpdatedAt = now,
		};
		db.Materials.Add(material);

		var order = new ProductionOrder
		{
			Plant = plant,
			Material = material,
			OrderNumber = "ORD001",
			OrderType = "STD",
			PlannedQuantity = 100,
			CreatedAt = now,
			UpdatedAt = now,
		};
		db.ProductionOrders.Add(order);

		var phase = new ProductionOrderPhase
		{
			ProductionOrder = order,
			PhaseNumber = "0010",
			WorkCenter = wc,
			ExternalId = "EXT-0010",
			PlannedQuantity = 100,
			Status = OrderStatus.Released,
			WorkCode = "TURN",
			Description = "Turning",
			StartDate = now,
			EndDate = now.AddDays(1),
			CreatedAt = now,
			UpdatedAt = now,
		};
		db.ProductionOrderPhases.Add(phase);

		// Create warehouse first
		var warehouse = new Warehouse
		{
			PlantId = plant.Id,
			Code = "WH1",
			Description = "Warehouse 1",
			Disabled = false,
			CreatedAt = now,
			UpdatedAt = now,
		};
		db.Warehouses.Add(warehouse);
		await db.SaveChangesAsync();

		var storageLocation = new StorageLocation
		{
			WarehouseId = warehouse.Id,
			Code = "SL01",
			Description = "Storage Location 01",
			Zone = "A",
			Slot = "01",
			Disabled = false,
			CreatedAt = now,
			UpdatedAt = now,
		};
		db.StorageLocations.Add(storageLocation);

		var stock = new MaterialStock
		{
			Material = material,
			StorageLocation = storageLocation,
			Quantity = 500,
			LastMovementDate = now,
		};
		db.MaterialStocks.Add(stock);

		db.OperatorShifts.Add(new OperatorShift
		{
			Operator = op1,
			EventType = OperatorEventType.CheckIn,
			EventTime = now,
			Source = "Test",
			CreatedAt = now,
			UpdatedAt = now,
		});

		await db.SaveChangesAsync();
		return (db, machine, op1, phase, material, storageLocation);
	}

	public static Task<(
		OpenMESDbContext db,
		Machine machine,
		Operator op1,
		ProductionOrderPhase phase,
		Material material,
		StorageLocation storageLocation)> SeedForErpExportTests()
		=> SeedForProductionDeclarationTests();
}
