using MelloSilveiraTools.Core.ExtensionMethods;
using Microsoft.Extensions.Logging.Abstractions;

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
    public void IsNullOrEmpty_ShouldCorrectlyDetectNullOrEmpty()
    {
        List<int>? nullList = null;
        List<int> emptyList = [];
        List<int> populatedList = [1, 2];

        Assert.True(nullList.IsNullOrEmpty());
        Assert.True(emptyList.IsNullOrEmpty());
        Assert.False(populatedList.IsNullOrEmpty());
    }

    [Fact]
    public void IsNotNullOrEmpty_ShouldCorrectlyDetectPopulated()
    {
        List<int>? nullList = null;
        List<int> emptyList = [];
        List<int> populatedList = [10];

        Assert.False(nullList.IsNotNullOrEmpty());
        Assert.False(emptyList.IsNotNullOrEmpty());
        Assert.True(populatedList.IsNotNullOrEmpty());
    }

    [Fact]
    public void GetSourceType_ShouldReturnRuntimeTypeOfFirstElementOrGenericType()
    {
        object[] objects = ["string item", 123];
        List<int> emptyInts = [];

        Type populatedType = objects.GetSourceType();
        Type emptyType = emptyInts.GetSourceType();

        Assert.Equal(typeof(string), populatedType);
        Assert.Equal(typeof(int), emptyType);
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

    [Fact]
    public async Task ForeachAsync_DispatchesParallelItemsWithinConcurrencyLimit()
    {
        List<int> numbers = [1, 2, 3, 4, 5];
        System.Collections.Concurrent.ConcurrentBag<int> processed = [];

        await numbers.ForeachAsync(async n =>
        {
            await Task.Yield();
            processed.Add(n * 10);
        }, maxDegreeOfParallelism: 2, NullLogger.Instance);

        Assert.Equal(5, processed.Count);
        Assert.Contains(10, processed);
        Assert.Contains(50, processed);
    }

    [Fact]
    public void GetCombinations_ComputesCartesianProductCorrectly()
    {
        List<List<string>> lists =
        [
            ["A", "B"],
            ["1", "2"]
        ];

        List<string[]> combinations = lists.GetCombinations().ToList();

        // Should produce 2x2 = 4 combinations: ["A","1"], ["A","2"], ["B","1"], ["B","2"]
        Assert.Equal(4, combinations.Count);
        Assert.Equal(["A", "1"], combinations[0]);
        Assert.Equal(["A", "2"], combinations[1]);
        Assert.Equal(["B", "1"], combinations[2]);
        Assert.Equal(["B", "2"], combinations[3]);
    }
}
