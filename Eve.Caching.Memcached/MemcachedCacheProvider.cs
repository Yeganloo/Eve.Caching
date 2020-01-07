using System;
using Eve.Caching;
using Enyim.Caching.Memcached;


namespace Eve.Caching.Memcached 
{
    public class MemcachedCacheProvider<TValue> : ICacheProvider<string,TValue>
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
            _Cache.StoreAsync(StoreMode.Set,key,obj, Expiration.Never).Wait();
        }
        public void Cache(string key, TValue obj, TimeOutMode mode, int timeOut)
        {
            switch(mode)
            {
                case TimeOutMode.AccessCount:
                case TimeOutMode.LastUse:
                default:
                    throw new NotImplementedException();
                case TimeOutMode.Never:
                    Cache(key,obj);
                    break;
                case TimeOutMode.FromCreate:
                    _Cache.StoreAsync(StoreMode.Set,key,obj,new TimeSpan(00,00,timeOut)).Wait();
                    break;
            }
        }
        public void Remove(string key)
        {
            _Cache.DeleteAsync(key).Wait();
        }

        public void Cache(string key, string subkey, TValue obj)
        {
            Cache(joinKeis(key,subkey),obj);
        }
        public void Cache(string key, string subkey, TValue obj, TimeOutMode mode, int timeOut)
        {
            Cache(joinKeis(key,subkey),obj,mode,timeOut);
        }
        public void Remove(string key, string subkey)
        {
            Remove(joinKeis(key,subkey));
        }

        public void Clear()
        {
            _Cache.FlushAll().Wait();
        }
        public bool HasKey(string key)
        {
            throw new NotImplementedException();
        }
        public bool HasKey(string key, string subkey)
        {
            return HasKey(joinKeis(key,subkey));
        }

        public TValue this[string key] 
        {
            set
            {
                Cache(key,value);
            }
            get
            {
                var t = _Cache.GetAsync(key);
                t.Wait();
                return (TValue)t.Result;
            }
        }
        public TValue this[string key, string subkey] 
        { 
            set
            {
                this[joinKeis(key,subkey)] = value;
            }
            get
            {
                return this[joinKeis(key,subkey)];
            }
        }

        public T Get<T>(string key) where T : TValue
        {
            var t = _Cache.GetAsync(key);
            t.Wait();
            return (T)t.Result;
        }
        public T Get<T>(string key, string subkey) where T : TValue
        {
            return Get<T>(joinKeis(key,subkey));
        }
        public object Get(string key, Type type)
        {
            var t = _Cache.GetAsync(key);
            t.Wait();
            return Convert.ChangeType(t.Result,type);
        }
        public object Get(string key, string subkey, Type type)
        {
            return Get(joinKeis(key,subkey),type);
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
