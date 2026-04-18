using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Contexts;
using OpenMES.Data.Entities;
using OpenMES.Data.Helpers;

namespace OpenMES.MigrationService;

internal static class MesDataSeeding
{

	public static async Task SeedDataAsync(DbContext dbContext, CancellationToken cancellationToken)
	{
		//var strategy = dbContext.Database.CreateExecutionStrategy();
		//await strategy.ExecuteAsync(async () =>
		//{
			// Seed the database

			//await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

			var plant = await dbContext.Set<Plant>().OrderBy(t => t.Id).FirstOrDefaultAsync(cancellationToken);
			if (plant is null)
			{
				plant = new Plant
				{
					Code = "P001",
					Description = "Plant 1",
					CreatedAt = DateTimeOffset.UtcNow,
					UpdatedAt = DateTimeOffset.UtcNow
				};
				dbContext.Set<Plant>().Add(plant);
				await dbContext.SaveChangesAsync(cancellationToken);
			}

			if (!await dbContext.Set<Material>().AnyAsync(cancellationToken))
			{
				dbContext.Set<Material>().Add(new Material
				{
					PartNumber = "PN1000",
					PartType = "SEMIFINISHED",
					PartDescription = "PIN",
					UnitOfMeasure = "PCS",
					AllowOverproduction = false
				});
				dbContext.Set<Material>().Add(new Material
				{
					PartNumber = "PN2000",
					PartType = "SEMIFINISHED",
					PartDescription = "GEAR",
					UnitOfMeasure = "PCS",
					AllowOverproduction = false
				});
				dbContext.Set<Material>().Add(new Material
				{
					PartNumber = "PN3000",
					PartType = "RAW",
					PartDescription = "Steel Rod",
					UnitOfMeasure = "KG",
					AllowOverproduction = true
				});
				dbContext.Set<Material>().Add(new Material
				{
					PartNumber = "PN4000",
					PartType = "CONSUMABLE",
					PartDescription = "Cutting Oil",
					UnitOfMeasure = "LT",
					IsConsumable = true,
					AllowOverproduction = true
				});
				dbContext.Set<Material>().Add(new Material
				{
					PartNumber = "PN5000",
					PartType = "PHANTOM",
					PartDescription = "Assembly Group",
					UnitOfMeasure = "PCS",
					IsPhantom = true,
					AllowOverproduction = false
				});
				await dbContext.SaveChangesAsync(cancellationToken);
			}

			if (!await dbContext.Set<WorkCenter>().AnyAsync(cancellationToken))
			{
				var wc1 = new WorkCenter
				{
					Code = "WC001",
					Description = "Turning",
					Plant = plant
				};
				var wc2 = new WorkCenter
				{
					Code = "WC002",
					Description = "Milling",
					Plant = plant
				};
				var wc3 = new WorkCenter
				{
					Code = "WC003",
					Description = "Grinding",
					Plant = plant
				};
				dbContext.Set<WorkCenter>().Add(wc1);
				dbContext.Set<WorkCenter>().Add(wc2);
				dbContext.Set<WorkCenter>().Add(wc3);

				// Machines for WC001
				dbContext.Set<Machine>().Add(new Machine
				{
					Code = "M001",
					Description = "Lathe 1",
					Type = "CNC",
					Status = MachineStatus.Idle,
					WorkCenter = wc1,
					AllowConcurrentSessions = true,
					TimeAllocationMode = MachineTimeAllocationMode.Uniform
				});
				dbContext.Set<Machine>().Add(new Machine
				{
					Code = "M002",
					Description = "Lathe 2",
					Type = "CNC",
					Status = MachineStatus.Idle,
					WorkCenter = wc1,
					AllowConcurrentSessions = false,
					TimeAllocationMode = MachineTimeAllocationMode.Proportional
				});

				// Machines for WC002
				dbContext.Set<Machine>().Add(new Machine
				{
					Code = "M003",
					Description = "Mill 1",
					Type = "CNC",
					Status = MachineStatus.Idle,
					WorkCenter = wc2,
					AllowConcurrentSessions = true,
					TimeAllocationMode = MachineTimeAllocationMode.Uniform
				});

				// Machines for WC003
				dbContext.Set<Machine>().Add(new Machine
				{
					Code = "M004",
					Description = "Grinder 1",
					Type = "CONVENTIONAL",
					Status = MachineStatus.Idle,
					WorkCenter = wc3,
					AllowConcurrentSessions = false,
					TimeAllocationMode = MachineTimeAllocationMode.Proportional
				});
				await dbContext.SaveChangesAsync(cancellationToken);
			}

			if (!await dbContext.Set<MachineStopReason>().AnyAsync(cancellationToken))
			{
				dbContext.Set<MachineStopReason>().AddRange(
					new MachineStopReason
					{
						Code = "BREAKDOWN",
						Description = "Machine Breakdown",
						Category = MachineStopCategory.Breakdown,
						Disabled = false
					},
					new MachineStopReason
					{
						Code = "MAINTENANCE",
						Description = "Preventive Maintenance",
						Category = MachineStopCategory.Maintenance,
						Disabled = false
					},
					new MachineStopReason
					{
						Code = "TOOLCHANGE",
						Description = "Tool Change",
						Category = MachineStopCategory.Setup,
						Disabled = false
					},
					new MachineStopReason
					{
						Code = "MATERIAL_SHORTAGE",
						Description = "Material Not Available",
						Category = MachineStopCategory.MaterialWaiting,
						Disabled = false
					},
					new MachineStopReason
					{
						Code = "OPERATOR_BREAK",
						Description = "Operator Break",
						Category = MachineStopCategory.Organizational,
						Disabled = false
					},
					new MachineStopReason
					{
						Code = "SETUP",
						Description = "Setup & Adjustment",
						Category = MachineStopCategory.Setup,
						Disabled = false
					}
				);
				await dbContext.SaveChangesAsync(cancellationToken);
			}

			if (!await dbContext.Set<ClientDevice>().AnyAsync())
			{
				dbContext.Set<ClientDevice>().Add(new ClientDevice
				{
					Name = "Term1",
					Plant = plant,
					Description = "Terminal 1",
					Password = "123456",
					AuthToken = Helpers.CalculateMD5($"Term1:123456"),
					Machines = dbContext.Set<Machine>().Where(t => t.Code.StartsWith("M")).ToList()
				});
				await dbContext.SaveChangesAsync(cancellationToken);
			}

			if (!await dbContext.Set<Operator>().AnyAsync(cancellationToken))
			{
				dbContext.Set<Operator>().AddRange(
					new Operator
					{
						EmployeeNumber = "OP001",
						Name = "James Bond",
						Badge = "007",
						Plant = plant
					},
					new Operator
					{
						EmployeeNumber = "OP002",
						Name = "Thomas Anderson",
						Badge = "101",
						Plant = plant
					},
					new Operator
					{
						EmployeeNumber = "OP003",
						Name = "Sarah Connor",
						Badge = "102",
						Plant = plant
					},
					new Operator
					{
						EmployeeNumber = "OP004",
						Name = "John Smith",
						Badge = "103",
						Plant = plant
					}
				);
				await dbContext.SaveChangesAsync(cancellationToken);
			}

			if (!await dbContext.Set<Warehouse>().AnyAsync(cancellationToken))
			{
				dbContext.Set<Warehouse>().AddRange(
					new Warehouse
					{
						PlantId = plant.Id,
						Code = "WH-A",
						Description = "Warehouse A - Main Storage",
						Disabled = false,
						CreatedAt = DateTimeOffset.UtcNow,
						UpdatedAt = DateTimeOffset.UtcNow
					},
					new Warehouse
					{
						PlantId = plant.Id,
						Code = "WH-B",
						Description = "Warehouse B - Secondary Storage",
						Disabled = false,
						CreatedAt = DateTimeOffset.UtcNow,
						UpdatedAt = DateTimeOffset.UtcNow
					}
				);
				await dbContext.SaveChangesAsync(cancellationToken);
			}

			if (!await dbContext.Set<StorageLocation>().AnyAsync(cancellationToken))
			{
				var warehouseA = await dbContext.Set<Warehouse>().FirstAsync(x => x.Code == "WH-A", cancellationToken);
				var warehouseB = await dbContext.Set<Warehouse>().FirstAsync(x => x.Code == "WH-B", cancellationToken);

				dbContext.Set<StorageLocation>().AddRange(
					new StorageLocation
					{
						WarehouseId = warehouseA.Id,
						Code = "A-01-01",
						Description = "Warehouse A - Shelf 01 - Position 01",
						Zone = "01",
						Slot = "01",
						Disabled = false,
						CreatedAt = DateTimeOffset.UtcNow,
						UpdatedAt = DateTimeOffset.UtcNow
					},
					new StorageLocation
					{
						WarehouseId = warehouseA.Id,
						Code = "A-01-02",
						Description = "Warehouse A - Shelf 01 - Position 02",
						Zone = "01",
						Slot = "02",
						Disabled = false,
						CreatedAt = DateTimeOffset.UtcNow,
						UpdatedAt = DateTimeOffset.UtcNow
					},
					new StorageLocation
					{
						WarehouseId = warehouseB.Id,
						Code = "B-01-01",
						Description = "Warehouse B - Shelf 01 - Position 01",
						Zone = "01",
						Slot = "01",
						Disabled = false,
						CreatedAt = DateTimeOffset.UtcNow,
						UpdatedAt = DateTimeOffset.UtcNow
					}
				);
				await dbContext.SaveChangesAsync(cancellationToken);
			}

			if (!await dbContext.Set<ProductionOrder>().AnyAsync(cancellationToken))
			{
				var materials = await dbContext.Set<Material>().OrderBy(t => t.PartNumber).ToListAsync(cancellationToken);
				var wcTurning = await dbContext.Set<WorkCenter>().FirstAsync(x => x.Code == "WC001", cancellationToken);
				var wcMilling = await dbContext.Set<WorkCenter>().FirstAsync(x => x.Code == "WC002", cancellationToken);
				var wcGrinding = await dbContext.Set<WorkCenter>().FirstAsync(x => x.Code == "WC003", cancellationToken);

				// Single-phase order (PN1000)
				var order1 = new ProductionOrder
				{
					Plant = plant,
					OrderNumber = "PO1000",
					Material = materials[0],
					OrderType = "PO",
					PlannedQuantity = 100,
					ConfirmedQuantity = 0,
					ScrapQuantity = 0,
					Status = OrderStatus.Released
				};
				order1.ProductionOrderPhases.Add(new ProductionOrderPhase
				{
					PhaseNumber = "0010",
					Description = "Turning",
					WorkCenter = wcTurning,
					PlannedQuantity = 100,
					CounterQuantity = 0,
					ConfirmedQuantity = 0,
					ScrapQuantity = 0,
					Status = OrderStatus.Released,
					ExternalId = "PO1000-0010",
					WorkCode = "TURN",
					StartDate = DateTimeOffset.UtcNow,
					EndDate = DateTimeOffset.UtcNow.AddDays(1)
				});
				dbContext.Set<ProductionOrder>().Add(order1);

				// Multi-phase order (PN2000) - 3 phases for quantity validation testing
				var order2 = new ProductionOrder
				{
					Plant = plant,
					OrderNumber = "PO2000",
					Material = materials[1],
					OrderType = "PO",
					PlannedQuantity = 150,
					ConfirmedQuantity = 0,
					ScrapQuantity = 0,
					Status = OrderStatus.Released
				};
				order2.ProductionOrderPhases.Add(new ProductionOrderPhase
				{
					PhaseNumber = "0010",
					Description = "Turning - Rough",
					WorkCenter = wcTurning,
					PlannedQuantity = 150,
					CounterQuantity = 0,
					ConfirmedQuantity = 0,
					ScrapQuantity = 0,
					Status = OrderStatus.Released,
					ExternalId = "PO2000-0010",
					WorkCode = "TURN-R",
					StartDate = DateTimeOffset.UtcNow,
					EndDate = DateTimeOffset.UtcNow.AddDays(1)
				});
				order2.ProductionOrderPhases.Add(new ProductionOrderPhase
				{
					PhaseNumber = "0020",
					Description = "Milling",
					WorkCenter = wcMilling,
					PlannedQuantity = 150,
					CounterQuantity = 0,
					ConfirmedQuantity = 0,
					ScrapQuantity = 0,
					Status = OrderStatus.Released,
					ExternalId = "PO2000-0020",
					WorkCode = "MILL",
					StartDate = DateTimeOffset.UtcNow.AddDays(1),
					EndDate = DateTimeOffset.UtcNow.AddDays(2)
				});
				order2.ProductionOrderPhases.Add(new ProductionOrderPhase
				{
					PhaseNumber = "0030",
					Description = "Grinding - Finish",
					WorkCenter = wcGrinding,
					PlannedQuantity = 150,
					CounterQuantity = 0,
					ConfirmedQuantity = 0,
					ScrapQuantity = 0,
					Status = OrderStatus.Released,
					ExternalId = "PO2000-0030",
					WorkCode = "GRIND-F",
					StartDate = DateTimeOffset.UtcNow.AddDays(2),
					EndDate = DateTimeOffset.UtcNow.AddDays(3)
				});
				dbContext.Set<ProductionOrder>().Add(order2);

				// Two-phase order with AllowOverproduction material (PN3000)
				var order3 = new ProductionOrder
				{
					Plant = plant,
					OrderNumber = "PO3000",
					Material = materials[2],
					OrderType = "PO",
					PlannedQuantity = 500,
					ConfirmedQuantity = 0,
					ScrapQuantity = 0,
					Status = OrderStatus.Released
				};
				order3.ProductionOrderPhases.Add(new ProductionOrderPhase
				{
					PhaseNumber = "0010",
					Description = "Rough Cutting",
					WorkCenter = wcTurning,
					PlannedQuantity = 500,
					CounterQuantity = 0,
					ConfirmedQuantity = 0,
					ScrapQuantity = 0,
					Status = OrderStatus.Released,
					ExternalId = "PO3000-0010",
					WorkCode = "CUT-R",
					StartDate = DateTimeOffset.UtcNow,
					EndDate = DateTimeOffset.UtcNow.AddDays(1)
				});
				order3.ProductionOrderPhases.Add(new ProductionOrderPhase
				{
					PhaseNumber = "0020",
					Description = "Finish Cutting",
					WorkCenter = wcMilling,
					PlannedQuantity = 500,
					CounterQuantity = 0,
					ConfirmedQuantity = 0,
					ScrapQuantity = 0,
					Status = OrderStatus.Released,
					ExternalId = "PO3000-0020",
					WorkCode = "CUT-F",
					StartDate = DateTimeOffset.UtcNow.AddDays(1),
					EndDate = DateTimeOffset.UtcNow.AddDays(2)
				});
				dbContext.Set<ProductionOrder>().Add(order3);

				// Additional orders to exercise the phase picker in WebClient
				// (multiple phases on the same WorkCenter visible simultaneously)

				// PO4000 — PIN variant, 3 phases all on Turning (WC001) → picker on M001/M002
				var order4 = new ProductionOrder
				{
					Plant = plant,
					OrderNumber = "PO4000",
					Material = materials[0],
					OrderType = "PO",
					PlannedQuantity = 200,
					Status = OrderStatus.Released
				};
				order4.ProductionOrderPhases.Add(new ProductionOrderPhase
				{
					PhaseNumber = "0010", Description = "Facing",
					WorkCenter = wcTurning, PlannedQuantity = 200,
					Status = OrderStatus.Released, ExternalId = "PO4000-0010",
					WorkCode = "FACE",
					StartDate = DateTimeOffset.UtcNow, EndDate = DateTimeOffset.UtcNow.AddDays(1)
				});
				order4.ProductionOrderPhases.Add(new ProductionOrderPhase
				{
					PhaseNumber = "0020", Description = "Rough Turn",
					WorkCenter = wcTurning, PlannedQuantity = 200,
					Status = OrderStatus.Released, ExternalId = "PO4000-0020",
					WorkCode = "TURN-R",
					StartDate = DateTimeOffset.UtcNow.AddDays(1), EndDate = DateTimeOffset.UtcNow.AddDays(2)
				});
				order4.ProductionOrderPhases.Add(new ProductionOrderPhase
				{
					PhaseNumber = "0030", Description = "Finish Turn",
					WorkCenter = wcTurning, PlannedQuantity = 200,
					Status = OrderStatus.Released, ExternalId = "PO4000-0030",
					WorkCode = "TURN-F",
					StartDate = DateTimeOffset.UtcNow.AddDays(2), EndDate = DateTimeOffset.UtcNow.AddDays(3)
				});
				dbContext.Set<ProductionOrder>().Add(order4);

				// PO5000 — GEAR variant, 2 phases on Milling (WC002) → picker on M003
				var order5 = new ProductionOrder
				{
					Plant = plant,
					OrderNumber = "PO5000",
					Material = materials[1],
					OrderType = "PO",
					PlannedQuantity = 80,
					Status = OrderStatus.Released
				};
				order5.ProductionOrderPhases.Add(new ProductionOrderPhase
				{
					PhaseNumber = "0010", Description = "Profile Mill",
					WorkCenter = wcMilling, PlannedQuantity = 80,
					Status = OrderStatus.Released, ExternalId = "PO5000-0010",
					WorkCode = "MILL-P",
					StartDate = DateTimeOffset.UtcNow, EndDate = DateTimeOffset.UtcNow.AddDays(1)
				});
				order5.ProductionOrderPhases.Add(new ProductionOrderPhase
				{
					PhaseNumber = "0020", Description = "Slot Mill",
					WorkCenter = wcMilling, PlannedQuantity = 80,
					Status = OrderStatus.Released, ExternalId = "PO5000-0020",
					WorkCode = "MILL-S",
					StartDate = DateTimeOffset.UtcNow.AddDays(1), EndDate = DateTimeOffset.UtcNow.AddDays(2)
				});
				dbContext.Set<ProductionOrder>().Add(order5);

				// PO6000 — Steel Rod, 2 phases on Grinding (WC003) → picker on M004
				var order6 = new ProductionOrder
				{
					Plant = plant,
					OrderNumber = "PO6000",
					Material = materials[2],
					OrderType = "PO",
					PlannedQuantity = 300,
					Status = OrderStatus.Released
				};
				order6.ProductionOrderPhases.Add(new ProductionOrderPhase
				{
					PhaseNumber = "0010", Description = "Rough Grind",
					WorkCenter = wcGrinding, PlannedQuantity = 300,
					Status = OrderStatus.Released, ExternalId = "PO6000-0010",
					WorkCode = "GRIND-R",
					StartDate = DateTimeOffset.UtcNow, EndDate = DateTimeOffset.UtcNow.AddDays(1)
				});
				order6.ProductionOrderPhases.Add(new ProductionOrderPhase
				{
					PhaseNumber = "0020", Description = "Finish Grind",
					WorkCenter = wcGrinding, PlannedQuantity = 300,
					Status = OrderStatus.Released, ExternalId = "PO6000-0020",
					WorkCode = "GRIND-F",
					StartDate = DateTimeOffset.UtcNow.AddDays(1), EndDate = DateTimeOffset.UtcNow.AddDays(2)
				});
				dbContext.Set<ProductionOrder>().Add(order6);

				await dbContext.SaveChangesAsync(cancellationToken);
			}

			await dbContext.SaveChangesAsync(cancellationToken);
			//await transaction.CommitAsync(cancellationToken);

		//});
	}
}