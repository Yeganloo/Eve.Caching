using StackExchange.Redis;
using System;

namespace Eve.Caching.Redis
{
    public class RedisCacheProvider<TVal> : ICacheProvider<string, TVal>
    {
        private ConnectionMultiplexer Connection;
        private MessagePack.MessagePackSerializerOptions Option;
        private const string TimerPrifix = "~!Time_";

        private readonly LuaScript _LuaGet;
        private readonly LuaScript _LuaCache;
        private readonly LuaScript _LuaDel;

        private readonly LuaScript _LuaGetSub;
        private readonly LuaScript _LuaCacheSub;
        private readonly LuaScript _LuaDelSub;

        private readonly DateTime _BaseDate;

        protected string GetTimerKey(string key) => $"{TimerPrifix}{key}";
        protected string GetTimerKey(string key, string subKey) => $"{TimerPrifix}{key}{TimerPrifix}{subKey}";

        private void Store(string key, TVal obj, TimeOutMode mode = TimeOutMode.Never, int timeOut = 0)
        {
            _Cache.ScriptEvaluate(_LuaCache, new { key = (RedisKey)key, value = Serialize(obj), mode = (int)mode, timer = timeOut, timerkey = GetTimerKey(key), creation = DateTime.UtcNow.Subtract(_BaseDate).TotalSeconds });
        }
        private void Store(string key, string subkey, TVal obj, TimeOutMode mode = TimeOutMode.Never, int timeOut = 0)
        {
            _Cache.ScriptEvaluate(_LuaCacheSub, new { key = (RedisKey)key, subkey = (RedisKey)subkey, value = Serialize(obj), mode = (int)mode, timer = timeOut, timerkey = GetTimerKey(key, subkey), creation = DateTime.UtcNow.Subtract(_BaseDate).TotalSeconds });
        }

        private byte[] Restore(string key)
        {
            return (byte[])_Cache.ScriptEvaluate(_LuaGet, new { key = key, timerkey = GetTimerKey(key), now = DateTime.UtcNow.Subtract(_BaseDate).TotalSeconds });
        }
        private byte[] Restore(string key, string subkey)
        {
            return (byte[])_Cache.ScriptEvaluate(_LuaGetSub, new { key = key, subkey = subkey, timerkey = GetTimerKey(key, subkey), now = DateTime.UtcNow.Subtract(_BaseDate).TotalSeconds });
        }

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
            return MessagePack.MessagePackSerializer.Deserialize(type, val, Option);
        }

        public IDatabase _Cache
        {
            get
            {
                return Connection.GetDatabase();
            }
        }

        public RedisCacheProvider()
        {
            _BaseDate = new DateTime(1970, 01, 01, 00, 00, 00);

            _LuaCache = LuaScript.Prepare(@"redis.call('hset',@timerkey,'mode',@mode);
redis.call('hset',@timerkey,'counter',@timer);
redis.call('hset',@timerkey,'creation',@creation);
redis.call('set',@key,@value);");
            _LuaDel = LuaScript.Prepare(@"redis.call('del',@timerkey);
redis.call('del',@key);
return nil;");
            _LuaGet = LuaScript.Prepare($@"local mode = tonumber(redis.call('hget',@timerkey,'mode'));
if mode == nil then
    return nil;
end
if mode == {(byte)TimeOutMode.Never} then
    return redis.call('get',@key);
elseif mode == {(byte)TimeOutMode.AccessCount} then
    local cnt = tonumber(redis.call('hincrby',@timerkey,'counter',-1));
    if cnt >= 0 then
        return redis.call('get',@key);
    else
        redis.call('del',@timerkey);
        redis.call('del',@key);
        return nil;
    end
elseif mode == {(byte)TimeOutMode.FromCreate} then
    local crt = tonumber(redis.call('hget',@timerkey,'creation'));
    if @now - crt < tonumber(redis.call('hget',@timerkey,'counter')) then
        return redis.call('get',@key);
    else
        redis.call('del',@timerkey);
        redis.call('del',@key);
        return nil;
    end
else
    local crt = tonumber(redis.call('hget',@timerkey,'creation'));
    if @now - crt < tonumber(redis.call('hget',@timerkey,'counter')) then
        redis.call('hset',@timerkey,'creation',@now);
        return redis.call('get',@key);
    else
        redis.call('del',@timerkey);
        redis.call('del',@key);
        return nil;
    end
end
");

            _LuaCacheSub = LuaScript.Prepare(@"redis.call('hset',@timerkey,'mode',@mode);
redis.call('hset',@timerkey,'counter',@timer);
redis.call('hset',@timerkey,'creation',@creation);
redis.call('hset',@key,@subkey,@value);");
            _LuaDelSub = LuaScript.Prepare(@"redis.call('del',@timerkey);
redis.call('hdel',@key,@subkey);
return nil;");
            _LuaGetSub = LuaScript.Prepare($@"local mode = tonumber(redis.call('hget',@timerkey,'mode'));
if mode == nil then
    return nil;
end
if mode == {(byte)TimeOutMode.Never} then
    return redis.call('hget',@key,@subkey);
elseif mode == {(byte)TimeOutMode.AccessCount} then
    local cnt = tonumber(redis.call('hincrby',@timerkey,'counter',-1));
    if cnt > 0 then
        return redis.call('hget',@key,@subkey);
    else
        redis.call('del',@timerkey);
        redis.call('hdel',@key,@subkey);
        return nil;
    end
elseif mode == {(byte)TimeOutMode.FromCreate} then
    local crt = redis.call('hget',@timerkey,'creation');
    if crt - @now > 0 then
        return redis.call('hget',@key,@subkey);
    else
        redis.call('del',@timerkey);
        redis.call('hdel',@key,@subkey);
        return nil;
    end
else
    local crt = redis.call('hget',@timerkey,'creation');
    if crt - @now > 0 then
        redis.call('hset',@timerkey,'creation',@now);
        return redis.call('hget',@key,@subkey);
    else
        redis.call('del',@timerkey);
        redis.call('hdel',@key,@subkey);
        return nil;
    end
end
");
        }

        public RedisCacheProvider(ConfigurationOptions options, MessagePack.MessagePackSerializerOptions mspOptions = null) : this()
        {
            Option = mspOptions ?? MessagePack.Resolvers.ContractlessStandardResolver.Options;
            Connection = ConnectionMultiplexer.Connect(options);
        }

        public TVal this[string key]
        {
            get => Deserialize<TVal>(Restore(key));
            set
            {
                Store(key, value);
            }
        }

        public TVal this[string key, string subKey]
        {
            get => Deserialize<TVal>(Restore(key, subKey));
            set
            {
                Store(key, subKey, value);
            }
        }

        public void Cache(string key, TVal obj, TimeOutMode mode, int timeOut)
        {
            if (timeOut < 0)
                throw new ArgumentException($"TimeOut is a Posetive number!\r\nValue is {timeOut}");
            Store(key, obj, mode, timeOut);
        }

        public void Cache(string key, TVal obj)
        {
            Store(key, obj);
        }

        public void Cache(string key, string subkey, TVal obj)
        {
            Store(key, subkey, obj);
        }

        public void Cache(string key, string subkey, TVal obj, TimeOutMode mode, int timeOut)
        {
            if (timeOut < 0)
                throw new ArgumentException($"TimeOut is a Posetive number!\r\nValue is {timeOut}");
            Store(key, subkey, obj, mode, timeOut);
        }

        /// <summary>
        /// To dangerous!
        /// Clear Entire Database!
        /// </summary>
        public void Clear()
        {
            _Cache.Execute("FLUSHDB");
        }

        //BUG Check expiertion on exists.
        public bool HasKey(string key)
        {
            return _Cache.KeyExists(key);
        }

        public void Remove(string key)
        {
            _Cache.ScriptEvaluate(_LuaDel, new { key = key, timerkey = GetTimerKey(key) });
        }

        public void Remove(string key, string subKey)
        {
            _Cache.ScriptEvaluate(_LuaDelSub, new { key = key, subkey = subKey, timerkey = GetTimerKey(key, subKey) });
        }

        //BUG Check expiertion on exists.
        public bool HasKey(string key, string subKey)
        {
            return _Cache.HashExists(key, subKey);
        }

        public T Get<T>(string key) where T : TVal
        {
            var tmp = Restore(key);
            return tmp != null ? Deserialize<T>(tmp) : default(T);
        }

        public T Get<T>(string key, string subKey) where T : TVal
        {
            var tmp = Restore(key, subKey);
            return tmp != null ? Deserialize<T>(tmp) : default(T);
        }

        public object Get(string key, Type type)
        {
            var tmp = Restore(key);
            return tmp != null ? Deserialize(tmp, type) : type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        public object Get(string key, string subKey, Type type)
        {
            var tmp = Restore(key, subKey);
            return tmp != null ? Deserialize(tmp, type) : type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}
