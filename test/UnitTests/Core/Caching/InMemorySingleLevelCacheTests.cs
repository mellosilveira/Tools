using MelloSilveiraTools.Core.Caching;

namespace UnitTests.Core.Caching;

public class InMemorySingleLevelCacheTests
{
    [Fact]
    public void TryGet_WhenKeyDoesNotExist_ShouldReturnFalse()
    {
        InMemorySingleLevelCache cache = new();

        bool found = cache.TryGet("missingKey", out string? value);

        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void SetAndTryGet_WhenKeyExists_ShouldReturnTrueAndValue()
    {
        InMemorySingleLevelCache cache = new();
        string key = "sampleKey";
        int expectedValue = 12345;

        cache.Set(key, expectedValue);
        bool found = cache.TryGet(key, out int actualValue);

        Assert.True(found);
        Assert.Equal(expectedValue, actualValue);
    }

    [Fact]
    public void GetOrAdd_WhenKeyMissing_ShouldInvokeFactoryAndStore()
    {
        InMemorySingleLevelCache cache = new();
        string key = "calcKey";
        int factoryCalls = 0;

        string firstResult = cache.GetOrAdd(key, () =>
        {
            factoryCalls++;
            return "computedValue";
        });

        string secondResult = cache.GetOrAdd(key, () =>
        {
            factoryCalls++;
            return "differentValue";
        });

        Assert.Equal("computedValue", firstResult);
        Assert.Equal("computedValue", secondResult);
        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    public void Remove_ShouldDeleteEntry()
    {
        InMemorySingleLevelCache cache = new();
        string key = "temporaryKey";

        cache.Set(key, "data");
        cache.Remove(key);
        bool found = cache.TryGet(key, out string? value);

        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void GetKeys_ShouldReturnAllStoredKeys()
    {
        InMemorySingleLevelCache cache = new();
        cache.Set("key1", 1);
        cache.Set("key2", 2);

        List<string> keys = [.. cache.GetKeys()];

        Assert.Equal(2, keys.Count);
        Assert.Contains("key1", keys);
        Assert.Contains("key2", keys);
    }

    [Fact]
    public void Clear_ShouldRemoveAllEntries()
    {
        InMemorySingleLevelCache cache = new();
        cache.Set("key1", 1);
        cache.Set("key2", 2);

        cache.Clear();

        Assert.Empty(cache.GetKeys());
    }
}
