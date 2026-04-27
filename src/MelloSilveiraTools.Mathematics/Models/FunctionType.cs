namespace MelloSilveiraTools.Mathematics.Models;

/// <summary>
/// Contains the function types.
/// </summary>
public enum FunctionType
{
    /// <summary>
    /// Function can be represented by any mathematical equation.
    /// </summary>
    Generic = 0,

    /// <summary>
    /// f(x) = c
    /// </summary>
    Constant = 1,

    /// <summary>
    /// f(x) = a_0 + a_1 * x + a_2 * x^2 + ... + a_n * x^n
    /// </summary>
    Polynomial = 2,

    /// <summary>
    /// f(x) = a_0 * exp(a_1 * x) + a_2 * exp(a_3 * x) + ... + a_n-1 * exp(a_n * x)
    /// </summary>
    Exponential = 3,

    /// <summary>
    /// f(x) = a_0 * sin[a_1 * (x - a_2)] + ... + a_n-2 * sin[a_n-1 * (x - a_n)]
    /// </summary>
    Sine = 4,

    /// <summary>
    /// f(x) = a_0 * cos[a_1 * (x - a_2)] + ... + a_n-2 * cos[a_n-1 * (x - a_n)]
    /// </summary>
    Cosine = 5,

    /// <summary>
    /// f(x) = a_0 * x^(-a_1)
    /// </summary>
    PowerLaw = 6,

    /// <summary>
    /// f(x) = a_0 + a_1 * exp(a_2 * x) + ... a_n-1 * exp(a_n * x)
    /// </summary>
    PronySeries = 7,
}
