using BA.Dmo.Domain.Shared.Access;

namespace BA.Dmo.Domain.Modules.JobOn;

/// <summary>
/// Immutable revision snapshot per N05 (TD-18). Each Save creates a NEW revision
/// - never UPDATE of saved revisions. Revision_id is pinned by downstream consumers
/// (Peso, Pegamentos) for historical attribution. image_asset_id is LOGICAL metadata only.
/// </summary>
public sealed record JobOnRevision
{
    /// <summary>Primary key.</summary>
    public Guid JobOnRevisionId { get; init; }

    /// <summary>Parent Job On ID.</summary>
    public Guid JobOnId { get; init; }

    /// <summary>Revision number (>= 1).</summary>
    public int RevisionNumber { get; init; }

    /// <summary>Optional production_snapshot JSON.</summary>
    public string? ProductionSnapshot { get; init; }

    /// <summary>Optional article reference snapshot.</summary>
    public string? ReferenceSnapshot { get; init; }

    /// <summary>Optional machine_snapshot.</summary>
    public string? MachineSnapshot { get; init; }

    /// <summary>Optional dates_snapshot.</summary>
    public string? DatesSnapshot { get; init; }

    /// <summary>JSONB: sections (secções de produção).</summary>
    public string Sections { get; init; } = "{}";

    /// <summary>Optional drop_count (gota).</summary>
    public decimal? DropCount { get; init; }

    /// <summary>Optional type_snapshot.</summary>
    public string? TypeSnapshot { get; init; }

    /// <summary>Optional stop_snapshot.</summary>
    public string? StopSnapshot { get; init; }

    /// <summary>Optional weight_snapshot (peso em gramas).</summary>
    public decimal? WeightSnapshot { get; init; }

    /// <summary>Optional process_snapshot (NNPB/PS from Peso lot).</summary>
    public string? ProcessSnapshot { get; init; }

    /// <summary>General notes field.</summary>
    public string? GeneralNotes { get; init; }

    /// <summary>image_asset_id - LOGICAL metadata ONLY, not binary (TD-23).</summary>
    public string? ImageAssetId { get; init; }

    /// <summary>Change reason (mandatory when editing closed revision).</summary>
    public string? ChangeReason { get; init; }

    /// <summary>Actor who saved this revision.</summary>
    public string? SavedBy { get; init; }

    /// <summary>Saved timestamp.</summary>
    public DateTime SavedAtUtc { get; init; }

    /// <summary>Components collection loaded separately.</summary>
    public IReadOnlyList<JobOnComponent>? Components { get; init; }

    /// <summary>Verifications collection loaded separately.</summary>
    public IReadOnlyList<JobOnVerificationOccurrence>? Verifications { get; init; }

    /// <summary>Clockwise rotation: clone this revision with modifications.</summary>
    public JobOnRevision CloneWithChanges(
        string? generalNotes = null,
        string? changeReason = null,
        string? imageAssetId = null,
        IReadOnlyList<JobOnComponent>? newComponents = null,
        IReadOnlyList<JobOnVerificationOccurrence>? newVerifications = null)
    {
        return new JobOnRevision
        {
            JobOnRevisionId = Guid.NewGuid(),
            JobOnId = this.JobOnId,
            RevisionNumber = this.RevisionNumber + 1,
            ProductionSnapshot = this.ProductionSnapshot,
            ReferenceSnapshot = this.ReferenceSnapshot,
            MachineSnapshot = this.MachineSnapshot,
            DatesSnapshot = this.DatesSnapshot,
            Sections = this.Sections,
            DropCount = this.DropCount,
            TypeSnapshot = this.TypeSnapshot,
            StopSnapshot = this.StopSnapshot,
            WeightSnapshot = this.WeightSnapshot,
            ProcessSnapshot = this.ProcessSnapshot,
            GeneralNotes = generalNotes ?? this.GeneralNotes,
            ImageAssetId = imageAssetId ?? this.ImageAssetId,
            ChangeReason = changeReason,
            SavedBy = this.SavedBy,
            SavedAtUtc = DateTime.UtcNow,
            Components = newComponents ?? this.Components,
            Verifications = newVerifications ?? this.Verifications
        };
    }
}
