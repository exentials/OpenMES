using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OpenMES.Data.Common;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;
using OpenMES.WebApi;
using OpenMES.WebApi.Controllers;

namespace OpenMES.Tests;

/// <summary>
/// Tests for quantity validation rules in ProductionDeclarationController.
/// Rule A: Intra-phase cap (unless first phase with AllowOverproduction=true)
/// Rule B: Inter-phase constraint (confirmed pieces <= previous phase confirmed)
/// </summary>
public class QuantityValidationTests
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

	/// <summary>Helper: sets up two-phase order for testing.</summary>
	private static async Task<(ProductionOrderPhase phase1, ProductionOrderPhase phase2)>
		SeedTwoPhaseOrder(OpenMESDbContext db, bool allowOverproduction = false)
	{
		// Set AllowOverproduction on the seeded material
		var material = await db.Materials.FirstAsync();
		material.AllowOverproduction = allowOverproduction;

		var order = await db.ProductionOrders.Include(o => o.ProductionOrderPhases).FirstAsync();
		var plant  = await db.Plants.FirstAsync();
		var wc     = await db.WorkCenters.FirstAsync();
		var now    = DateTimeOffset.UtcNow;

		// Rename existing phase to 0010
		var phase1 = order.ProductionOrderPhases.First();
		phase1.PhaseNumber = "0010";

		// Add a second phase 0020
		var phase2 = new ProductionOrderPhase
		{
			ProductionOrder = order, PhaseNumber = "0020",
			WorkCenter = wc, ExternalId = "EXT-0020",
			PlannedQuantity = 100, Status = OrderStatus.Released,
			WorkCode = "MILL", Description = "Milling",
			StartDate = now, EndDate = now.AddDays(1),
			CreatedAt = now, UpdatedAt = now,
		};
		db.ProductionOrderPhases.Add(phase2);
		await db.SaveChangesAsync();
		return (phase1, phase2);
	}

	// ── Rule A: Intra-phase cap ──────────────────────────────────────────────

	/// <summary>QV_A1: Within plan — succeeds.</summary>
	[Fact]
	public async Task QV_A1_Create_WithinPlan_Succeeds()
	{
		var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		// phase.PlannedQuantity = 100 (seeded), confirmed=0
		// Declare confirmed=60, scrap=10 (total=70 ≤ 100)

		await Controller(db).Create(DeclDto(phase.Id, op1.Id, machine.Id, confirmed: 60, scrap: 10), CancellationToken.None);

		var updatedPhase = await db.ProductionOrderPhases.FindAsync(phase.Id);
		Assert.NotNull(updatedPhase);
		Assert.Equal(60m, updatedPhase!.ConfirmedQuantity);
		Assert.Equal(10m, updatedPhase!.ScrapQuantity);
	}

	/// <summary>QV_A2: Exceeds plan — throws ProblemException.</summary>
	[Fact]
	public async Task QV_A2_Create_ExceedsPlan_ThrowsProblemException()
	{
		var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		// phase.PlannedQuantity = 100, confirmed=0
		// Declare confirmed=90, scrap=20 (total=110 > 100)

		var ex = await Assert.ThrowsAsync<ProblemException>(async () =>
			await Controller(db).Create(DeclDto(phase.Id, op1.Id, machine.Id, confirmed: 90, scrap: 20), CancellationToken.None));
		
		Assert.Contains("Quantity exceeds phase plan", ex.Error);
	}

	/// <summary>QV_A3: Partially filled, then exceeds — throws on second declaration.</summary>
	[Fact]
	public async Task QV_A3_Create_ExceedsPlan_PartiallyFilled_ThrowsProblemException()
	{
		var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		var controller = Controller(db);

		// First declaration: 60 (ok, ≤ 100)
		await controller.Create(DeclDto(phase.Id, op1.Id, machine.Id, confirmed: 60), CancellationToken.None);

		// Second declaration: 50 more (60+50=110 > 100) → should throw
		var ex = await Assert.ThrowsAsync<ProblemException>(async () =>
			await controller.Create(DeclDto(phase.Id, op1.Id, machine.Id, confirmed: 50), CancellationToken.None));

		Assert.Contains("Quantity exceeds phase plan", ex.Error);
	}

	/// <summary>QV_A4: First phase with AllowOverproduction=true — no limit.</summary>
	[Fact]
	public async Task QV_A4_Create_FirstPhase_AllowOverproduction_NoLimit()
	{
		var (db, machine, op1, _, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		var (phase1, _) = await SeedTwoPhaseOrder(db, allowOverproduction: true);

		// Declare confirmed=200 > PlannedQty=100
		await Controller(db).Create(DeclDto(phase1.Id, op1.Id, machine.Id, confirmed: 200, scrap: 0), CancellationToken.None);

		var updatedPhase = await db.ProductionOrderPhases.FindAsync(phase1.Id);
		Assert.NotNull(updatedPhase);
		Assert.Equal(200m, updatedPhase!.ConfirmedQuantity);
	}

	/// <summary>QV_A5: Second phase — AllowOverproduction cap still applies.</summary>
	[Fact]
	public async Task QV_A5_Create_SecondPhase_AllowOverproduction_CapStillApplies()
	{
		var (db, machine, op1, _, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		var (phase1, phase2) = await SeedTwoPhaseOrder(db, allowOverproduction: true);

		// Simulate first phase already confirmed=100
		phase1.ConfirmedQuantity = 100;
		await db.SaveChangesAsync();

		// Try to declare on SECOND phase: confirmed=200 > PlannedQty=100
		// Should throw even with AllowOverproduction (only exempts first phase)
		var ex = await Assert.ThrowsAsync<ProblemException>(async () =>
			await Controller(db).Create(DeclDto(phase2.Id, op1.Id, machine.Id, confirmed: 200, scrap: 0), CancellationToken.None));

		Assert.Contains("Quantity exceeds phase plan", ex.Error);
	}

	// ── Rule B: Inter-phase constraint ───────────────────────────────────────

	/// <summary>QV_B1: Second phase within previous confirmed — succeeds.</summary>
	[Fact]
	public async Task QV_B1_Create_SecondPhase_WithinPrevConfirmed_Succeeds()
	{
		var (db, machine, op1, _, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		var (phase1, phase2) = await SeedTwoPhaseOrder(db, allowOverproduction: false);

		// Set phase1.ConfirmedQuantity=80 (simulating prior declarations)
		phase1.ConfirmedQuantity = 80;
		await db.SaveChangesAsync();

		// Declare on phase2: confirmed=50 (≤ 80) → succeeds
		await Controller(db).Create(DeclDto(phase2.Id, op1.Id, machine.Id, confirmed: 50), CancellationToken.None);

		var updatedPhase2 = await db.ProductionOrderPhases.FindAsync(phase2.Id);
		Assert.NotNull(updatedPhase2);
		Assert.Equal(50m, updatedPhase2!.ConfirmedQuantity);
	}

	/// <summary>QV_B2: Second phase exceeds previous confirmed — throws.</summary>
	[Fact]
	public async Task QV_B2_Create_SecondPhase_ExceedsPrevConfirmed_ThrowsProblemException()
	{
		var (db, machine, op1, _, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		var (phase1, phase2) = await SeedTwoPhaseOrder(db, allowOverproduction: false);

		// Set phase1.ConfirmedQuantity=80
		phase1.ConfirmedQuantity = 80;
		await db.SaveChangesAsync();

		// Declare on phase2: confirmed=90 (> 80) → throws
		var ex = await Assert.ThrowsAsync<ProblemException>(async () =>
			await Controller(db).Create(DeclDto(phase2.Id, op1.Id, machine.Id, confirmed: 90), CancellationToken.None));

		Assert.Contains("Quantity exceeds previous phase output", ex.Error);
	}

	/// <summary>QV_B3: Scrap from prev phase not transferred — inter-phase uses confirmed only.</summary>
	[Fact]
	public async Task QV_B3_Create_SecondPhase_PrevPhaseScrapNotTransferred()
	{
		var (db, machine, op1, _, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		var (phase1, phase2) = await SeedTwoPhaseOrder(db, allowOverproduction: false);

		// Set phase1: ConfirmedQuantity=70, ScrapQuantity=30 (total=100)
		phase1.ConfirmedQuantity = 70;
		phase1.ScrapQuantity = 30;
		await db.SaveChangesAsync();

		// Declare on phase2: confirmed=75 (> ConfirmedQty=70, even though total=100)
		// Should throw (inter-phase constraint is on ConfirmedQty only)
		var ex = await Assert.ThrowsAsync<ProblemException>(async () =>
			await Controller(db).Create(DeclDto(phase2.Id, op1.Id, machine.Id, confirmed: 75), CancellationToken.None));

		Assert.Contains("Quantity exceeds previous phase output", ex.Error);
	}

	/// <summary>QV_B4: Partially filled phase, then exceeds remaining capacity.</summary>
	[Fact]
	public async Task QV_B4_Create_SecondPhase_PartiallyFilled_RemainingIsCorrect()
	{
		var (db, machine, op1, _, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		var (phase1, phase2) = await SeedTwoPhaseOrder(db, allowOverproduction: false);
		var controller = Controller(db);

		// Set phase1.ConfirmedQuantity=80
		phase1.ConfirmedQuantity = 80;
		await db.SaveChangesAsync();

		// First declaration on phase2: confirmed=30 (30 ≤ 80) → succeeds
		await controller.Create(DeclDto(phase2.Id, op1.Id, machine.Id, confirmed: 30), CancellationToken.None);

		// Second declaration on phase2: confirmed=40 (30+40=70 ≤ 80) → succeeds
		await controller.Create(DeclDto(phase2.Id, op1.Id, machine.Id, confirmed: 40), CancellationToken.None);

		// Third declaration on phase2: confirmed=20 (70+20=90 > 80) → throws
		var ex = await Assert.ThrowsAsync<ProblemException>(async () =>
			await controller.Create(DeclDto(phase2.Id, op1.Id, machine.Id, confirmed: 20), CancellationToken.None));

		Assert.Contains("Quantity exceeds previous phase output", ex.Error);
	}

	/// <summary>QV_B5: First phase has no previous phase constraint.</summary>
	[Fact]
	public async Task QV_B5_Create_FirstPhase_NoPrevPhaseConstraint()
	{
		var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		// Single-phase order: phase.PlannedQuantity=100

		// Declare confirmed=90 (≤ PlannedQty=100) → succeeds
		// No inter-phase check runs because this is first (only) phase
		await Controller(db).Create(DeclDto(phase.Id, op1.Id, machine.Id, confirmed: 90), CancellationToken.None);

		var updatedPhase = await db.ProductionOrderPhases.FindAsync(phase.Id);
		Assert.NotNull(updatedPhase);
		Assert.Equal(90m, updatedPhase!.ConfirmedQuantity);
	}
}
