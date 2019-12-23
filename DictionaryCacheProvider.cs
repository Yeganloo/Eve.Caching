using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Eve.Caching
{
    public class DictionaryCacheProvider<TKey, TVal> : ICacheProvider<TKey, TVal>
    {
        protected ConcurrentDictionary<TKey, ItemContainer<TVal>> _Cache = new ConcurrentDictionary<TKey, ItemContainer<TVal>>();

        public TVal this[TKey Key]
        {
            get
            {
                return Get<TVal>(Key);
            }
            set
            {
                Cache(Key, value, TimeOutMode.Never, 0);
            }
        }

        public virtual void Cache(TKey key, TVal obj, TimeOutMode mode, int timeOut)
        {
            if (timeOut < 0)
                throw new ArgumentException($"TimeOut is a Posetive number!\r\nValue is {timeOut}");
            _Cache.AddOrUpdate(key, k => new ItemContainer<TVal> { AccessCounter = timeOut, Content = obj, CreationTime = DateTime.UtcNow, Mode = mode }, (k, v) =>
            {
                v.AccessCounter = timeOut;
                v.Mode = mode;
                v.Content = obj;
                return v;
            });

        }

        public virtual void Cache(TKey key, TVal obj)
        {
            Cache(key, obj, TimeOutMode.Never, 0);
        }

        /// <summary>
        /// Can not be implimented!
        /// </summary>
        /// <param key="Key"></param>
        /// <param key="SubKey"></param>
        /// <returns></returns>
        public virtual TVal this[TKey Key, TKey SubKey]
        {
            get => throw new System.NotImplementedException();
            set => throw new System.NotImplementedException();
        }

        /// <summary>
        /// Can not be implimented!
        /// </summary>
        /// <param key="Key"></param>
        /// <param key="SubKey"></param>
        /// <returns></returns>
        public virtual void Cache(TKey key, TKey subkey, TVal obj)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Can not be implimented!
        /// </summary>
        /// <param key="Key"></param>
        /// <param key="SubKey"></param>
        /// <returns></returns>
        public virtual void Cache(TKey key, TKey subkey, TVal obj, TimeOutMode mode, int timeOut)
        {
            throw new System.NotImplementedException();
        }

        public virtual void Clear()
        {
            _Cache.Clear();
        }

        public virtual bool HasKey(TKey key)
        {
            return _Cache.ContainsKey(key);
        }

        public virtual void Remove(TKey key)
        {
            _Cache.TryRemove(key, out ItemContainer<TVal> item);
        }

        /// <summary>
        /// Can not be implimented!
        /// </summary>
        /// <param key="Key"></param>
        /// <param key="SubKey"></param>
        /// <returns></returns>
        public virtual void Remove(TKey key, TKey subKey)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Can not be implimented!
        /// </summary>
        /// <param name="key"></param>
        /// <param name="subKey"></param>
        /// <returns></returns>
        public virtual bool HasKey(TKey key, TKey subKey)
        {
            throw new NotImplementedException();
        }

        public virtual T Get<T>(TKey key) where T : TVal
        {
            return (T)Get(key, typeof(T));
        }

        /// <summary>
        /// Can not be implimented!
        /// </summary>
        /// <param name="key"></param>
        /// <param name="subKey"></param>
        /// <returns></returns>
        public virtual T Get<T>(TKey key, TKey SubKey) where T : TVal
        {
            throw new NotImplementedException();
        }

        private object _GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        public virtual object Get(TKey key, Type type)
        {
            try
            {
                var ct = _Cache[key];
                switch (ct.Mode)
                {
                    case TimeOutMode.AccessCount:
                        lock (ct)
                        {
                            if (ct.AccessCounter-- > 0)
                                return ct.Content;
                            Remove(key);
                            return _GetDefault(type);
                        }
                    case TimeOutMode.LastUse:
                        lock (ct)
                        {
                            if (DateTime.UtcNow.Subtract(ct.CreationTime).TotalSeconds <= ct.AccessCounter)
                            {
                                ct.CreationTime = DateTime.UtcNow;
                                return ct.Content;
                            }
                            Remove(key);
                            return _GetDefault(type);
                        }
                    case TimeOutMode.FromCreate:
                        if (DateTime.UtcNow.Subtract(ct.CreationTime).TotalSeconds <= ct.AccessCounter)
                            return ct.Content;
                        Remove(key);
                        return _GetDefault(type);
                    default:
                    case TimeOutMode.Never:
                        return ct.Content;
                }
            }
            catch (KeyNotFoundException)
            {
                return _GetDefault(type);
            }
        }

        /// <summary>
        /// Can not be implimented!
        /// </summary>
        /// <param name="key"></param>
        /// <param name="subKey"></param>
        /// <returns></returns>
        public virtual object Get(TKey key, TKey SubKey, Type type)
        {
            throw new NotImplementedException();
        }
    }
}
