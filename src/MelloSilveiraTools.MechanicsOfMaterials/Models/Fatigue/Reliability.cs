namespace MelloSilveiraTools.MechanicsOfMaterials.Models.Fatigue;

/// <summary>
/// It contains the reliability for project to fatigue analysis.
/// </summary>
public enum Reliability : int
{
    /// <summary>
    /// 50 per cent.
    /// </summary>
    Fifty = 50,

    /// <summary>
    /// 90 per cent.
    /// </summary>
    Ninety = 90,

    /// <summary>
    /// 95 per cent.
    /// </summary>
    NinetyFive = 95,

    /// <summary>
    /// 99 per cent.
    /// </summary>
    NinetyNine = 99,

    /// <summary>
    /// 99.9 per cent.
    /// </summary>
    NinetyNinePointNine = 999,
}
