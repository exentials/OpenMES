using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;
using OpenMES.WebApi.Controllers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace OpenMES.Tests;

/// <summary>
/// Tests for WebClient shift and declaration flows.
///
/// Covers three areas:
///   1. Shift transition logic — CanApplyEvent rules (inline, no WebClient dependency)
///   2. OperatorShiftSnapshot — deriving present/on-break/absent state
///   3. Declaration flow — operator selection and open-session API preconditions
///      validated at the API layer (ProductionDeclarationController)
///   4. ActionViewModel derived-property rules (logic replicated inline)
/// </summary>
public class WebClientShiftAndDeclarationFlowTests
{
	// ─────────────────────────────────────────────────────────────────────────
	// 1. Shift transition logic — CanApplyEvent
	//    Replicates OperatorShiftClientService.CanApplyEvent to keep the test
	//    project free of a WebClient (Sdk.Web) assembly reference.
	// ─────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Pure function that mirrors OperatorShiftClientService.CanApplyEvent.
	/// Kept here so tests do not need a reference to OpenMES.WebClient.
	/// If the production implementation changes, this must be updated too.
	/// </summary>
	private static bool CanApplyEvent(OperatorEventType? latest, OperatorEventType target)
		=> (target, latest) switch
		{
			(OperatorEventType.CheckIn,    null)                          => true,
			(OperatorEventType.CheckIn,    OperatorEventType.CheckOut)    => true,
			(OperatorEventType.BreakStart, OperatorEventType.CheckIn)     => true,
			(OperatorEventType.BreakStart, OperatorEventType.BreakEnd)    => true,
			(OperatorEventType.BreakEnd,   OperatorEventType.BreakStart)  => true,
			(OperatorEventType.CheckOut,   OperatorEventType.CheckIn)     => true,
			(OperatorEventType.CheckOut,   OperatorEventType.BreakStart)  => true,
			(OperatorEventType.CheckOut,   OperatorEventType.BreakEnd)    => true,
			_ => false,
		};

	[Theory]
	[InlineData(null,                          OperatorEventType.CheckIn)]
	[InlineData(OperatorEventType.CheckOut,    OperatorEventType.CheckIn)]
	[InlineData(OperatorEventType.CheckIn,     OperatorEventType.BreakStart)]
	[InlineData(OperatorEventType.BreakEnd,    OperatorEventType.BreakStart)]
	[InlineData(OperatorEventType.BreakStart,  OperatorEventType.BreakEnd)]
	[InlineData(OperatorEventType.CheckIn,     OperatorEventType.CheckOut)]
	[InlineData(OperatorEventType.BreakStart,  OperatorEventType.CheckOut)]
	[InlineData(OperatorEventType.BreakEnd,    OperatorEventType.CheckOut)]
	public void CanApplyEvent_ValidTransition_ReturnsTrue(
		OperatorEventType? latest, OperatorEventType target)
		=> Assert.True(CanApplyEvent(latest, target));

	[Theory]
	[InlineData(OperatorEventType.CheckIn,     OperatorEventType.CheckIn)]
	[InlineData(OperatorEventType.BreakStart,  OperatorEventType.CheckIn)]
	[InlineData(OperatorEventType.BreakEnd,    OperatorEventType.CheckIn)]
	[InlineData(null,                          OperatorEventType.CheckOut)]
	[InlineData(OperatorEventType.CheckOut,    OperatorEventType.CheckOut)]
	[InlineData(null,                          OperatorEventType.BreakStart)]
	[InlineData(OperatorEventType.CheckOut,    OperatorEventType.BreakStart)]
	[InlineData(OperatorEventType.BreakStart,  OperatorEventType.BreakStart)]
	[InlineData(null,                          OperatorEventType.BreakEnd)]
	[InlineData(OperatorEventType.CheckIn,     OperatorEventType.BreakEnd)]
	[InlineData(OperatorEventType.CheckOut,    OperatorEventType.BreakEnd)]
	[InlineData(OperatorEventType.BreakEnd,    OperatorEventType.BreakEnd)]
	public void CanApplyEvent_InvalidTransition_ReturnsFalse(
		OperatorEventType? latest, OperatorEventType target)
		=> Assert.False(CanApplyEvent(latest, target));

	// ─────────────────────────────────────────────────────────────────────────
	// 2. OperatorShiftSnapshot — presence derivation from latest events
	// ─────────────────────────────────────────────────────────────────────────

	private static bool IsPresent(OperatorEventType? latest)
		=> latest is OperatorEventType.CheckIn or OperatorEventType.BreakEnd;

	private static bool IsOnBreak(OperatorEventType? latest)
		=> latest == OperatorEventType.BreakStart;

	[Theory]
	[InlineData(OperatorEventType.CheckIn,    true,  false)]
	[InlineData(OperatorEventType.BreakEnd,   true,  false)]
	[InlineData(OperatorEventType.BreakStart, false, true)]
	[InlineData(OperatorEventType.CheckOut,   false, false)]
	public void PresenceState_MatchesLatestEvent(
		OperatorEventType evt, bool expectedPresent, bool expectedOnBreak)
	{
		Assert.Equal(expectedPresent, IsPresent(evt));
		Assert.Equal(expectedOnBreak, IsOnBreak(evt));
	}

	[Fact]
	public void PresenceState_NoShift_IsAbsent()
	{
		Assert.False(IsPresent(null));
		Assert.False(IsOnBreak(null));
	}

	[Fact]
	public void Snapshot_GroupByOperator_LatestEventWins()
	{
		var now = DateTimeOffset.UtcNow;
		var allEvents = new List<OperatorShiftDto>
		{
			new() { OperatorId = 1, EventType = OperatorEventType.CheckIn,  EventTime = now.AddHours(-3) },
			new() { OperatorId = 1, EventType = OperatorEventType.CheckOut, EventTime = now.AddHours(-1) },
			new() { OperatorId = 2, EventType = OperatorEventType.CheckIn,  EventTime = now.AddHours(-2) },
		};

		var latest = allEvents
			.GroupBy(x => x.OperatorId)
			.Select(g => g.OrderByDescending(s => s.EventTime).First())
			.ToDictionary(x => x.OperatorId, x => x.EventType);

		Assert.Equal(OperatorEventType.CheckOut, latest[1]); // last event wins
		Assert.Equal(OperatorEventType.CheckIn,  latest[2]);
		Assert.False(IsPresent(latest[1]));
		Assert.True(IsPresent(latest[2]));
	}

	[Fact]
	public void Snapshot_MultipleOperators_IndependentPresenceState()
	{
		var now = DateTimeOffset.UtcNow;
		var latestShifts = new Dictionary<int, OperatorEventType>
		{
			[1] = OperatorEventType.CheckIn,
			[2] = OperatorEventType.BreakStart,
			[3] = OperatorEventType.BreakEnd,
			[4] = OperatorEventType.CheckOut,
		};

		Assert.True(IsPresent(latestShifts[1]));   // checked in
		Assert.False(IsPresent(latestShifts[2]));  // on break → not present
		Assert.True(IsPresent(latestShifts[3]));   // back from break
		Assert.False(IsPresent(latestShifts[4]));  // checked out
	}

	[Fact]
	public void Snapshot_PresentOperators_ExcludesOnBreakAndCheckedOut()
	{
		var now = DateTimeOffset.UtcNow;
		var latestShifts = new Dictionary<int, OperatorEventType>
		{
			[1] = OperatorEventType.CheckIn,
			[2] = OperatorEventType.BreakStart,
			[3] = OperatorEventType.BreakEnd,
			[4] = OperatorEventType.CheckOut,
		};

		var presentIds = latestShifts
			.Where(kv => IsPresent(kv.Value))
			.Select(kv => kv.Key)
			.ToHashSet();

		Assert.Equal(2, presentIds.Count);
		Assert.Contains(1, presentIds);
		Assert.Contains(3, presentIds);
		Assert.DoesNotContain(2, presentIds);
		Assert.DoesNotContain(4, presentIds);
	}

	// ─────────────────────────────────────────────────────────────────────────
	// 3. Declaration flow API preconditions — operator selection enforcement
	// ─────────────────────────────────────────────────────────────────────────

	private static ProductionDeclarationController DeclController(
		OpenMES.Data.Contexts.OpenMESDbContext db)
		=> new(db, NullLogger<ProductionDeclarationController>.Instance);

	private static ProductionDeclarationDto MakeDeclDto(
		int phaseId, int opId, int machineId, decimal qty = 5)
		=> new()
		{
			ProductionOrderPhaseId = phaseId,
			MachineId              = machineId,
			OperatorId             = opId,
			ConfirmedQuantity      = qty,
			ScrapQuantity          = 0,
			DeclarationDate        = DateTimeOffset.UtcNow,
		};

	/// <summary>
	/// Happy path: operator checked in (seeded by factory), declaration succeeds.
	/// Represents the terminal UX: operator selected in dialog → Work session open → Declare.
	/// </summary>
	[Fact]
	public async Task Declaration_OperatorCheckedIn_Succeeds()
	{
		var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();

		var result = await DeclController(db).Create(
			MakeDeclDto(phase.Id, op1.Id, machine.Id, qty: 10), CancellationToken.None);

		Assert.True(result.Value > 0);
	}

	/// <summary>
	/// Operator selected in dialog but absent (no shift events).
	/// Server blocks the declaration — UI selection does not bypass the check.
	/// </summary>
	[Fact]
	public async Task Declaration_OperatorAbsent_ApiBlocksWithProblemException()
	{
		var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		db.OperatorShifts.RemoveRange(db.OperatorShifts);
		await db.SaveChangesAsync();

		await Assert.ThrowsAsync<OpenMES.WebApi.ProblemException>(() =>
			DeclController(db).Create(MakeDeclDto(phase.Id, op1.Id, machine.Id), CancellationToken.None));
	}

	/// <summary>
	/// Operator selected in dialog but currently on break.
	/// Server blocks the declaration even though UI shows operator as "present".
	/// </summary>
	[Fact]
	public async Task Declaration_OperatorOnBreak_ApiBlocksWithProblemException()
	{
		var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		var now = DateTimeOffset.UtcNow;
		db.OperatorShifts.Add(new OperatorShift
		{
			OperatorId = op1.Id, EventType = OperatorEventType.BreakStart,
			EventTime = now, Source = "Test", CreatedAt = now, UpdatedAt = now,
		});
		await db.SaveChangesAsync();

		await Assert.ThrowsAsync<OpenMES.WebApi.ProblemException>(() =>
			DeclController(db).Create(MakeDeclDto(phase.Id, op1.Id, machine.Id), CancellationToken.None));
	}

	/// <summary>
	/// Operator checked in, then checks out before declaring.
	/// Server blocks — post-checkout declaration is not allowed.
	/// </summary>
	[Fact]
	public async Task Declaration_OperatorCheckedOut_ApiBlocksWithProblemException()
	{
		var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		var now = DateTimeOffset.UtcNow;
		db.OperatorShifts.Add(new OperatorShift
		{
			OperatorId = op1.Id, EventType = OperatorEventType.CheckOut,
			EventTime = now, Source = "Test", CreatedAt = now, UpdatedAt = now,
		});
		await db.SaveChangesAsync();

		await Assert.ThrowsAsync<OpenMES.WebApi.ProblemException>(() =>
			DeclController(db).Create(MakeDeclDto(phase.Id, op1.Id, machine.Id), CancellationToken.None));
	}

	/// <summary>
	/// Operator returns from break (BreakEnd) and declares successfully.
	/// Ensures BreakEnd is treated as a valid presence state for declarations.
	/// </summary>
	[Fact]
	public async Task Declaration_OperatorAfterBreakEnd_Succeeds()
	{
		var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForProductionDeclarationTests();
		var now = DateTimeOffset.UtcNow;
		db.OperatorShifts.Add(new OperatorShift
		{
			OperatorId = op1.Id, EventType = OperatorEventType.BreakEnd,
			EventTime = now, Source = "Test", CreatedAt = now, UpdatedAt = now,
		});
		await db.SaveChangesAsync();

		var result = await DeclController(db).Create(
			MakeDeclDto(phase.Id, op1.Id, machine.Id, qty: 8), CancellationToken.None);

		Assert.True(result.Value > 0);
	}

	// ─────────────────────────────────────────────────────────────────────────
	// 4. ActionViewModel derived properties (pure logic, no Blazor dependency)
	//    Mirrors the rules in ActionViewModel without referencing the project.
	// ─────────────────────────────────────────────────────────────────────────

	// CanDeclare = HasOpenSession && SessionType == Work && ActivePhase != null
	private static bool CanDeclare(
		WorkSessionDto? openSession, ProductionOrderPhaseDto? activePhase)
		=> openSession is not null
		   && openSession.SessionType == WorkSessionType.Work
		   && activePhase is not null;

	[Fact]
	public void CanDeclare_WorkSession_WithPhase_ReturnsTrue()
	{
		var session = new WorkSessionDto { SessionType = WorkSessionType.Work, OperatorId = 1 };
		var phase   = new ProductionOrderPhaseDto { Id = 1 };
		Assert.True(CanDeclare(session, phase));
	}

	[Fact]
	public void CanDeclare_SetupSession_ReturnsFalse()
	{
		var session = new WorkSessionDto { SessionType = WorkSessionType.Setup, OperatorId = 1 };
		var phase   = new ProductionOrderPhaseDto { Id = 1 };
		Assert.False(CanDeclare(session, phase));
	}

	[Fact]
	public void CanDeclare_WaitSession_ReturnsFalse()
	{
		var session = new WorkSessionDto { SessionType = WorkSessionType.Wait, OperatorId = 1 };
		var phase   = new ProductionOrderPhaseDto { Id = 1 };
		Assert.False(CanDeclare(session, phase));
	}

	[Fact]
	public void CanDeclare_NoPhase_ReturnsFalse()
	{
		var session = new WorkSessionDto { SessionType = WorkSessionType.Work, OperatorId = 1 };
		Assert.False(CanDeclare(session, null));
	}

	[Fact]
	public void CanDeclare_NoSession_ReturnsFalse()
	{
		var phase = new ProductionOrderPhaseDto { Id = 1 };
		Assert.False(CanDeclare(null, phase));
	}

	// IsOperatorPresent = latest event is CheckIn or BreakEnd
	[Theory]
	[InlineData(OperatorEventType.CheckIn,    true)]
	[InlineData(OperatorEventType.BreakEnd,   true)]
	[InlineData(OperatorEventType.BreakStart, false)]
	[InlineData(OperatorEventType.CheckOut,   false)]
	public void IsOperatorPresent_ReflectsLatestShiftEvent(
		OperatorEventType evt, bool expected)
		=> Assert.Equal(expected, IsPresent(evt));

	[Fact]
	public void IsOperatorPresent_NoShift_ReturnsFalse()
		=> Assert.False(IsPresent(null));

	// IsOperatorOnBreak = latest event is BreakStart
	[Theory]
	[InlineData(OperatorEventType.BreakStart, true)]
	[InlineData(OperatorEventType.CheckIn,    false)]
	[InlineData(OperatorEventType.BreakEnd,   false)]
	[InlineData(OperatorEventType.CheckOut,   false)]
	public void IsOperatorOnBreak_ReflectsLatestShiftEvent(
		OperatorEventType evt, bool expected)
		=> Assert.Equal(expected, IsOnBreak(evt));

	[Fact]
	public void IsOperatorOnBreak_NoShift_ReturnsFalse()
		=> Assert.False(IsOnBreak(null));
}
