using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;
using OpenMES.WebApi.Controllers;
using Xunit;

namespace OpenMES.Tests;

public class MachinePhasePlacementControllerTests
{
    private static MachinePhasePlacementController Controller(OpenMES.Data.Contexts.OpenMESDbContext db)
        => new(db, NullLogger<MachinePhasePlacementController>.Instance);

    private static MachinePhasePlacementDto PlaceDto(int machineId, int phaseId, int operatorId) => new()
    {
        MachineId = machineId,
        ProductionOrderPhaseId = phaseId,
        PlacedByOperatorId = operatorId,
        PlacedAt = DateTimeOffset.UtcNow,
        Source = "Terminal",
    };

    private static async Task AddShift(OpenMES.Data.Contexts.OpenMESDbContext db, int operatorId, OperatorEventType type)
    {
        var now = DateTimeOffset.UtcNow;
        db.OperatorShifts.Add(new OperatorShift
        {
            OperatorId = operatorId,
            EventType = type,
            EventTime = now,
            Source = "Test",
            CreatedAt = now,
            UpdatedAt = now,
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Place_OperatorPresent_Succeeds()
    {
        var (db, machine, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests();
        await AddShift(db, op1.Id, OperatorEventType.CheckIn);

        var result = await Controller(db).Place(PlaceDto(machine.Id, phase.Id, op1.Id), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<MachinePhasePlacementDto>(ok.Value);
        Assert.Equal(machine.Id, dto.MachineId);
        Assert.Equal(phase.Id, dto.ProductionOrderPhaseId);
        Assert.Equal(MachinePhasePlacementStatus.Placed, dto.Status);
        Assert.Null(dto.UnplacedAt);
    }

    [Fact]
    public async Task Place_OperatorAbsent_ReturnsBadRequest()
    {
        var (db, machine, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests();

        var result = await Controller(db).Place(PlaceDto(machine.Id, phase.Id, op1.Id), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Place_PhaseOnDifferentWorkCenter_ReturnsBadRequest()
    {
        var (db, machine, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests();
        await AddShift(db, op1.Id, OperatorEventType.CheckIn);

        var wc2 = new WorkCenter
        {
            Code = "WC2",
            Description = "Work Center 2",
            PlantId = (await db.Plants.FirstAsync()).Id,
        };
        db.WorkCenters.Add(wc2);
        await db.SaveChangesAsync();

        phase.WorkCenterId = wc2.Id;
        await db.SaveChangesAsync();

        var result = await Controller(db).Place(PlaceDto(machine.Id, phase.Id, op1.Id), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Place_DuplicateOpenPlacement_ReturnsBadRequest()
    {
        var (db, machine, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests();
        await AddShift(db, op1.Id, OperatorEventType.CheckIn);

        var controller = Controller(db);
        var first = await controller.Place(PlaceDto(machine.Id, phase.Id, op1.Id), CancellationToken.None);
        Assert.IsType<OkObjectResult>(first.Result);

        var second = await controller.Place(PlaceDto(machine.Id, phase.Id, op1.Id), CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(second.Result);
    }

    [Fact]
    public async Task Unplace_OpenPlacement_Succeeds()
    {
        var (db, machine, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests();
        await AddShift(db, op1.Id, OperatorEventType.CheckIn);
        var controller = Controller(db);

        var placed = await controller.Place(PlaceDto(machine.Id, phase.Id, op1.Id), CancellationToken.None);
        var placedDto = Assert.IsType<MachinePhasePlacementDto>(Assert.IsType<OkObjectResult>(placed.Result).Value);

        var unplaced = await controller.Unplace(placedDto.Id, CancellationToken.None);
        var unplacedDto = Assert.IsType<MachinePhasePlacementDto>(Assert.IsType<OkObjectResult>(unplaced.Result).Value);

        Assert.Equal(MachinePhasePlacementStatus.Closed, unplacedDto.Status);
        Assert.NotNull(unplacedDto.UnplacedAt);
    }

    [Fact]
    public async Task GetOpenByMachine_ReturnsOnlyOpenPlacements()
    {
        var (db, machine, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests();
        await AddShift(db, op1.Id, OperatorEventType.CheckIn);
        var controller = Controller(db);

        var placed = await controller.Place(PlaceDto(machine.Id, phase.Id, op1.Id), CancellationToken.None);
        var placement = Assert.IsType<MachinePhasePlacementDto>(Assert.IsType<OkObjectResult>(placed.Result).Value);

        var open1 = await controller.GetOpenByMachine(machine.Id, CancellationToken.None);
        var items1 = Assert.IsAssignableFrom<IEnumerable<MachinePhasePlacementDto>>(Assert.IsType<OkObjectResult>(open1.Result).Value).ToList();
        Assert.Single(items1);

        await controller.Unplace(placement.Id, CancellationToken.None);

        var open2 = await controller.GetOpenByMachine(machine.Id, CancellationToken.None);
        var items2 = Assert.IsAssignableFrom<IEnumerable<MachinePhasePlacementDto>>(Assert.IsType<OkObjectResult>(open2.Result).Value).ToList();
        Assert.Empty(items2);
    }

    [Fact]
    public async Task StartSetup_FromPlaced_SetsInSetupAndOpensSetupSession()
    {
        var (db, machine, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests();
        await AddShift(db, op1.Id, OperatorEventType.CheckIn);
        var controller = Controller(db);

        var placed = await controller.Place(PlaceDto(machine.Id, phase.Id, op1.Id), CancellationToken.None);
        var placement = Assert.IsType<MachinePhasePlacementDto>(Assert.IsType<OkObjectResult>(placed.Result).Value);

        var started = await controller.StartSetup(placement.Id, op1.Id, CancellationToken.None);
        var dto = Assert.IsType<MachinePhasePlacementDto>(Assert.IsType<OkObjectResult>(started.Result).Value);

        Assert.Equal(MachinePhasePlacementStatus.InSetup, dto.Status);
        Assert.True(await db.WorkSessions.AnyAsync(x =>
            x.MachineId == machine.Id &&
            x.ProductionOrderPhaseId == phase.Id &&
            x.SessionType == WorkSessionType.Setup &&
            x.Status == WorkSessionStatus.Open));
    }

    [Fact]
    public async Task PauseSetup_FromInSetup_ClosesSetupSessionsAndSetsPaused()
    {
        var (db, machine, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests();
        await AddShift(db, op1.Id, OperatorEventType.CheckIn);
        var controller = Controller(db);

        var placed = await controller.Place(PlaceDto(machine.Id, phase.Id, op1.Id), CancellationToken.None);
        var placement = Assert.IsType<MachinePhasePlacementDto>(Assert.IsType<OkObjectResult>(placed.Result).Value);

        await controller.StartSetup(placement.Id, op1.Id, CancellationToken.None);
        var paused = await controller.PauseSetup(placement.Id, CancellationToken.None);
        var dto = Assert.IsType<MachinePhasePlacementDto>(Assert.IsType<OkObjectResult>(paused.Result).Value);

        Assert.Equal(MachinePhasePlacementStatus.SetupPaused, dto.Status);
        Assert.False(await db.WorkSessions.AnyAsync(x =>
            x.MachineId == machine.Id &&
            x.ProductionOrderPhaseId == phase.Id &&
            x.SessionType == WorkSessionType.Setup &&
            x.Status == WorkSessionStatus.Open));
    }

    [Fact]
    public async Task StartWork_FromSetupPaused_SetsInWorkAndOpensWorkSession()
    {
        var (db, machine, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests();
        await AddShift(db, op1.Id, OperatorEventType.CheckIn);
        var controller = Controller(db);

        var placed = await controller.Place(PlaceDto(machine.Id, phase.Id, op1.Id), CancellationToken.None);
        var placement = Assert.IsType<MachinePhasePlacementDto>(Assert.IsType<OkObjectResult>(placed.Result).Value);

        await controller.StartSetup(placement.Id, op1.Id, CancellationToken.None);
        await controller.PauseSetup(placement.Id, CancellationToken.None);

        var startedWork = await controller.StartWork(placement.Id, op1.Id, CancellationToken.None);
        var dto = Assert.IsType<MachinePhasePlacementDto>(Assert.IsType<OkObjectResult>(startedWork.Result).Value);

        Assert.Equal(MachinePhasePlacementStatus.InWork, dto.Status);
        Assert.True(await db.WorkSessions.AnyAsync(x =>
            x.MachineId == machine.Id &&
            x.ProductionOrderPhaseId == phase.Id &&
            x.SessionType == WorkSessionType.Work &&
            x.Status == WorkSessionStatus.Open));
    }

    [Fact]
    public async Task Close_WithOpenWorkSession_ReturnsBadRequest()
    {
        var (db, machine, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests();
        await AddShift(db, op1.Id, OperatorEventType.CheckIn);
        var controller = Controller(db);

        var placed = await controller.Place(PlaceDto(machine.Id, phase.Id, op1.Id), CancellationToken.None);
        var placement = Assert.IsType<MachinePhasePlacementDto>(Assert.IsType<OkObjectResult>(placed.Result).Value);

        await controller.StartWork(placement.Id, op1.Id, CancellationToken.None);

        var close = await controller.Close(placement.Id, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(close.Result);
    }

    [Fact]
    public async Task Close_FromWorkPaused_ClosesPlacement()
    {
        var (db, machine, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests();
        await AddShift(db, op1.Id, OperatorEventType.CheckIn);
        var controller = Controller(db);

        var placed = await controller.Place(PlaceDto(machine.Id, phase.Id, op1.Id), CancellationToken.None);
        var placement = Assert.IsType<MachinePhasePlacementDto>(Assert.IsType<OkObjectResult>(placed.Result).Value);

        await controller.StartWork(placement.Id, op1.Id, CancellationToken.None);
        await controller.PauseWork(placement.Id, CancellationToken.None);

        var close = await controller.Close(placement.Id, CancellationToken.None);
        var dto = Assert.IsType<MachinePhasePlacementDto>(Assert.IsType<OkObjectResult>(close.Result).Value);

        Assert.Equal(MachinePhasePlacementStatus.Closed, dto.Status);
        Assert.NotNull(dto.UnplacedAt);
    }
}
