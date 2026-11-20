using MelloSilveiraTools.Core.Models;

namespace UnitTests.Core.Models;

/// <summary>
/// Unit tests for <see cref="Constants"/> verifying predefined numerical guard collections.
/// </summary>
public sealed class ConstantsTests
{
    [Fact]
    public void InvalidValues_ContainsAllExpectedSentinelDoubles()
    {
        List<double> invalidValues = Constants.InvalidValues;

        Assert.NotNull(invalidValues);
        Assert.Equal(5, invalidValues.Count);
        Assert.Contains(double.NaN, invalidValues);
        Assert.Contains(double.PositiveInfinity, invalidValues);
        Assert.Contains(double.NegativeInfinity, invalidValues);
        Assert.Contains(double.MaxValue, invalidValues);
        Assert.Contains(double.MinValue, invalidValues);
    }
}
