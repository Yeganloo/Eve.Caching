using StackExchange.Redis;
using System;

namespace Eve.Caching.Redis
{
    public class RedisCacheProvider<TVal> : ICacheProvider<string, TVal>
    {
        private ConnectionMultiplexer Connection;
        private MessagePack.MessagePackSerializerOptions Option;

        private byte[] Serialize(TVal obj)
        {
            return MessagePack.MessagePackSerializer.Serialize(obj, Option);
        }
        private T Deserialize<T>(byte[] val)
        {
            if (val == null)
                return default(T);

            return MessagePack.MessagePackSerializer.Deserialize<T>(val, Option);
        }

        private object Deserialize(byte[] val, Type type)
        {
            return typeof(MessagePack.MessagePackSerializer).GetMethod("Deserialize", new Type[] { typeof(byte[]), typeof(MessagePack.MessagePackSerializerOptions) }).MakeGenericMethod(type).Invoke(null, new object[] { val, Option });
        }

        public IDatabase _Cache
        {
            get
            {
                return Connection.GetDatabase();
            }
        }


        public RedisCacheProvider(ConfigurationOptions options, MessagePack.MessagePackSerializerOptions mspOptions = null)
        {
            Option = mspOptions ?? MessagePack.Resolvers.ContractlessStandardResolver.Options;
            Connection = ConnectionMultiplexer.Connect(options);
        }

        public TVal this[string key]
        {
            get => Deserialize<TVal>((byte[])_Cache.StringGet(key));
            set
            {
                _Cache.StringSet(key, Serialize(value));
            }
        }

        public TVal this[string key, string SubKey]
        {
            get => Deserialize<TVal>((byte[])_Cache.HashGet(key, SubKey));
            set
            {
                _Cache.HashSet(key, SubKey, Serialize(value));
            }
        }

        public void Cache(string name, TVal obj, TimeOutMode mode, int timeOut)
        {
            if (timeOut < 0)
                throw new ArgumentException($"TimeOut is a Posetive number!\r\nValue is {timeOut}");
            if ((mode & (TimeOutMode.FromCreate | TimeOutMode.Never)) == 0)
                throw new ArgumentException($"TimeOut Mode is not supported!\r\nMode is {mode.ToString()}");
            this[name] = obj;
            if (mode == TimeOutMode.FromCreate)
                _Cache.KeyExpireAsync(name, DateTime.Now.AddMilliseconds(timeOut));
        }

        public void Cache(string key, TVal obj)
        {
            this[key] = obj;
        }

        public void Cache(string key, string subkey, TVal obj)
        {
            this[key, subkey] = obj;
        }

        public void Cache(string key, string subkey, TVal obj, TimeOutMode mode, int timeOut)
        {
            if (timeOut < 0)
                throw new ArgumentException($"TimeOut is a Posetive number!\r\nValue is {timeOut}");
            if ((mode & (TimeOutMode.FromCreate | TimeOutMode.Never)) == 0)
                throw new ArgumentException($"TimeOut Mode is not supported!\r\nMode is {mode.ToString()}");
            this[key, subkey] = obj;
            if (mode == TimeOutMode.FromCreate)
                _Cache.KeyExpireAsync(key, DateTime.Now.AddMilliseconds(timeOut));
        }

        /// <summary>
        /// To dangerous!
        /// Clear Entire Database!
        /// </summary>
        public void Clear()
        {
            _Cache.Execute("FLUSHDB");
        }

        public bool HasKey(string key)
        {
            return _Cache.KeyExists(key);
        }

        public void Remove(string name)
        {
            _Cache.KeyDelete(name);
        }

        public void Remove(string key, string subKey)
        {
            _Cache.HashDelete(key, subKey);
        }

        public bool HasKey(string key, string subKey)
        {
            return _Cache.HashExists(key, subKey);
        }

        public T Get<T>(string key) where T : TVal
        {
            return Deserialize<T>((byte[])_Cache.StringGet(key));
        }

        public T Get<T>(string key, string SubKey) where T : TVal
        {
            return Deserialize<T>((byte[])_Cache.HashGet(key, SubKey));
        }

        public object Get(string key, Type type)
        {
            return Deserialize((byte[])_Cache.StringGet(key), type);
        }

        public object Get(string key, string SubKey, Type type)
        {
            return Deserialize((byte[])_Cache.HashGet(key, SubKey), type);
        }
    }
}
