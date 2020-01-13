using System;
using Eve.Caching;
using Enyim.Caching.Memcached;
using System.Threading.Tasks;

namespace Eve.Caching.Memcached
{
    public class MemcachedCacheProvider<TValue> : ICacheProvider<string, TValue>
    {
        private MemcachedCluster _cluster = null;
        private MemcachedClient _client = null;

        public MemcachedCacheProvider(MemcachedCluster cluster)
        {
            this._cluster = cluster;
        }
        public MemcachedCacheProvider(MemcachedClient client)
        {
            this._client = client;
        }

        private IMemcachedClient _Cache
        {
            get
            {
                return _cluster == null ? _client : _cluster.GetClient();
            }
        }

        private string joinKeis(string key, string subkey)
        {
            return $"!~{key}_{subkey}";
        }

        public void Cache(string key, TValue obj)
        {
            Cache(key, obj, TimeOutMode.Never, 0);
        }
        public void Cache(string key, TValue obj, TimeOutMode mode, int timeOut)
        {
            var tmp = new ItemContainer<TValue>()
            {
                Content = obj,
                Mode = mode,
                CreationTime = DateTime.Now,
                AccessCounter = timeOut
            };
            switch (mode)
            {
                case TimeOutMode.AccessCount:
                case TimeOutMode.Never:
                default:
                    _Cache.StoreAsync(StoreMode.Set, key, tmp).Wait();
                    break;
                case TimeOutMode.LastUse:
                case TimeOutMode.FromCreate:
                    _Cache.StoreAsync(StoreMode.Set, key, tmp, new TimeSpan(00, 00, timeOut)).Wait();
                    break;
            }
        }
        public void Remove(string key)
        {
            _Cache.DeleteAsync(key).Wait();
        }

        public void Cache(string key, string subkey, TValue obj)
        {
            Cache(joinKeis(key, subkey), obj);
        }
        public void Cache(string key, string subkey, TValue obj, TimeOutMode mode, int timeOut)
        {
            Cache(joinKeis(key, subkey), obj, mode, timeOut);
        }
        public void Remove(string key, string subkey)
        {
            Remove(joinKeis(key, subkey));
        }

        public void Clear()
        {
            _Cache.FlushAll().Wait();
        }
        public bool HasKey(string key)
        {
            try
            {
                var t = _Cache.GetAsync(key);
                t.Wait();
                ItemContainer<TValue> item;
                switch ((item = (ItemContainer<TValue>)t.Result).Mode)
                {
                    case TimeOutMode.Never:
                    case TimeOutMode.FromCreate:
                    case TimeOutMode.LastUse:
                    default:
                        return true;
                    case TimeOutMode.AccessCount:
                        return item.AccessCounter > 0;
                }
            }
            catch
            {
                return false;
            }
        }
        public bool HasKey(string key, string subkey)
        {
            return HasKey(joinKeis(key, subkey));
        }

        public TValue this[string key]
        {
            set
            {
                Cache(key, value);
            }
            get
            {
                return Get<TValue>(key);
            }
        }
        public TValue this[string key, string subkey]
        {
            set
            {
                Cache(joinKeis(key, subkey), value);
            }
            get
            {
                return Get<TValue>(joinKeis(key, subkey));
            }
        }

        public T Get<T>(string key) where T : TValue
        {
            try
            {
                var t = _Cache.GetAsync(key);
                t.Wait();
                ItemContainer<TValue> tmp = (ItemContainer<TValue>)t.Result;
                switch (tmp.Mode)
                {
                    case TimeOutMode.Never:
                    case TimeOutMode.FromCreate:
                    default:
                        return (T)tmp.Content;
                    case TimeOutMode.AccessCount:
                        if (tmp.AccessCounter-- > 0)
                        {
                            _Cache.StoreAsync(StoreMode.Replace, key, tmp).Wait();
                            return (T)tmp.Content;
                        }
                        else
                        {
                            Remove(key);
                            return default(T);
                        }
                    case TimeOutMode.LastUse:
                        if (tmp.AccessCounter >= DateTime.UtcNow.Subtract(tmp.CreationTime).TotalSeconds)
                        {
                            tmp.CreationTime = DateTime.UtcNow;
                            _Cache.StoreAsync(StoreMode.Set, key, tmp, new TimeSpan(00, 00, tmp.AccessCounter)).Wait();
                            return (T)tmp.Content;
                        }
                        else
                        {
                            Remove(key);
                            return default(T);
                        }
                }
            }
            catch
            {
                return default(T);
            }
        }
        public T Get<T>(string key, string subkey) where T : TValue
        {
            return Get<T>(joinKeis(key, subkey));
        }
        public object Get(string key, Type type)
        {
            return Convert.ChangeType(this.Get<TValue>(key), type);
        }
        public object Get(string key, string subkey, Type type)
        {
            return Get(joinKeis(key, subkey), type);
        }

        public void Cache(string key, TValue obj, DateTime expiry)
        {
            Cache(key, obj, TimeOutMode.FromCreate, (int)expiry.Subtract(DateTime.UtcNow).TotalSeconds);
        }

        public void Cache(string key, string subkey, TValue obj, DateTime expiry)
        {
            Cache(key, subkey, obj, TimeOutMode.FromCreate, (int)expiry.Subtract(DateTime.UtcNow).TotalSeconds);
        }
    }
}
