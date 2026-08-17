namespace BA.Dmo.Domain.Modules.JobOn;

/// <summary>
/// Job On lifecycle state per N05 (TD-27, modules/05 §4).
/// </summary>
public enum JobOnLifecycleState
{
    /// <summary>Rascunho – new creation, not yet planned.</summary>
    Rascunho,
    
    /// <summary>Planeado – saved with planned dates, active in calendar.</summary>
    Planeado,
    
    /// <summary>Em fabrico – production started.</summary>
    EmFabrico,
    
    /// <summary>Fechado – production completed.</summary>
    Fechado,
    
    /// <summary>Cancelado – production cancelled.</summary>
    Cancelado
}
