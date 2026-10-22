namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

/// <summary>
/// Encapsulates the results of analytical parameter pre-estimation for the continuous
/// Fung reduced relaxation function using logarithmic time-scale regression as formulated
/// by Y.C. Fung (1993, p. 302).
/// </summary>
/// <param name="InitialC">
/// The analytically estimated continuous relaxation spectrum magnitude (C), representing
/// the dimensionless relaxation stiffness or intensity of viscous damping.
/// </param>
/// <param name="FastRelaxationTime">
/// The fast relaxation time (τ₁), in seconds, corresponding to the lower cutoff of the continuous
/// relaxation spectrum (short-term viscoelastic decay). Detected at the transition from the initial
/// plateau G(0) = 1 into the linear logarithmic decay region.
/// </param>
/// <param name="SlowRelaxationTime">
/// The initial slow relaxation time (τ₂), in seconds, corresponding to the upper cutoff of the
/// continuous relaxation spectrum (long-term equilibrium). For biological soft tissues where experimental
/// testing duration is bounded to avoid specimen degradation, this represents a physically sound
/// lower bound for the unobserved long-term equilibrium.
/// </param>
/// <param name="RSquared">
/// The coefficient of determination (R²) of the linear regression in logarithmic time,
/// assessing the quality and linearity of the logarithmic decay approximation.
/// </param>
public readonly record struct FungLogarithmicEstimationResult(
    double InitialC,
    double FastRelaxationTime,
    double SlowRelaxationTime,
    double RSquared);
