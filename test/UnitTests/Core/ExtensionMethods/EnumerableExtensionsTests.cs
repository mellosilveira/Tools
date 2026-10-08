using MelloSilveiraTools.Core.ExtensionMethods;

namespace UnitTests.Core.ExtensionMethods;

public class EnumerableExtensionsTests
{
    [Fact]
    public void IsEmpty_WhenCollectionHasNoItems_ShouldReturnTrue()
    {
        List<int> empty = [];

        Assert.True(empty.IsEmpty());
    }

    [Fact]
    public void IsEmpty_WhenCollectionHasItems_ShouldReturnFalse()
    {
        List<int> items = [1, 2, 3];

        Assert.False(items.IsEmpty());
    }

    [Fact]
    public void FirstOrDefaultWithoutValidate_ShouldReturnMatchingElementOrFallback()
    {
        List<string> names = ["Alice", "Bob", "Charlie"];

        string? found = names.FirstOrDefaultWithoutValidate(n => n.StartsWith('B'));
        string? notFound = names.FirstOrDefaultWithoutValidate(n => n.StartsWith('Z'), defaultValue: "Default");

        Assert.Equal("Bob", found);
        Assert.Equal("Default", notFound);
    }

    [Fact]
    public void FirstWithoutValidate_ShouldReturnMatchOrThrow()
    {
        List<int> numbers = [10, 20, 30];

        int match = numbers.FirstWithoutValidate(n => n > 15);

        Assert.Equal(20, match);
        Assert.Throws<InvalidOperationException>(() =>
        {
            numbers.FirstWithoutValidate(n => n > 100);
        });
    }
}
