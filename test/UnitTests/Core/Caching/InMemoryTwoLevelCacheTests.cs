using MelloSilveiraTools.Core.Caching;

namespace UnitTests.Core.Caching;

public class InMemoryTwoLevelCacheTests
{
    [Fact]
    public void TryGet_WhenGroupOrKeyDoesNotExist_ShouldReturnFalse()
    {
        InMemoryTwoLevelCache cache = new();

        bool found = cache.TryGet("groupA", "key1", out string? value);

        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void SetAndTryGet_ShouldStoreAndRetrieveValueUnderGroup()
    {
        InMemoryTwoLevelCache cache = new();
        string group = "tenants";
        string key = "tenant_100";
        string payload = "TenantConfigData";

        cache.Set(group, key, payload);
        bool found = cache.TryGet(group, key, out string? value);

        Assert.True(found);
        Assert.Equal(payload, value);
    }

    [Fact]
    public void GetOrAdd_ShouldComputeOnlyOncePerGroupAndKey()
    {
        InMemoryTwoLevelCache cache = new();
        string group = "reports";
        string key = "daily_summary";
        int callCount = 0;

        int first = cache.GetOrAdd(group, key, () =>
        {
            callCount++;
            return 42;
        });

        int second = cache.GetOrAdd(group, key, () =>
        {
            callCount++;
            return 999;
        });

        Assert.Equal(42, first);
        Assert.Equal(42, second);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void Remove_SpecificGroupAndKey_ShouldRemoveOnlyTargetedEntry()
    {
        InMemoryTwoLevelCache cache = new();
        cache.Set("groupA", "key1", "val1");
        cache.Set("groupA", "key2", "val2");

        cache.Remove("groupA", "key1");

        Assert.False(cache.TryGet("groupA", "key1", out string? _));
        Assert.True(cache.TryGet("groupA", "key2", out string? remaining));
        Assert.Equal("val2", remaining);
    }

    [Fact]
    public void Remove_EntireGroup_ShouldClearAllEntriesUnderGroup()
    {
        InMemoryTwoLevelCache cache = new();
        cache.Set("groupX", "key1", 10);
        cache.Set("groupX", "key2", 20);
        cache.Set("groupY", "key3", 30);

        cache.Remove("groupX");

        Assert.False(cache.TryGet("groupX", "key1", out int _));
        Assert.False(cache.TryGet("groupX", "key2", out int _));
        Assert.True(cache.TryGet("groupY", "key3", out int remaining));
        Assert.Equal(30, remaining);
    }

    [Fact]
    public async Task StreamAll_ShouldEnumerateAllEntries()
    {
        InMemoryTwoLevelCache cache = new();
        cache.Set("dept1", "emp1", "Alice");
        cache.Set("dept2", "emp2", "Bob");

        List<(string Group, string Key, string Value)> results = [];
        await foreach ((string g, string k, string v) in cache.StreamAll<string>())
        {
            results.Add((g, k, v));
        }

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void Clear_ShouldPurgeAllGroups()
    {
        InMemoryTwoLevelCache cache = new();
        cache.Set("g1", "k1", "data");
        cache.Set("g2", "k2", "data");

        cache.Clear();

        Assert.Empty(cache.GetKeys());
    }
}
