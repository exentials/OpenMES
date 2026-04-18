using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OpenMES.Data.Common;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;
using OpenMES.WebApi.Controllers;

namespace OpenMES.Tests;

public class ErpExportTests
{
    private static ErpExportController Controller(OpenMESDbContext db)
        => new(db, NullLogger<ErpExportController>.Instance);

    private static async Task<WorkSession> AddClosedSession(
        OpenMESDbContext db, int opId, int phaseId, int machineId,
        string? externalCounterId = null, bool isReversal = false, int? reversalOfId = null,
        int? reversedById = null, decimal minutes = 60)
    {
        var now = DateTimeOffset.UtcNow;
        var s = new WorkSession
        {
            OperatorId = opId, ProductionOrderPhaseId = phaseId, MachineId = machineId,
            SessionType = WorkSessionType.Work, Status = WorkSessionStatus.Closed,
            StartTime = now.AddMinutes((double)-minutes), EndTime = now,
            AllocatedMinutes = minutes, Source = "Manual",
            PhaseExternalId = "EXT-0010",
            ExternalCounterId = externalCounterId,
            IsReversal = isReversal, ReversalOfId = reversalOfId, ReversedById = reversedById,
            CreatedAt = now, UpdatedAt = now,
        };
        db.WorkSessions.Add(s);
        await db.SaveChangesAsync();
        return s;
    }

    private static async Task<ProductionDeclaration> AddDeclaration(
        OpenMESDbContext db, int opId, int phaseId, int machineId,
        decimal confirmed = 10, string? externalCounterId = null,
        bool isReversal = false, int? reversalOfId = null, int? reversedById = null)
    {
        var now = DateTimeOffset.UtcNow;
        var d = new ProductionDeclaration
        {
            OperatorId = opId, ProductionOrderPhaseId = phaseId, MachineId = machineId,
            DeclarationDate = now, ConfirmedQuantity = confirmed, ScrapQuantity = 0,
            PhaseExternalId = "EXT-0010",
            ExternalCounterId = externalCounterId,
            IsReversal = isReversal, ReversalOfId = reversalOfId, ReversedById = reversedById,
            CreatedAt = now, UpdatedAt = now,
        };
        db.ProductionDeclarations.Add(d);
        await db.SaveChangesAsync();
        return d;
    }

    // Group WS - WorkSession export

    [Fact]
    public async Task WS1_ExportWorkSessions_NoPending_ReturnsEmptyResult()
    {
        var (db, _, _, _, _, _) = await TestDbFactory.SeedForErpExportTests();

        var result = await Controller(db).ExportWorkSessions(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ErpExportResultDto>(ok.Value);
        Assert.Equal(0, dto.ExportedCount);
        Assert.Empty(dto.Rows);
    }

    [Fact]
    public async Task WS2_ExportWorkSessions_OnePendingSession_ReturnsOneRow()
    {
        var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForErpExportTests();
        var session = await AddClosedSession(db, op1.Id, phase.Id, machine.Id);

        var result = await Controller(db).ExportWorkSessions(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ErpExportResultDto>(ok.Value);
        Assert.Equal(1, dto.ExportedCount);
        Assert.Single(dto.Rows);
        var row = dto.Rows[0];
        Assert.Equal("WorkSession", row.EntityType);
        Assert.Equal(session.Id, row.RecordId);
        Assert.Equal("EXT-0010", row.PhaseExternalId);
    }

    [Fact]
    public async Task WS3_ExportWorkSessions_SetsErpExportedAt()
    {
        var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForErpExportTests();
        var session = await AddClosedSession(db, op1.Id, phase.Id, machine.Id);

        await Controller(db).ExportWorkSessions(CancellationToken.None);

        var reloaded = await db.WorkSessions.FindAsync(session.Id);
        Assert.NotNull(reloaded);
        Assert.NotNull(reloaded!.ErpExportedAt);
    }

    [Fact]
    public async Task WS4_ExportWorkSessions_ExcludesAlreadyExported()
    {
        var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForErpExportTests();
        var pending = await AddClosedSession(db, op1.Id, phase.Id, machine.Id, externalCounterId: null);
        await AddClosedSession(db, op1.Id, phase.Id, machine.Id, externalCounterId: "ERP-001");

        var result = await Controller(db).ExportWorkSessions(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ErpExportResultDto>(ok.Value);
        Assert.Equal(1, dto.ExportedCount);
        Assert.Equal(pending.Id, dto.Rows.Single().RecordId);
    }

    [Fact]
    public async Task WS5_ExportWorkSessions_ExcludesReversedSessions()
    {
        var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForErpExportTests();
        await AddClosedSession(db, op1.Id, phase.Id, machine.Id, reversedById: null);
        await AddClosedSession(db, op1.Id, phase.Id, machine.Id, reversedById: 1);

        var result = await Controller(db).ExportWorkSessions(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ErpExportResultDto>(ok.Value);
        Assert.Equal(1, dto.ExportedCount);
    }

    [Fact]
    public async Task WS6_ExportWorkSessions_ExcludesOpenSessions()
    {
        var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForErpExportTests();
        var now = DateTimeOffset.UtcNow;
        db.WorkSessions.Add(new WorkSession
        {
            OperatorId = op1.Id, ProductionOrderPhaseId = phase.Id, MachineId = machine.Id,
            SessionType = WorkSessionType.Work, Status = WorkSessionStatus.Open,
            StartTime = now.AddMinutes(-30), AllocatedMinutes = 0, Source = "Manual",
            CreatedAt = now, UpdatedAt = now,
        });
        await db.SaveChangesAsync();

        var result = await Controller(db).ExportWorkSessions(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ErpExportResultDto>(ok.Value);
        Assert.Equal(0, dto.ExportedCount);
    }

    [Fact]
    public async Task WS7_ExportWorkSessions_ReversalRow_IncludesReversalOfExternalCounterId()
    {
        var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForErpExportTests();
        var original = await AddClosedSession(db, op1.Id, phase.Id, machine.Id,
            externalCounterId: "ERP-ORIG", reversedById: 999);
        var reversal = await AddClosedSession(db, op1.Id, phase.Id, machine.Id,
            isReversal: true, reversalOfId: original.Id);

        var result = await Controller(db).ExportWorkSessions(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ErpExportResultDto>(ok.Value);
        var row = dto.Rows.Single(r => r.RecordId == reversal.Id);
        Assert.True(row.IsReversal);
        Assert.Equal("ERP-ORIG", row.ReversalOfExternalCounterId);
    }

    // Group WS-C - WorkSession confirm

    [Fact]
    public async Task WSC1_ConfirmWorkSessions_EmptyItems_ReturnsBadRequest()
    {
        var (db, _, _, _, _, _) = await TestDbFactory.SeedForErpExportTests();

        var result = await Controller(db).ConfirmWorkSessions(
            new ErpConfirmationDto { Items = [] }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task WSC2_ConfirmWorkSessions_ValidItem_SetsExternalCounterId()
    {
        var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForErpExportTests();
        var session = await AddClosedSession(db, op1.Id, phase.Id, machine.Id);
        await Controller(db).ExportWorkSessions(CancellationToken.None);

        var result = await Controller(db).ConfirmWorkSessions(new ErpConfirmationDto
        {
            Items = [new ErpConfirmationItemDto { RecordId = session.Id, ExternalCounterId = "ERP-CNF-1" }]
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(1, ok.Value);
        var reloaded = await db.WorkSessions.FindAsync(session.Id);
        Assert.Equal("ERP-CNF-1", reloaded!.ExternalCounterId);
    }

    [Fact]
    public async Task WSC3_ConfirmWorkSessions_AlreadyConfirmed_IsSkipped()
    {
        var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForErpExportTests();
        var session = await AddClosedSession(db, op1.Id, phase.Id, machine.Id, externalCounterId: "ALREADY-SET");

        var result = await Controller(db).ConfirmWorkSessions(new ErpConfirmationDto
        {
            Items = [new ErpConfirmationItemDto { RecordId = session.Id, ExternalCounterId = "NEW" }]
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(0, ok.Value);
        var reloaded = await db.WorkSessions.FindAsync(session.Id);
        Assert.Equal("ALREADY-SET", reloaded!.ExternalCounterId);
    }

    [Fact]
    public async Task WSC4_ConfirmWorkSessions_UnknownId_IsSkipped()
    {
        var (db, _, _, _, _, _) = await TestDbFactory.SeedForErpExportTests();

        var result = await Controller(db).ConfirmWorkSessions(new ErpConfirmationDto
        {
            Items = [new ErpConfirmationItemDto { RecordId = 99999, ExternalCounterId = "X" }]
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(0, ok.Value);
    }

    // Group PD - ProductionDeclaration export

    [Fact]
    public async Task PD1_ExportDeclarations_NoPending_ReturnsEmptyResult()
    {
        var (db, _, _, _, _, _) = await TestDbFactory.SeedForErpExportTests();

        var result = await Controller(db).ExportProductionDeclarations(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ErpExportResultDto>(ok.Value);
        Assert.Equal(0, dto.ExportedCount);
    }

    [Fact]
    public async Task PD2_ExportDeclarations_OnePending_ReturnsOneRow()
    {
        var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForErpExportTests();
        await AddDeclaration(db, op1.Id, phase.Id, machine.Id, confirmed: 10);

        var result = await Controller(db).ExportProductionDeclarations(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ErpExportResultDto>(ok.Value);
        Assert.Equal(1, dto.ExportedCount);
        var row = dto.Rows.Single();
        Assert.Equal("ProductionDeclaration", row.EntityType);
        Assert.Equal(10m, row.ConfirmedQuantity);
    }

    [Fact]
    public async Task PD3_ExportDeclarations_ExcludesAlreadyExported()
    {
        var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForErpExportTests();
        await AddDeclaration(db, op1.Id, phase.Id, machine.Id, externalCounterId: null);
        await AddDeclaration(db, op1.Id, phase.Id, machine.Id, externalCounterId: "ERP-1");

        var result = await Controller(db).ExportProductionDeclarations(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ErpExportResultDto>(ok.Value);
        Assert.Equal(1, dto.ExportedCount);
    }

    [Fact]
    public async Task PD4_ExportDeclarations_ExcludesReversed()
    {
        var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForErpExportTests();
        await AddDeclaration(db, op1.Id, phase.Id, machine.Id, reversedById: null);
        await AddDeclaration(db, op1.Id, phase.Id, machine.Id, reversedById: 99);

        var result = await Controller(db).ExportProductionDeclarations(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ErpExportResultDto>(ok.Value);
        Assert.Equal(1, dto.ExportedCount);
    }

    [Fact]
    public async Task PD5_ExportDeclarations_ReversalRow_IncludesReversalOfExternalCounterId()
    {
        var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForErpExportTests();
        var original = await AddDeclaration(db, op1.Id, phase.Id, machine.Id,
            externalCounterId: "ERP-ORIG", reversedById: 999);
        var reversal = await AddDeclaration(db, op1.Id, phase.Id, machine.Id,
            isReversal: true, reversalOfId: original.Id);

        var result = await Controller(db).ExportProductionDeclarations(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ErpExportResultDto>(ok.Value);
        var row = dto.Rows.Single(r => r.RecordId == reversal.Id);
        Assert.True(row.IsReversal);
        Assert.Equal("ERP-ORIG", row.ReversalOfExternalCounterId);
    }

    // Group PD-C - ProductionDeclaration confirm

    [Fact]
    public async Task PDC1_ConfirmDeclarations_EmptyItems_ReturnsBadRequest()
    {
        var (db, _, _, _, _, _) = await TestDbFactory.SeedForErpExportTests();

        var result = await Controller(db).ConfirmProductionDeclarations(
            new ErpConfirmationDto { Items = [] }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task PDC2_ConfirmDeclarations_ValidItem_SetsExternalCounterId()
    {
        var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForErpExportTests();
        var decl = await AddDeclaration(db, op1.Id, phase.Id, machine.Id);
        await Controller(db).ExportProductionDeclarations(CancellationToken.None);

        var result = await Controller(db).ConfirmProductionDeclarations(new ErpConfirmationDto
        {
            Items = [new ErpConfirmationItemDto { RecordId = decl.Id, ExternalCounterId = "ERP-D-1" }]
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(1, ok.Value);
        var reloaded = await db.ProductionDeclarations.FindAsync(decl.Id);
        Assert.Equal("ERP-D-1", reloaded!.ExternalCounterId);
    }

    [Fact]
    public async Task PDC3_ConfirmDeclarations_AlreadyConfirmed_IsSkipped()
    {
        var (db, machine, op1, phase, _, _) = await TestDbFactory.SeedForErpExportTests();
        var decl = await AddDeclaration(db, op1.Id, phase.Id, machine.Id, externalCounterId: "ALREADY");

        var result = await Controller(db).ConfirmProductionDeclarations(new ErpConfirmationDto
        {
            Items = [new ErpConfirmationItemDto { RecordId = decl.Id, ExternalCounterId = "NEW" }]
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(0, ok.Value);
        var reloaded = await db.ProductionDeclarations.FindAsync(decl.Id);
        Assert.Equal("ALREADY", reloaded!.ExternalCounterId);
    }

    [Fact]
    public async Task PDC4_ConfirmDeclarations_UnknownId_IsSkipped()
    {
        var (db, _, _, _, _, _) = await TestDbFactory.SeedForErpExportTests();

        var result = await Controller(db).ConfirmProductionDeclarations(new ErpConfirmationDto
        {
            Items = [new ErpConfirmationItemDto { RecordId = 99999, ExternalCounterId = "X" }]
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(0, ok.Value);
    }
}
