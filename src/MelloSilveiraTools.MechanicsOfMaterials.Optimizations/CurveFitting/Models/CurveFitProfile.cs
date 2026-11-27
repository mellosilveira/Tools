namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

/// <summary>
/// Defines behavior and constraint profiles for curve fitting.
/// </summary>
public enum CurveFitProfile
{
    /// <summary>
    /// Automatic selection. If boolean validation rules are provided,
    /// selects RuleConstrained; otherwise, Standard.
    /// </summary>
    Automatic = 0,

    /// <summary>
    /// Fast analytical fitting for smooth functions and simple box bounds (Min/Max).
    /// Runs in milliseconds via L-BFGS.
    /// </summary>
    Standard = 1,

    /// <summary>
    /// Stochastic global search (Differential Evolution / GDEMO).
    /// Supports boolean consistency validations and discards invalid combinations without breaking derivatives.
    /// </summary>
    RuleConstrained = 2
}
