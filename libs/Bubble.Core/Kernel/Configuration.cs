using System.Globalization;
using Bubble.Core.Services;
using Microsoft.Extensions.Configuration;

namespace Bubble.Core.Kernel;

public sealed class Configuration : Singleton<Configuration>
{
    private IConfiguration _inner;

    public string this[params string[] keys] =>
        _inner[GetKey(keys)] ??
        throw new KeyNotFoundException($"Key '{GetKey(keys)}' not found in configuration.");

    public Configuration()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

        _inner = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();
    }
    
    public void Reload()
    {
        _inner = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();
    }

    public T? Get<T>(params string[] keys)
    {
        var key = GetKey(keys);

        return _inner.GetValue<T>(key) ??
               _inner.GetSection(key).Get<T>();
    }

    public string? GetConnectionString(string key)
    {
        return _inner.GetConnectionString(key);
    }

    private string GetKey(params string[] keys)
    {
        return string.Join(":", keys);
    }

    public T GetRequired<T>(params string[] keys)
    {
        var key = GetKey(keys);

        return _inner.GetValue<T>(key) ??
               _inner.GetSection(key).Get<T>() ??
               throw new KeyNotFoundException($"Key '{key}' not found in configuration.");
    }
}