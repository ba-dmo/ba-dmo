using System.Data;
using BA.Dmo.Domain.Modules.JobOn;
using BA.Dmo.Infrastructure.Persistence;
using BA.Dmo.Application.Modules.JobOn;
using BA.Dmo.Application.Shared.Persistence;
using Dapper;

using JobOnEntity = BA.Dmo.Domain.Modules.JobOn.JobOn;

namespace BA.Dmo.Infrastructure.Access;

/// <summary>
/// U-13 — Job On Dapper persistence (N05, TD-18). Implements IJobOnRepository port.
/// All CRUD operations map exactly to job_on* tables from migration N05.
/// </summary>
public sealed class DapperJobOnRepository : IJobOnRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DapperJobOnRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory
            ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<Guid> CreateAsync(JobOn jobOn, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO job_on (
    production_code, machine_code, planned_start_at, planned_end_at,
    lifecycle_state, copied_from_job_on_id, article_reference_id,
    created_at_utc)
VALUES (
    @ProductionCode, @MachineCode, @PlannedStartAt, @PlannedEndAt,
    @LifecycleState, @CopiedFromJobOnId, @ArticleReferenceId,
    @CreatedAtUtc)
RETURNING job_on_id;";

        var parameters = new DynamicParameters();
        parameters.Add("@ProductionCode", jobOn.ProductionCode);
        parameters.Add("@MachineCode", jobOn.MachineCode);
        parameters.Add("@PlannedStartAt", (object?)jobOn.PlannedStartAt ?? DBNull.Value);
        parameters.Add("@PlannedEndAt", (object?)jobOn.PlannedEndAt ?? DBNull.Value);
        parameters.Add("@LifecycleState", jobOn.LifecycleState.ToString().ToLowerInvariant());
        parameters.Add("@CopiedFromJobOnId", (object?)jobOn.CopiedFromJobOnId ?? DBNull.Value);
        parameters.Add("@ArticleReferenceId", (object?)jobOn.ArticleReferenceId ?? DBNull.Value);
        parameters.Add("@CreatedAtUtc", DateTime.UtcNow);

        var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        try
        {
            var id = await Db.ExecuteScalarAsync<Guid>(connection, sql, parameters, cancellationToken: cancellationToken);
            return id;
        }
        finally
        {
            await DisposeAsync(connection);
        }
    }

    public async Task<JobOn?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT 
    job_on_id,
    production_code,
    machine_code,
    planned_start_at,
    planned_end_at,
    lifecycle_state,
    current_revision_id,
    copied_from_job_on_id,
    article_reference_id,
    created_at_utc
FROM job_on 
WHERE job_on_id = @Id;";
        
        var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        try
        {
            var row = await Db.QuerySingleOrDefaultAsync<dynamic>(connection, sql, new { Id = id }, cancellationToken: cancellationToken);
            
            if (row == null) return null;

            var revisions = await GetRevisionsAsyncInternal(id, cancellationToken);
            
            var jobOn = new JobOnEntity(
                row.production_code!,
                row.machine_code!,
                row.planned_start_at?.ToDateTimeOffset(),
                row.planned_end_at?.ToDateTimeOffset(),
                revisions);
            jobOn.FromRow(row);
            
            return jobOn;
        }
        finally
        {
            await DisposeAsync(connection);
        }
    }

    public async Task<IReadOnlyList<JobOn>> GetActiveAsync(string machineCode, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var sql = @"
SELECT 
    job_on_id,
    production_code,
    machine_code,
    planned_start_at,
    planned_end_at,
    lifecycle_state,
    current_revision_id,
    copied_from_job_on_id,
    article_reference_id,
    created_at_utc
FROM job_on 
WHERE machine_code = @MachineCode 
  AND lifecycle_state IN ('planeado', 'em_fabrico')
" + (from.HasValue ? "AND planned_start_at >= @From" : "") +
  (to.HasValue ? "AND (planned_end_at IS NULL OR planned_end_at <= @To)" : "");

        var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        try
        {
            var rows = await Db.QueryAsync<dynamic>(connection, sql, new { MachineCode = machineCode, From = from, To = to }, cancellationToken: cancellationToken);

            return rows.Select(r => MapJobOn(r)).ToList().AsReadOnly();
        }
        finally
        {
            await DisposeAsync(connection);
        }
    }

    public async Task<JobOn?> GetByProductionCodeAsync(string productionCode, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT 
    job_on_id,
    production_code,
    machine_code,
    planned_start_at,
    planned_end_at,
    lifecycle_state,
    current_revision_id,
    copied_from_job_on_id,
    article_reference_id,
    created_at_utc
FROM job_on 
WHERE production_code = @ProductionCode;";
        
        var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        try
        {
            var row = await Db.QuerySingleOrDefaultAsync<dynamic>(connection, sql, new { ProductionCode = productionCode }, cancellationToken: cancellationToken);
            
            if (row == null) return null;

            var revisions = await GetRevisionsAsyncInternal(row.job_on_id, cancellationToken);
            return MapJobOn(row, revisions);
        }
        finally
        {
            await DisposeAsync(connection);
        }
    }

    public async Task UpdateLifecycleStateAsync(Guid id, JobOnLifecycleState newState, string actorId, CancellationToken cancellationToken = default)
    {
        const string sql = @"UPDATE job_on SET lifecycle_state = @NewState WHERE job_on_id = @Id;";
        
        var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        try
        {
            await Db.ExecuteAsync(connection, sql, new { NewState = newState.ToString().ToLowerInvariant(), Id = id }, cancellationToken: cancellationToken);
        }
        finally
        {
            await DisposeAsync(connection);
        }
    }

    public async Task InsertRevisionAsync(JobOnRevision revision, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO job_on_revision (
    job_on_revision_id, job_on_id, revision_number, sections, drop_count,
    general_notes, image_asset_id, change_reason, saved_by, saved_at_utc)
VALUES (
    @JobOnRevisionId, @JobOnId, @RevisionNumber, @Sections, @DropCount,
    @GeneralNotes, @ImageAssetId, @ChangeReason, @SavedBy, @SavedAtUtc);";

        var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        try
        {
            await Db.ExecuteAsync(connection, sql, new
            {
                revision.JobOnRevisionId,
                revision.JobOnId,
                revision.RevisionNumber,
                Sections = revision.Sections,
                DropCount = (object?)revision.DropCount ?? DBNull.Value,
                GeneralNotes = revision.GeneralNotes,
                ImageAssetId = (object?)revision.ImageAssetId ?? DBNull.Value,
                ChangeReason = revision.ChangeReason,
                SavedBy = revision.SavedBy,
                SavedAtUtc = revision.SavedAtUtc
            }, cancellationToken: cancellationToken);
        }
        finally
        {
            await DisposeAsync(connection);
        }
    }

    public async Task<IReadOnlyList<JobOnRevision>> GetRevisionsAsync(Guid jobOnId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT 
    job_on_revision_id,
    job_on_id,
    revision_number,
    sections,
    drop_count,
    general_notes,
    image_asset_id,
    change_reason,
    saved_by,
    saved_at_utc
FROM job_on_revision 
WHERE job_on_id = @JobOnId 
ORDER BY revision_number ASC;";
        
        var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        try
        {
            var rows = await Db.QueryAsync<dynamic>(connection, sql, new { JobOnId = jobOnId }, cancellationToken: cancellationToken);
            
            return rows.Select(r => MapRevision(r)).ToList();
        }
        finally
        {
            await DisposeAsync(connection);
        }
    }

    public async Task InsertComponentsAsync(IEnumerable<JobOnComponent> components, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO job_on_component (
    job_on_component_id, job_on_revision_id, family, source_tool_id, source_lot_id,
    reference_snapshot, lot_snapshot, technical_name_snapshot, planned_quantity,
    stock_snapshot, usage_snapshot, notes, display_order)
VALUES (
    @JobOnComponentId, @JobOnRevisionId, @Family, @SourceToolId, @SourceLotId,
    @ReferenceSnapshot, @LotSnapshot, @TechnicalNameSnapshot, @PlannedQuantity,
    @StockSnapshot, @UsageSnapshot, @Notes, @DisplayOrder);";

        var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        try
        {
            foreach (var component in components)
            {
                await Db.ExecuteAsync(connection, sql, new
                {
                    component.JobOnComponentId,
                    component.JobOnRevisionId,
                    Family = component.Family.ToString(),
                    SourceToolId = (object?)component.SourceToolId ?? DBNull.Value,
                    SourceLotId = (object?)component.SourceLotId ?? DBNull.Value,
                    ReferenceSnapshot = component.ReferenceSnapshot,
                    LotSnapshot = component.LotSnapshot,
                    TechnicalNameSnapshot = component.TechnicalNameSnapshot,
                    PlannedQuantity = (object?)component.PlannedQuantity ?? DBNull.Value,
                    StockSnapshot = (object?)component.StockSnapshot ?? DBNull.Value,
                    UsageSnapshot = (object?)component.UsageSnapshot ?? DBNull.Value,
                    Notes = component.Notes,
                    DisplayOrder = component.DisplayOrder
                }, cancellationToken: cancellationToken);
            }
        }
        finally
        {
            await DisposeAsync(connection);
        }
    }

    public async Task InsertFieldsAsync(IEnumerable<JobOnComponentField> fields, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO job_on_component_field (
    job_on_component_field_id, job_on_component_id, field_key, value_type,
    value_text, value_integer, value_decimal, value_boolean, value_date, display_order)
VALUES (
    @JobOnComponentFieldId, @JobOnComponentId, @FieldKey, @ValueType,
    @ValueText, @ValueInteger, @ValueDecimal, @ValueBoolean, @ValueDate, @DisplayOrder);";

        var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        try
        {
            foreach (var field in fields)
            {
                await Db.ExecuteAsync(connection, sql, new
                {
                    field.JobOnComponentFieldId,
                    field.JobOnComponentId,
                    FieldKey = field.FieldKey,
                    ValueType = field.ValueType,
                    ValueText = (object?)field.ValueText ?? DBNull.Value,
                    ValueInteger = (object?)field.ValueInteger ?? DBNull.Value,
                    ValueDecimal = (object?)field.ValueDecimal ?? DBNull.Value,
                    ValueBoolean = (object?)field.ValueBoolean ?? DBNull.Value,
                    ValueDate = (object?)field.ValueDate ?? DBNull.Value,
                    DisplayOrder = field.DisplayOrder
                }, cancellationToken: cancellationToken);
            }
        }
        finally
        {
            await DisposeAsync(connection);
        }
    }

    public async Task InsertRowsAsync(IEnumerable<JobOnComponentRow> rows, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO job_on_component_row (
    job_on_component_row_id, job_on_component_id, element_label, value_decimal,
    value_text, unit, machine_quantity, display_order)
VALUES (
    @JobOnComponentRowId, @JobOnComponentId, @ElementLabel, @ValueDecimal,
    @ValueText, @Unit, @MachineQuantity, @DisplayOrder);";

        var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        try
        {
            foreach (var rowEntity in rows)
            {
                await Db.ExecuteAsync(connection, sql, new
                {
                    rowEntity.JobOnComponentRowId,
                    rowEntity.JobOnComponentId,
                    ElementLabel = rowEntity.ElementLabel,
                    ValueDecimal = (object?)rowEntity.ValueDecimal ?? DBNull.Value,
                    ValueText = (object?)rowEntity.ValueText ?? DBNull.Value,
                    Unit = rowEntity.Unit,
                    MachineQuantity = (object?)rowEntity.MachineQuantity ?? DBNull.Value,
                    DisplayOrder = rowEntity.DisplayOrder
                }, cancellationToken: cancellationToken);
            }
        }
        finally
        {
            await DisposeAsync(connection);
        }
    }

    public async Task InsertVerificationsAsync(IEnumerable<JobOnVerificationOccurrence> verifications, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO job_on_verification_occurrence (
    job_on_verification_occurrence_id, job_on_component_id, source_rule_id,
    rule_text_snapshot, status, completed_by, completed_at_utc, created_at_utc)
VALUES (
    @JobOnVerificationOccurrenceId, @JobOnComponentId, @SourceRuleId,
    @RuleTextSnapshot, @Status, @CompletedBy, @CompletedAtUtc, @CreatedAtUtc);";

        var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
try
        {
            foreach (var v in verifications)
            {
                await Db.ExecuteAsync(connection, sql, new
                {
                    v.JobOnVerificationOccurrenceId,
                    v.JobOnComponentId,
                    SourceRuleId = (object?)v.SourceRuleId ?? DBNull.Value,
                    RuleTextSnapshot = v.RuleTextSnapshot,
                    Status = v.Status,
                    CompletedBy = (object?)v.CompletedBy ?? DBNull.Value,
                    CompletedAtUtc = (object?)v.CompletedAtUtc ?? DBNull.Value,
                    CreatedAtUtc = v.CreatedAtUtc
                }, cancellationToken: cancellationToken);
            }
        }
        finally
        {
            await DisposeAsync(connection);
        }
    }

    public async Task UpdateVerificationStatusAsync(Guid occurrenceId, string status, string? completedBy, DateTime? completedAtUtc, CancellationToken cancellationToken = default)
    {
        const string sql = @"
UPDATE job_on_verification_occurrence 
SET status = @Status, completed_by = @CompletedBy, completed_at_utc = @CompletedAtUtc, updated_at_utc = @UpdatedUtc
WHERE job_on_verification_occurrence_id = @OccurrenceId;";

        var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        try
        {
            await Db.ExecuteAsync(connection, sql, new
            {
                OccurrenceId = occurrenceId,
                Status = status,
                CompletedBy = (object?)completedBy ?? DBNull.Value,
                CompletedAtUtc = (object?)completedAtUtc ?? DBNull.Value,
                UpdatedUtc = DateTime.UtcNow
            }, cancellationToken: cancellationToken);
        }
        finally
        {
            await DisposeAsync(connection);
        }
    }

    public async Task<Guid?> GetCurrentRevisionIdAsync(Guid jobOnId, CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT current_revision_id FROM job_on WHERE job_on_id = @JobOnId;";
        
        var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        try
        {
            var result = await Db.ExecuteScalarAsync<Guid?>(connection, sql, new { JobOnId = jobOnId }, cancellationToken: cancellationToken);
            return result;
        }
        finally
        {
            await DisposeAsync(connection);
        }
    }

    public async Task UpdateCurrentRevisionAsync(Guid jobOnId, Guid revisionId, CancellationToken cancellationToken = default)
    {
        const string sql = @"UPDATE job_on SET current_revision_id = @RevisionId WHERE job_on_id = @JobOnId;";
        
        var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        try
        {
            await Db.ExecuteAsync(connection, sql, new { RevisionId = revisionId, JobOnId = jobOnId }, cancellationToken: cancellationToken);
        }
        finally
        {
            await DisposeAsync(connection);
        }
    }

    public async Task InsertAuditEventAsync(Guid jobId, Guid? revisionId, string eventType, string? beforeSnapshot, string? afterSnapshot, string actorId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO job_on_audit_event (job_on_id, job_on_revision_id, event_type, before_snapshot, after_snapshot, actor_id, occurred_at_utc)
VALUES (@JobId, @RevisionId, @EventType, @BeforeSnapshot, @AfterSnapshot, @ActorId, @OccurredAtUtc);";

        var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        try
        {
            await Db.ExecuteAsync(connection, sql, new
            {
                JobId = jobId,
                RevisionId = (object?)revisionId ?? DBNull.Value,
                EventType = eventType,
                BeforeSnapshot = (object?)beforeSnapshot ?? DBNull.Value,
                AfterSnapshot = (object?)afterSnapshot ?? DBNull.Value,
                ActorId = actorId,
                OccurredAtUtc = DateTime.UtcNow
            }, cancellationToken: cancellationToken);
        }
        finally
        {
            await DisposeAsync(connection);
        }
    }

    public async Task<IReadOnlyList<HistoricalProductionSummary>> GetHistoricalProductionsAsync(string? referenceFilter, string? machineFilter, DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        var sql = @"
SELECT 
    jo.job_on_id,
    jo.production_code,
    jo.machine_code,
    jo.planned_start_at,
    jo.planned_end_at,
    jc.reference_snapshot as reference_code,
    jr.revision_number as current_revision_number,
    COUNT(jr2.job_on_revision_id) as total_revision_count,
    jo.lifecycle_state
FROM job_on jo
LEFT JOIN job_on_component jc ON jc.job_on_revision_id = (
    SELECT job_on_revision_id FROM job_on_revision WHERE job_on_id = jo.job_on_id ORDER BY revision_number DESC LIMIT 1
)
LEFT JOIN job_on_revision jr ON jr.job_on_id = jo.job_on_id AND jr.revision_number = (
    SELECT MAX(revision_number) FROM job_on_revision WHERE job_on_id = jo.job_on_id
)
LEFT JOIN job_on_revision jr2 ON jr2.job_on_id = jo.job_on_id
WHERE 1=1" +
        (string.IsNullOrWhiteSpace(referenceFilter) ? "" : " AND jc.reference_snapshot ILIKE @RefFilter") +
        (string.IsNullOrWhiteSpace(machineFilter) ? "" : " AND jo.machine_code = @MachineFilter") +
        (from.HasValue ? " AND jo.planned_start_at >= @From" : "") +
        (to.HasValue ? " AND jo.planned_start_at <= @To" : "");

        var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        try
        {
            var rows = await Db.QueryAsync<HistoricalProductionSummary>(connection, sql, new
            {
                RefFilter = $"%{referenceFilter}%",
                MachineFilter = machineFilter,
                From = from,
                To = to
            }, cancellationToken: cancellationToken);

            return rows.ToList();
        }
        finally
        {
            await DisposeAsync(connection);
        }
    }

    private JobOn MapJobOn(dynamic row, IReadOnlyList<JobOnRevision>? revisions = null)
    {
        var rvs = revisions ?? new List<JobOnRevision>();
                var jobOn = new JobOnEntity(
            row.production_code!,
            row.machine_code!,
            row.planned_start_at?.ToDateTimeOffset(),
            row.planned_end_at?.ToDateTimeOffset(),
            rvs);
        jobOn.FromRow(row);
        return jobOn;
    }

    private async Task<IReadOnlyList<JobOnRevision>> GetRevisionsAsyncInternal(Guid jobOnId, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT 
    job_on_revision_id,
    job_on_id,
    revision_number,
    sections,
    drop_count,
    general_notes,
    image_asset_id,
    change_reason,
    saved_by,
    saved_at_utc
FROM job_on_revision 
WHERE job_on_id = @JobOnId 
ORDER BY revision_number ASC;";
        
        var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        try
        {
            var rows = await Db.QueryAsync<dynamic>(connection, sql, new { JobOnId = jobOnId }, cancellationToken: cancellationToken);
            return rows.Select(r => MapRevision(r)).ToList().AsReadOnly();
        }
        finally
        {
            await DisposeAsync(connection);
        }
    }

    private JobOnRevision MapRevision(dynamic row)
    {
        return new JobOnRevision
        {
            JobOnRevisionId = row.job_on_revision_id,
            JobOnId = row.job_on_id,
            RevisionNumber = row.revision_number,
            Sections = row.sections ?? "{}",
            DropCount = row.drop_count,
            GeneralNotes = row.general_notes,
            ImageAssetId = row.image_asset_id,
            ChangeReason = row.change_reason,
            SavedBy = row.saved_by,
            SavedAtUtc = row.saved_at_utc
        };
    }

    private static async Task DisposeAsync(System.Data.IDbConnection connection)
    {
        if (connection is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else
            connection.Dispose();
    }
}
