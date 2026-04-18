using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;
using OpenMES.WebApi.Controllers;
using Xunit;

namespace OpenMES.Tests;

/// <summary>
/// Tests for GET /operatorshift/present.
/// Verifies that only operators whose latest shift event is CheckIn or BreakEnd
/// are returned, with optional filtering by plantId.
/// </summary>
public class OperatorShiftPresentTests
{
	private static OperatorShiftController Controller(OpenMES.Data.Contexts.OpenMESDbContext db)
		=> new(db, NullLogger<OperatorShiftController>.Instance);

	/// <summary>
	/// Seeds two plants, two operators per plant, with no shift events.
	/// Returns: db, plant1, plant2, op1a, op1b (plant1), op2a, op2b (plant2).
	/// </summary>
	private static async Task<(
		OpenMES.Data.Contexts.OpenMESDbContext db,
		Plant plant1, Plant plant2,
		Operator op1a, Operator op1b,
		Operator op2a, Operator op2b)> SeedTwoPlants()
	{
		var db = TestDbFactory.Create();
		var now = DateTimeOffset.UtcNow;

		var plant1 = new Plant { Code = "P1", Description = "Plant 1", CreatedAt = now, UpdatedAt = now };
		var plant2 = new Plant { Code = "P2", Description = "Plant 2", CreatedAt = now, UpdatedAt = now };
		db.Plants.AddRange(plant1, plant2);

		var op1a = new Operator { Name = "Op 1A", EmployeeNumber = "1A", Badge = "B1A", Plant = plant1, CreatedAt = now, UpdatedAt = now };
		var op1b = new Operator { Name = "Op 1B", EmployeeNumber = "1B", Badge = "B1B", Plant = plant1, CreatedAt = now, UpdatedAt = now };
		var op2a = new Operator { Name = "Op 2A", EmployeeNumber = "2A", Badge = "B2A", Plant = plant2, CreatedAt = now, UpdatedAt = now };
		var op2b = new Operator { Name = "Op 2B", EmployeeNumber = "2B", Badge = "B2B", Plant = plant2, CreatedAt = now, UpdatedAt = now };
		db.Operators.AddRange(op1a, op1b, op2a, op2b);

		await db.SaveChangesAsync();
		return (db, plant1, plant2, op1a, op1b, op2a, op2b);
	}

	private static async Task AddEvent(
		OpenMES.Data.Contexts.OpenMESDbContext db,
		int operatorId,
		OperatorEventType type,
		int minutesAgo = 60)
	{
		var now = DateTimeOffset.UtcNow;
		db.OperatorShifts.Add(new OperatorShift
		{
			OperatorId = operatorId,
			EventType = type,
			EventTime = now.AddMinutes(-minutesAgo),
			Source = "Test",
			CreatedAt = now,
			UpdatedAt = now,
		});
		await db.SaveChangesAsync();
	}

	// ── Basic presence rules ──────────────────────────────────────────────────

	[Fact]
	public async Task Present_NoShiftEvents_ReturnsEmpty()
	{
		var (db, plant1, _, _, _, _, _) = await SeedTwoPlants();

		var result = await Controller(db).GetPresent(plant1.Id, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var items = Assert.IsAssignableFrom<IEnumerable<OperatorDto>>(ok.Value).ToList();
		Assert.Empty(items);
	}

	[Fact]
	public async Task Present_AfterCheckIn_OperatorIsPresent()
	{
		var (db, plant1, _, op1a, _, _, _) = await SeedTwoPlants();
		await AddEvent(db, op1a.Id, OperatorEventType.CheckIn);

		var result = await Controller(db).GetPresent(plant1.Id, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var items = Assert.IsAssignableFrom<IEnumerable<OperatorDto>>(ok.Value).ToList();
		Assert.Single(items);
		Assert.Equal(op1a.Id, items[0].Id);
	}

	[Fact]
	public async Task Present_AfterCheckOut_OperatorIsAbsent()
	{
		var (db, plant1, _, op1a, _, _, _) = await SeedTwoPlants();
		await AddEvent(db, op1a.Id, OperatorEventType.CheckIn, minutesAgo: 120);
		await AddEvent(db, op1a.Id, OperatorEventType.CheckOut, minutesAgo: 10);

		var result = await Controller(db).GetPresent(plant1.Id, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var items = Assert.IsAssignableFrom<IEnumerable<OperatorDto>>(ok.Value).ToList();
		Assert.Empty(items);
	}

	[Fact]
	public async Task Present_OperatorOnBreak_IsNotPresent()
	{
		// BreakStart is NOT a presence qualifier — endpoint returns CheckIn or BreakEnd only
		var (db, plant1, _, op1a, _, _, _) = await SeedTwoPlants();
		await AddEvent(db, op1a.Id, OperatorEventType.CheckIn, minutesAgo: 120);
		await AddEvent(db, op1a.Id, OperatorEventType.BreakStart, minutesAgo: 30);

		var result = await Controller(db).GetPresent(plant1.Id, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var items = Assert.IsAssignableFrom<IEnumerable<OperatorDto>>(ok.Value).ToList();
		Assert.Empty(items);
	}

	[Fact]
	public async Task Present_AfterBreakEnd_OperatorIsPresent()
	{
		var (db, plant1, _, op1a, _, _, _) = await SeedTwoPlants();
		await AddEvent(db, op1a.Id, OperatorEventType.CheckIn, minutesAgo: 180);
		await AddEvent(db, op1a.Id, OperatorEventType.BreakStart, minutesAgo: 60);
		await AddEvent(db, op1a.Id, OperatorEventType.BreakEnd, minutesAgo: 20);

		var result = await Controller(db).GetPresent(plant1.Id, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var items = Assert.IsAssignableFrom<IEnumerable<OperatorDto>>(ok.Value).ToList();
		Assert.Single(items);
		Assert.Equal(op1a.Id, items[0].Id);
	}

	// ── Multiple operators ────────────────────────────────────────────────────

	[Fact]
	public async Task Present_TwoOperators_BothCheckedIn_ReturnsBoth()
	{
		var (db, plant1, _, op1a, op1b, _, _) = await SeedTwoPlants();
		await AddEvent(db, op1a.Id, OperatorEventType.CheckIn, minutesAgo: 60);
		await AddEvent(db, op1b.Id, OperatorEventType.CheckIn, minutesAgo: 45);

		var result = await Controller(db).GetPresent(plant1.Id, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var items = Assert.IsAssignableFrom<IEnumerable<OperatorDto>>(ok.Value).ToList();
		Assert.Equal(2, items.Count);
	}

	[Fact]
	public async Task Present_OneCheckedIn_OneCheckedOut_ReturnsOne()
	{
		var (db, plant1, _, op1a, op1b, _, _) = await SeedTwoPlants();
		await AddEvent(db, op1a.Id, OperatorEventType.CheckIn, minutesAgo: 120);
		await AddEvent(db, op1b.Id, OperatorEventType.CheckIn, minutesAgo: 120);
		await AddEvent(db, op1b.Id, OperatorEventType.CheckOut, minutesAgo: 10);

		var result = await Controller(db).GetPresent(plant1.Id, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var items = Assert.IsAssignableFrom<IEnumerable<OperatorDto>>(ok.Value).ToList();
		Assert.Single(items);
		Assert.Equal(op1a.Id, items[0].Id);
	}

	[Fact]
	public async Task Present_MixedStates_OnlyCheckInAndBreakEndArePresent()
	{
		var (db, plant1, _, op1a, op1b, _, _) = await SeedTwoPlants();
		// op1a: CheckIn → BreakStart → BreakEnd → present
		await AddEvent(db, op1a.Id, OperatorEventType.CheckIn, minutesAgo: 180);
		await AddEvent(db, op1a.Id, OperatorEventType.BreakStart, minutesAgo: 60);
		await AddEvent(db, op1a.Id, OperatorEventType.BreakEnd, minutesAgo: 20);
		// op1b: CheckIn → BreakStart → not present
		await AddEvent(db, op1b.Id, OperatorEventType.CheckIn, minutesAgo: 180);
		await AddEvent(db, op1b.Id, OperatorEventType.BreakStart, minutesAgo: 10);

		var result = await Controller(db).GetPresent(plant1.Id, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var items = Assert.IsAssignableFrom<IEnumerable<OperatorDto>>(ok.Value).ToList();
		Assert.Single(items);
		Assert.Equal(op1a.Id, items[0].Id);
	}

	// ── Plant filtering ───────────────────────────────────────────────────────

	[Fact]
	public async Task Present_FilterByPlant_ReturnsOnlyOperatorsOfThatPlant()
	{
		var (db, plant1, _, op1a, _, op2a, _) = await SeedTwoPlants();
		await AddEvent(db, op1a.Id, OperatorEventType.CheckIn);
		await AddEvent(db, op2a.Id, OperatorEventType.CheckIn);

		var result = await Controller(db).GetPresent(plant1.Id, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var items = Assert.IsAssignableFrom<IEnumerable<OperatorDto>>(ok.Value).ToList();
		Assert.Single(items);
		Assert.Equal(op1a.Id, items[0].Id);
	}

	[Fact]
	public async Task Present_FilterByPlant2_ReturnsBothPlant2Operators()
	{
		var (db, _, plant2, op1a, _, op2a, op2b) = await SeedTwoPlants();
		await AddEvent(db, op1a.Id, OperatorEventType.CheckIn);
		await AddEvent(db, op2a.Id, OperatorEventType.CheckIn);
		await AddEvent(db, op2b.Id, OperatorEventType.CheckIn);

		var result = await Controller(db).GetPresent(plant2.Id, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var items = Assert.IsAssignableFrom<IEnumerable<OperatorDto>>(ok.Value).ToList();
		Assert.Equal(2, items.Count);
		Assert.All(items, dto => Assert.Contains(dto.Id, new[] { op2a.Id, op2b.Id }));
	}

	[Fact]
	public async Task Present_NoPlantFilter_ReturnsAllPresentOperators()
	{
		var (db, _, _, op1a, _, op2a, _) = await SeedTwoPlants();
		await AddEvent(db, op1a.Id, OperatorEventType.CheckIn);
		await AddEvent(db, op2a.Id, OperatorEventType.CheckIn);

		var result = await Controller(db).GetPresent(null, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var items = Assert.IsAssignableFrom<IEnumerable<OperatorDto>>(ok.Value).ToList();
		Assert.Equal(2, items.Count);
	}

	[Fact]
	public async Task Present_FilterByPlant_NonePresent_ReturnsEmpty()
	{
		var (db, plant1, _, _, _, op2a, _) = await SeedTwoPlants();
		// Only plant2 operator checked in
		await AddEvent(db, op2a.Id, OperatorEventType.CheckIn);

		var result = await Controller(db).GetPresent(plant1.Id, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var items = Assert.IsAssignableFrom<IEnumerable<OperatorDto>>(ok.Value).ToList();
		Assert.Empty(items);
	}

	[Fact]
	public async Task Present_FilterByNonExistentPlant_ReturnsEmpty()
	{
		var (db, _, _, op1a, _, _, _) = await SeedTwoPlants();
		await AddEvent(db, op1a.Id, OperatorEventType.CheckIn);

		var result = await Controller(db).GetPresent(plantId: 9999, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var items = Assert.IsAssignableFrom<IEnumerable<OperatorDto>>(ok.Value).ToList();
		Assert.Empty(items);
	}

	// ── Presence consistency with multiple events ─────────────────────────────

	[Fact]
	public async Task Present_CheckInOutThenCheckInAgain_IsPresent()
	{
		// Operator completes a full shift then checks in again → must be present
		var (db, plant1, _, op1a, _, _, _) = await SeedTwoPlants();
		await AddEvent(db, op1a.Id, OperatorEventType.CheckIn, minutesAgo: 300);
		await AddEvent(db, op1a.Id, OperatorEventType.CheckOut, minutesAgo: 180);
		await AddEvent(db, op1a.Id, OperatorEventType.CheckIn, minutesAgo: 60);

		var result = await Controller(db).GetPresent(plant1.Id, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var items = Assert.IsAssignableFrom<IEnumerable<OperatorDto>>(ok.Value).ToList();
		Assert.Single(items);
		Assert.Equal(op1a.Id, items[0].Id);
	}

	[Fact]
	public async Task Present_Op1CheckedOut_Op2CheckedIn_OnlyOp2Returned()
	{
		var (db, plant1, _, op1a, op1b, _, _) = await SeedTwoPlants();
		await AddEvent(db, op1a.Id, OperatorEventType.CheckIn, minutesAgo: 180);
		await AddEvent(db, op1a.Id, OperatorEventType.CheckOut, minutesAgo: 90);
		await AddEvent(db, op1b.Id, OperatorEventType.CheckIn, minutesAgo: 60);

		var result = await Controller(db).GetPresent(plant1.Id, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var items = Assert.IsAssignableFrom<IEnumerable<OperatorDto>>(ok.Value).ToList();
		Assert.Single(items);
		Assert.Equal(op1b.Id, items[0].Id);
	}

	// ── Cross-plant isolation ─────────────────────────────────────────────────

	[Fact]
	public async Task Present_Plant1OperatorChecksOut_Plant2OperatorUnaffected()
	{
		var (db, plant1, plant2, op1a, _, op2a, _) = await SeedTwoPlants();
		await AddEvent(db, op1a.Id, OperatorEventType.CheckIn, minutesAgo: 120);
		await AddEvent(db, op1a.Id, OperatorEventType.CheckOut, minutesAgo: 10);
		await AddEvent(db, op2a.Id, OperatorEventType.CheckIn, minutesAgo: 60);

		var p2Result = await Controller(db).GetPresent(plant2.Id, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(p2Result.Result);
		var items = Assert.IsAssignableFrom<IEnumerable<OperatorDto>>(ok.Value).ToList();
		Assert.Single(items);
		Assert.Equal(op2a.Id, items[0].Id);
	}

	[Fact]
	public async Task Present_AllOperatorsAbsent_FilteredByPlant_ReturnsEmpty()
	{
		var (db, plant1, _, op1a, op1b, _, _) = await SeedTwoPlants();
		await AddEvent(db, op1a.Id, OperatorEventType.CheckIn, minutesAgo: 180);
		await AddEvent(db, op1a.Id, OperatorEventType.CheckOut, minutesAgo: 90);
		await AddEvent(db, op1b.Id, OperatorEventType.CheckIn, minutesAgo: 120);
		await AddEvent(db, op1b.Id, OperatorEventType.CheckOut, minutesAgo: 30);

		var result = await Controller(db).GetPresent(plant1.Id, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var items = Assert.IsAssignableFrom<IEnumerable<OperatorDto>>(ok.Value).ToList();
		Assert.Empty(items);
	}
}
