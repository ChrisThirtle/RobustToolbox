using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.ResourceManagement;

[Virtual]
public class SharedResourceCache : IResourceCache
{
private readonly Dictionary<Type, Dictionary<ResPath, BaseResource>> _cachedResources =
        new();

    private readonly Dictionary<Type, BaseResource> _fallbacks = new();

    public T GetResource<T>(string path, bool useFallback = true) where T : BaseResource, new()
    {
        return GetResource<T>(new ResPath(path), useFallback);
    }

    public T GetResource<T>(ResPath path, bool useFallback = true) where T : BaseResource, new()
    {
        var cache = GetTypeDict<T>();
        if (cache.TryGetValue(path, out var cached))
        {
            return (T) cached;
        }

        var resource = new T();
        try
        {
            var dependencies = IoCManager.Instance!;
            resource.Load(dependencies, path);
            cache[path] = resource;
            return resource;
        }
        catch (Exception e)
        {
            if (useFallback && resource.Fallback != null)
            {
                Logger.Error(
                    $"Exception while loading resource {typeof(T)} at '{path}', resorting to fallback.\n{Environment.StackTrace}\n{e}");
                return GetResource<T>(resource.Fallback.Value, false);
            }
            else
            {
                Logger.Error(
                    $"Exception while loading resource {typeof(T)} at '{path}', no fallback available\n{Environment.StackTrace}\n{e}");
                throw;
            }
        }
    }

    public bool TryGetResource<T>(string path, [NotNullWhen(true)] out T? resource) where T : BaseResource, new()
    {
        return TryGetResource(new ResPath(path), out resource);
    }

    public bool TryGetResource<T>(ResPath path, [NotNullWhen(true)] out T? resource) where T : BaseResource, new()
    {
        var cache = GetTypeDict<T>();
        if (cache.TryGetValue(path, out var cached))
        {
            resource = (T) cached;
            return true;
        }

        var _resource = new T();
        try
        {
            var dependencies = IoCManager.Instance!;
            _resource.Load(dependencies, path);
            resource = _resource;
            cache[path] = resource;
            return true;
        }
        catch
        {
            resource = null;
            return false;
        }
    }

    public void ReloadResource<T>(string path) where T : BaseResource, new()
    {
        ReloadResource<T>(new ResPath(path));
    }

    public void ReloadResource<T>(ResPath path) where T : BaseResource, new()
    {
        var cache = GetTypeDict<T>();

        if (!cache.TryGetValue(path, out var res))
        {
            return;
        }

        try
        {
            var dependencies = IoCManager.Instance!;
            res.Reload(dependencies, path);
        }
        catch (Exception e)
        {
            Logger.Error($"Exception while reloading resource {typeof(T)} at '{path}'\n{e}");
            throw;
        }
    }

    public bool HasResource<T>(string path) where T : BaseResource, new()
    {
        return HasResource<T>(new ResPath(path));
    }

    public bool HasResource<T>(ResPath path) where T : BaseResource, new()
    {
        return TryGetResource<T>(path, out var _);
    }

    public void CacheResource<T>(string path, T resource) where T : BaseResource, new()
    {
        CacheResource(new ResPath(path), resource);
    }

    public void CacheResource<T>(ResPath path, T resource) where T : BaseResource, new()
    {
        GetTypeDict<T>()[path] = resource;
    }

    public T GetFallback<T>() where T : BaseResource, new()
    {
        if (_fallbacks.TryGetValue(typeof(T), out var fallback))
        {
            return (T) fallback;
        }

        var res = new T();
        if (res.Fallback == null)
        {
            throw new InvalidOperationException($"Resource of type '{typeof(T)}' has no fallback.");
        }

        fallback = GetResource<T>(res.Fallback.Value, useFallback: false);
        _fallbacks.Add(typeof(T), fallback);
        return (T) fallback;
    }

    public IEnumerable<KeyValuePair<ResPath, T>> GetAllResources<T>() where T : BaseResource, new()
    {
        return GetTypeDict<T>().Select(p => new KeyValuePair<ResPath, T>(p.Key, (T) p.Value));
    }

    #region IDisposable Members

    private bool disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        if (disposing)
        {
            foreach (var res in _cachedResources.Values.SelectMany(dict => dict.Values))
            {
                res.Dispose();
            }
        }

        disposed = true;
    }

    ~SharedResourceCache()
    {
        Dispose(false);
    }

    #endregion IDisposable Members

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected Dictionary<ResPath, BaseResource> GetTypeDict<T>()
    {
        if (!_cachedResources.TryGetValue(typeof(T), out var ret))
        {
            ret = new Dictionary<ResPath, BaseResource>();
            _cachedResources.Add(typeof(T), ret);
        }

        return ret;
    }
}
