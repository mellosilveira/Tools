namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

/// <summary>
/// Contains all mechanical models used in application.
///     Elastic model - 0XX.
///     Linear viscoelastic model - 1XX.
///     Quasilinear viscoelastic model - 2XX.
///     Nonlinear viscoelastic model - 3XX.
/// </summary>
public enum MechanicalModel
{
    /// <summary>
    /// Elastic mechanical model.
    /// </summary>
    Elastic = 001,

    /// <summary>
    /// Maxwell's linear viscoelastic model.
    /// </summary>
    Maxwell = 101,

    /// <summary>
    /// Fung's quasilinear viscoelastic model.
    /// </summary>
    Fung = 201,

    /// <summary>
    /// Simplified Fung's quasilinear viscoelastic model.
    /// </summary>
    SimplifiedFung = 202,

    /// <summary>
    /// Schapery's nonlinear viscoelastic model.
    /// </summary>
    Schapery = 301,

    /// <summary>
    /// Modified Superposition Method.
    /// </summary>
    ModifiedSuperpositionMethod = 302
}
