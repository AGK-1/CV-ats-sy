using Microsoft.Extensions.Caching.Memory;

public class TempStorageService
{
    private readonly IMemoryCache _cache;

    public TempStorageService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void SetTempValue(string key, string value, int seconds)
    {
        _cache.Set(key, value, TimeSpan.FromSeconds(seconds));
    }

    public string GetTempValue(string key)
    {
        return _cache.TryGetValue(key, out string value) ? value : null;
    }

    public void SetPermanentValue(string key, string value)
    {
        _cache.Set(key, value); // без времени → живёт вечно
    }
    public string GetPermanentValue(string key)
    {
        return GetTempValue(key);
    }
}
