using System;

namespace Eve.Caching
{
    public class SimpleCacheProvider<TVal> : DictionaryCacheProvider<string, TVal>
    {

        private string joinKeis(string key, string subkey)
        {
            return $"!~{key}_{subkey}";
        }

        public override TVal this[string Key, string SubKey]
        {
            get => this[joinKeis(Key, SubKey)];
            set => this[joinKeis(Key, SubKey)] = value;
        }

        public override void Cache(string key, string subkey, TVal obj)
        {
            Cache(joinKeis(key, subkey), obj, TimeOutMode.Never, 0);
        }

        public override void Cache(string key, string subkey, TVal obj, TimeOutMode mode, int timeOut)
        {
            Cache(key, subkey, obj);
        }

        public override void Remove(string key, string subKey)
        {
            Remove(joinKeis(key, subKey));
        }

        public override bool HasKey(string key, string subKey)
        {
            return HasKey(joinKeis(key, subKey));
        }

        public override T Get<T>(string key, string SubKey)
        {
            return Get<T>(joinKeis(key, SubKey));
        }

        public override object Get(string key, string SubKey, Type type)
        {
            return Get(joinKeis(key, SubKey), type);
        }
    }
}
