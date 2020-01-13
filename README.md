# Eve.Caching

Eve.Caching provide a simple caching interface. The base library also containes a simple in-memory provider. There is also more extention libraries that impliment this interface for popular caching services like Redis or Memcached.

# Installing Module
Nuget package is avalible!
## Base library

``` shell
dotnet add package Eve.Caching
```
## Redis provider
``` shell
dotnet add package Eve.Caching.Redis
```
## Memcached provider
``` shell
dotnet add package Eve.Caching.Memcached
```

# Simple usage

## Simple provider
``` c#
    ICacheProvider<string, object> _cache = new SimpleCacheProvider<object>();
    _cache.Cache("test", new { message = "Hello World!" });
    Console.WriteLine((string)_cache.Get<dynamic>("test").message);
```
## Redis Provider
``` c#
    string cnn = "127.0.0.1:6379,defaultDatabase=0";
    ICacheProvider<string, object> _cache = new RedisCacheProvider<object>(StackExchange.Redis.ConfigurationOptions.Parse(cnn));
    _cache.Cache("test", new { message = "Hello World!" });
    Console.WriteLine((string)_cache.Get<Dictionary<object,object>>("test")["message"]);

```

## Memcached Provider
``` c#
    string cnn = "127.0.0.1:11211";
    var cluster = new Enyim.Caching.Memcached.MemcachedCluster(cnn);
    cluster.Start();
    ICacheProvider<string, object> _cache = new MemcachedCacheProvider<object>(cluster);
    _cache.Cache("test", new testclass { message = "Hello World!" });
    Console.WriteLine((string)_cache.Get<dynamic>("test").message);
    cluster.Dispose();

```
