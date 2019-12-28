using System;

namespace Eve.Caching
{
    public interface ICacheProvider<TKey, TValue>
    {
        void Cache(TKey key, TValue obj);
        void Cache(TKey key, TValue obj, TimeOutMode mode, int timeOut);
        void Remove(TKey key);

        void Cache(TKey key, TKey subkey, TValue obj);
        void Cache(TKey key, TKey subkey, TValue obj, TimeOutMode mode, int timeOut);
        void Remove(TKey key, TKey subKey);

        void Clear();
        bool HasKey(TKey key);
        bool HasKey(TKey key, TKey subKey);

        TValue this[TKey key] { set; }
        TValue this[TKey key, TKey SubKey] { set; }

        T Get<T>(TKey key) where T : TValue;
        T Get<T>(TKey key, TKey SubKey) where T : TValue;
        object Get(TKey key, Type type);
        object Get(TKey key, TKey SubKey, Type type);
    }
}
