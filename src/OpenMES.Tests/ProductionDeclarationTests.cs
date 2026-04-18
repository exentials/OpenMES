using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OpenMES.Data.Common;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;
using OpenMES.WebApi;
using OpenMES.WebApi.Controllers;

namespace OpenMES.Tests;

public class ProductionDeclarationTests
{
	private static ProductionDeclarationController Controller(OpenMESDbContext db)
		=> new(db, NullLogger<ProductionDeclarationController>.Instance);

	private static ProductionDeclarationDto DeclDto(
		int phaseId, int opId, int machineId,
		decimal confirmed = 10, decimal scrap = 0)
		=> new()
		{
			ProductionOrderPhaseId = phaseId,
			OperatorId             = opId,
			MachineId              = machineId,
			DeclarationDate        = DateTimeOffset.UtcNow,
			ConfirmedQuantity      = confirmed,
			ScrapQuantity          = scrap,
		};

	[Fact]
	public async Task Create_UpdatesPhaseConfirmedQuantity()
	{
		var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		await Controller(db).Create(DeclDto(phase.Id, op1.Id, machine.Id, confirmed: 10), CancellationToken.None);

		var updatedPhase = await db.ProductionOrderPhases.FindAsync(phase.Id);
		Assert.NotNull(updatedPhase);
		Assert.Equal(10m, updatedPhase!.ConfirmedQuantity);
	}

	[Fact]
	public async Task Create_UpdatesPhaseScrapQuantity()
	{
		var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		await Controller(db).Create(DeclDto(phase.Id, op1.Id, machine.Id, confirmed: 5, scrap: 2), CancellationToken.None);

		var updatedPhase = await db.ProductionOrderPhases.FindAsync(phase.Id);
		Assert.NotNull(updatedPhase);
		Assert.Equal(2m, updatedPhase!.ScrapQuantity);
	}

	[Fact]
	public async Task Create_TwoDeclarations_AggregatesAreSummed()
	{
		var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		var controller = Controller(db);

		await controller.Create(DeclDto(phase.Id, op1.Id, machine.Id, confirmed: 10), CancellationToken.None);
		await controller.Create(DeclDto(phase.Id, op1.Id, machine.Id, confirmed: 15), CancellationToken.None);

		var updatedPhase = await db.ProductionOrderPhases.FindAsync(phase.Id);
		Assert.NotNull(updatedPhase);
		Assert.Equal(25m, updatedPhase!.ConfirmedQuantity);
	}

	[Fact]
	public async Task Create_SnapshotsPhaseExternalId()
	{
		var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		var result = await Controller(db).Create(DeclDto(phase.Id, op1.Id, machine.Id), CancellationToken.None);

		var declarationId = Assert.IsType<int>(result.Value);
		var declaration = await db.ProductionDeclarations.FirstOrDefaultAsync(x => x.Id == declarationId);

		Assert.NotNull(declaration);
		Assert.Equal("EXT-0010", declaration!.PhaseExternalId);
	}

	[Fact]
	public async Task Create_OperatorCheckedOut_ThrowsProblemException()
	{
		var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();

		db.OperatorShifts.Add(new OperatorShift
		{
			OperatorId = op1.Id,
			EventType = OperatorEventType.CheckOut,
			EventTime = DateTimeOffset.UtcNow,
			Source = "Test",
			CreatedAt = DateTimeOffset.UtcNow,
			UpdatedAt = DateTimeOffset.UtcNow,
		});
		await db.SaveChangesAsync();

		var ex = await Assert.ThrowsAsync<ProblemException>(async () =>
			await Controller(db).Create(DeclDto(phase.Id, op1.Id, machine.Id, confirmed: 5), CancellationToken.None));

		Assert.Equal("Operator not present", ex.Error);
	}

	[Fact]
	public async Task Create_OperatorOnBreak_ThrowsProblemException()
	{
		var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();

		db.OperatorShifts.Add(new OperatorShift
		{
			OperatorId = op1.Id,
			EventType = OperatorEventType.BreakStart,
			EventTime = DateTimeOffset.UtcNow,
			Source = "Test",
			CreatedAt = DateTimeOffset.UtcNow,
			UpdatedAt = DateTimeOffset.UtcNow,
		});
		await db.SaveChangesAsync();

		var ex = await Assert.ThrowsAsync<ProblemException>(async () =>
			await Controller(db).Create(DeclDto(phase.Id, op1.Id, machine.Id, confirmed: 5), CancellationToken.None));

		Assert.Equal("Operator on break", ex.Error);
	}

	[Fact]
	public async Task Create_PhaseNotFound_ThrowsProblemException()
	{
		var (db, machine, op1, _, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		var dto = DeclDto(phaseId: 99999, opId: op1.Id, machineId: machine.Id);

		await Assert.ThrowsAsync<ProblemException>(async () =>
			await Controller(db).Create(dto, CancellationToken.None));
	}

	[Fact]
	public async Task GetPendingExport_ReturnsOnlyUnexportedAndNotReversed()
	{
		var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		var now = DateTimeOffset.UtcNow;

		db.ProductionDeclarations.AddRange(
			new ProductionDeclaration
			{
				ProductionOrderPhaseId = phase.Id,
				OperatorId = op1.Id,
				MachineId = machine.Id,
				DeclarationDate = now,
				ConfirmedQuantity = 10,
				ReversedById = null,
				ExternalCounterId = null,
				CreatedAt = now,
				UpdatedAt = now,
			},
			new ProductionDeclaration
			{
				ProductionOrderPhaseId = phase.Id,
				OperatorId = op1.Id,
				MachineId = machine.Id,
				DeclarationDate = now.AddMinutes(1),
				ConfirmedQuantity = 10,
				ReversedById = null,
				ExternalCounterId = "ERP1",
				CreatedAt = now,
				UpdatedAt = now,
			},
			new ProductionDeclaration
			{
				ProductionOrderPhaseId = phase.Id,
				OperatorId = op1.Id,
				MachineId = machine.Id,
				DeclarationDate = now.AddMinutes(2),
				ConfirmedQuantity = 10,
				ReversedById = 1,
				ExternalCounterId = null,
				CreatedAt = now,
				UpdatedAt = now,
			});
		await db.SaveChangesAsync();

		var response = await Controller(db).GetPendingExport(CancellationToken.None);
		var ok = Assert.IsType<OkObjectResult>(response.Result);
		var items = Assert.IsAssignableFrom<IEnumerable<ProductionDeclarationDto>>(ok.Value).ToList();

		Assert.Single(items);
		Assert.Null(items[0].ExternalCounterId);
		Assert.Null(items[0].ReversedById);
	}

	[Fact]
	public async Task Correct_NotExported_DeletesOriginalAndCreatesNew()
	{
		var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		var controller = Controller(db);

		var create = await controller.Create(DeclDto(phase.Id, op1.Id, machine.Id, confirmed: 10), CancellationToken.None);
		var originalId = Assert.IsType<int>(create.Value);

		var corrected = DeclDto(phase.Id, op1.Id, machine.Id, confirmed: 99);
		var correctResult = await controller.Correct(originalId, corrected, CancellationToken.None);
		Assert.IsType<OkObjectResult>(correctResult.Result);

		var original = await db.ProductionDeclarations.FindAsync(originalId);
		var replacement = await db.ProductionDeclarations.FirstOrDefaultAsync(x => x.Id != originalId && !x.IsReversal && x.ConfirmedQuantity == 99);
		var updatedPhase = await db.ProductionOrderPhases.FindAsync(phase.Id);

		Assert.Null(original);
		Assert.NotNull(replacement);
		Assert.NotNull(updatedPhase);
		Assert.Equal(99m, updatedPhase!.ConfirmedQuantity);
	}

	[Fact]
	public async Task Correct_NotExported_PreservesPhaseExternalId()
	{
		var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		var controller = Controller(db);

		var create = await controller.Create(DeclDto(phase.Id, op1.Id, machine.Id, confirmed: 10), CancellationToken.None);
		var originalId = Assert.IsType<int>(create.Value);

		await controller.Correct(originalId, DeclDto(phase.Id, op1.Id, machine.Id, confirmed: 15), CancellationToken.None);

		var replacement = await db.ProductionDeclarations
			.Where(x => x.Id != originalId && !x.IsReversal)
			.OrderByDescending(x => x.Id)
			.FirstOrDefaultAsync();

		Assert.NotNull(replacement);
		Assert.Equal("EXT-0010", replacement!.PhaseExternalId);
	}

	[Fact]
	public async Task Correct_IsReversal_ReturnsBadRequest()
	{
		var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		var now = DateTimeOffset.UtcNow;

		var reversal = new ProductionDeclaration
		{
			ProductionOrderPhaseId = phase.Id,
			OperatorId = op1.Id,
			MachineId = machine.Id,
			DeclarationDate = now,
			ConfirmedQuantity = -10,
			IsReversal = true,
			CreatedAt = now,
			UpdatedAt = now,
		};
		db.ProductionDeclarations.Add(reversal);
		await db.SaveChangesAsync();

		var result = await Controller(db).Correct(reversal.Id, DeclDto(phase.Id, op1.Id, machine.Id), CancellationToken.None);
		Assert.IsType<BadRequestObjectResult>(result.Result);
	}

	[Fact]
	public async Task Correct_AlreadyReversed_ReturnsBadRequest()
	{
		var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		var now = DateTimeOffset.UtcNow;

		var original = new ProductionDeclaration
		{
			ProductionOrderPhaseId = phase.Id,
			OperatorId = op1.Id,
			MachineId = machine.Id,
			DeclarationDate = now,
			ConfirmedQuantity = 10,
			ReversedById = 1,
			CreatedAt = now,
			UpdatedAt = now,
		};
		db.ProductionDeclarations.Add(original);
		await db.SaveChangesAsync();

		var result = await Controller(db).Correct(original.Id, DeclDto(phase.Id, op1.Id, machine.Id), CancellationToken.None);
		Assert.IsType<BadRequestObjectResult>(result.Result);
	}

	[Fact]
	public async Task Correct_NotFound_ReturnsNotFound()
	{
		var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();

		var result = await Controller(db).Correct(99999, DeclDto(phase.Id, op1.Id, machine.Id), CancellationToken.None);
		Assert.IsType<NotFoundResult>(result.Result);
	}

	[Fact]
	public async Task Correct_AlreadyExported_CreatesReversalWithNegatedQuantities()
	{
		var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		var controller = Controller(db);

		var create = await controller.Create(DeclDto(phase.Id, op1.Id, machine.Id, confirmed: 10, scrap: 3), CancellationToken.None);
		var originalId = Assert.IsType<int>(create.Value);

		var original = await db.ProductionDeclarations.FindAsync(originalId);
		Assert.NotNull(original);
		original!.ExternalCounterId = "ERP-001";
		await db.SaveChangesAsync();

		await controller.Correct(originalId, DeclDto(phase.Id, op1.Id, machine.Id, confirmed: 20), CancellationToken.None);

		var reversal = await db.ProductionDeclarations.FirstOrDefaultAsync(x => x.IsReversal && x.ReversalOfId == originalId);
		Assert.NotNull(reversal);
		Assert.Equal(-10m, reversal!.ConfirmedQuantity);
		Assert.Equal(-3m, reversal.ScrapQuantity);
		Assert.Equal(originalId, reversal.ReversalOfId);
	}

	[Fact]
	public async Task Correct_AlreadyExported_MarksOriginalAsReversed()
	{
		var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		var controller = Controller(db);

		var create = await controller.Create(DeclDto(phase.Id, op1.Id, machine.Id, confirmed: 10), CancellationToken.None);
		var originalId = Assert.IsType<int>(create.Value);

		var original = await db.ProductionDeclarations.FindAsync(originalId);
		Assert.NotNull(original);
		original!.ExternalCounterId = "ERP-001";
		await db.SaveChangesAsync();

		await controller.Correct(originalId, DeclDto(phase.Id, op1.Id, machine.Id, confirmed: 11), CancellationToken.None);

		var refreshedOriginal = await db.ProductionDeclarations.FindAsync(originalId);
		var reversal = await db.ProductionDeclarations.FirstOrDefaultAsync(x => x.IsReversal && x.ReversalOfId == originalId);

		Assert.NotNull(refreshedOriginal);
		Assert.NotNull(reversal);
		Assert.Equal(reversal!.Id, refreshedOriginal!.ReversedById);
	}

	[Fact]
	public async Task Correct_AlreadyExported_CreatesNewCorrectedRecord()
	{
		var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		var controller = Controller(db);

		var create = await controller.Create(DeclDto(phase.Id, op1.Id, machine.Id, confirmed: 10), CancellationToken.None);
		var originalId = Assert.IsType<int>(create.Value);

		var original = await db.ProductionDeclarations.FindAsync(originalId);
		Assert.NotNull(original);
		original!.ExternalCounterId = "ERP-001";
		await db.SaveChangesAsync();

		await controller.Correct(originalId, DeclDto(phase.Id, op1.Id, machine.Id, confirmed: 77), CancellationToken.None);

		var corrected = await db.ProductionDeclarations.FirstOrDefaultAsync(x =>
			x.Id != originalId && !x.IsReversal && x.ReversedById == null && x.ConfirmedQuantity == 77);
		Assert.NotNull(corrected);
	}

	[Fact]
	public async Task Correct_AlreadyExported_PhaseAggregatesUpdated()
	{
		var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		var controller = Controller(db);

		var create = await controller.Create(DeclDto(phase.Id, op1.Id, machine.Id, confirmed: 10), CancellationToken.None);
		var originalId = Assert.IsType<int>(create.Value);

		var original = await db.ProductionDeclarations.FindAsync(originalId);
		Assert.NotNull(original);
		original!.ExternalCounterId = "ERP-001";
		await db.SaveChangesAsync();

		await controller.Correct(originalId, DeclDto(phase.Id, op1.Id, machine.Id, confirmed: 20), CancellationToken.None);

		var updatedPhase = await db.ProductionOrderPhases.FindAsync(phase.Id);
		Assert.NotNull(updatedPhase);
		Assert.Equal(10m, updatedPhase!.ConfirmedQuantity);
	}

	[Fact]
	public async Task Create_WithAutomaticPickingLine_CreatesStockMovement()
	{
		var (db, machine, op1, phase, material, storageLocation) = await TestDbFactory.SeedForProductionDeclarationTests();
		var now = DateTimeOffset.UtcNow;

		phase.PlannedQuantity = 50;
		db.PhasePickingLists.Add(new PhasePickingList
		{
			ProductionOrderPhaseId = phase.Id,
			MaterialId = material.Id,
			StorageLocationId = storageLocation.Id,
			IsAutomatic = true,
			IsConsumable = false,
			IsPhantom = false,
			RequiredQuantity = 50,
			PickedQuantity = 0,
			Status = PickingStatus.Pending,
			CreatedAt = now,
			UpdatedAt = now,
		});
		await db.SaveChangesAsync();

		await Controller(db).Create(DeclDto(phase.Id, op1.Id, machine.Id, confirmed: 50), CancellationToken.None);

		var movements = await db.StockMovements.Where(x => x.MovementType == StockMovementType.ProductionIssue).ToListAsync();
		Assert.Single(movements);
		Assert.Equal(50m, movements[0].Quantity);
	}

	[Fact]
	public async Task Create_WithAutomaticPickingLine_DecrementsMaterialStock()
	{
		var (db, machine, op1, phase, material, storageLocation) = await TestDbFactory.SeedForProductionDeclarationTests();
		var now = DateTimeOffset.UtcNow;

		phase.PlannedQuantity = 50;
		db.PhasePickingLists.Add(new PhasePickingList
		{
			ProductionOrderPhaseId = phase.Id,
			MaterialId = material.Id,
			StorageLocationId = storageLocation.Id,
			IsAutomatic = true,
			IsConsumable = false,
			IsPhantom = false,
			RequiredQuantity = 50,
			PickedQuantity = 0,
			Status = PickingStatus.Pending,
			CreatedAt = now,
			UpdatedAt = now,
		});
		await db.SaveChangesAsync();

		await Controller(db).Create(DeclDto(phase.Id, op1.Id, machine.Id, confirmed: 50), CancellationToken.None);

		var stock = await db.MaterialStocks
			.FirstOrDefaultAsync(x => x.MaterialId == material.Id && x.StorageLocationId == storageLocation.Id);
		Assert.NotNull(stock);
		Assert.Equal(450m, stock!.Quantity);
	}
}
