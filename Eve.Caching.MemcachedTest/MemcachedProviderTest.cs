using System;
using Xunit;
using Eve.Caching.Memcached;
using Enyim.Caching.Memcached;
using System.Threading;

namespace Eve.Caching.MemcachedTest
{
    public class MemcachedProviderTest
    {
        private const int _Rounds = 10000;
        [Serializable]
        public class testObj
        {
            public int I;
        }

        public ICacheProvider<string, testObj> GetProvider()
        {
            MemcachedCluster cls = new MemcachedCluster("localhost");
            cls.Start();
            return new MemcachedCacheProvider<testObj>(cls);
        }


        [Fact]
        public void ReadWrite()
        {
            var cache = GetProvider();
            var tmp = new testObj[_Rounds];
            for (int i = 0; i < _Rounds; i++)
            {
                cache.Cache(i.ToString(), tmp[i] = new testObj { I = i });
            }
            for (int i = 0; i < _Rounds; i++)
            {
                Assert.Equal(tmp[i].I, cache.Get<testObj>(i.ToString()).I);
            }
        }

        [Fact]
        public void Remove()
        {
            var cache = GetProvider();
            cache.Cache("tst", new testObj());
            cache.Remove("tst");
            Assert.Null(cache.Get<testObj>("tst"));
        }

        [Fact]
        public void HasKey()
        {
            var cache = GetProvider();
            cache.Cache("tst", new testObj(), TimeOutMode.AccessCount, 1);
            Assert.True(cache.HasKey("tst"));
            cache.Get<testObj>("tst");
            Assert.False(cache.HasKey("tst"));

            cache.Cache("tst", new testObj(), TimeOutMode.FromCreate, 1);
            Assert.True(cache.HasKey("tst"));
            Thread.Sleep(1000);
            Assert.False(cache.HasKey("tst"));

            cache.Cache("tst", new testObj(), TimeOutMode.LastUse, 1);
            Assert.True(cache.HasKey("tst"));
            Thread.Sleep(1000);
            Assert.False(cache.HasKey("tst"));
        }

        [Fact]
        public void ExpierCreate()
        {
            var cache = GetProvider();
            cache.Cache("tstc", new testObj(), TimeOutMode.FromCreate, 3);
            Thread.Sleep(500);
            Assert.NotNull(cache.Get<testObj>("tstc"));
            Thread.Sleep(500);
            Assert.NotNull(cache.Get<testObj>("tstc"));
            Thread.Sleep(2000);
            Assert.Null(cache.Get<testObj>("tstc"));
        }

        [Fact]
        public void ExpierDate()
        {
            var cache = GetProvider();
            cache.Cache("tstd", new testObj(), DateTime.UtcNow.AddSeconds(3));
            Thread.Sleep(500);
            Assert.NotNull(cache.Get<testObj>("tstd"));
            Thread.Sleep(500);
            Assert.NotNull(cache.Get<testObj>("tstd"));
            Thread.Sleep(2000);
            Assert.Null(cache.Get<testObj>("tstd"));
        }

         [Fact]
         public void ExpierAccess()
         {
             var cache = GetProvider();
             cache.Cache("tst", new testObj(), TimeOutMode.LastUse, 2);
             Thread.Sleep(500);
             Assert.NotNull(cache.Get<testObj>("tst"));
             Thread.Sleep(500);
             Assert.NotNull(cache.Get<testObj>("tst"));
             Thread.Sleep(2000);
             Assert.Null(cache.Get<testObj>("tst"));
         }

         [Fact]
         public void ExpierCnt()
         {
             var cache = GetProvider();
             cache.Cache("tst", new testObj(), TimeOutMode.AccessCount, 2);
             Assert.NotNull(cache.Get<testObj>("tst"));
             Assert.NotNull(cache.Get<testObj>("tst"));
             Assert.Null(cache.Get<testObj>("tst"));
         }

        [Fact]
        public void ReadWrite_Subkey()
        {
            var cache = GetProvider();
            var tmp = new testObj[_Rounds];
            for (int i = 0; i < _Rounds; i++)
            {
                cache.Cache("rw", i.ToString(), tmp[i] = new testObj { I = i });
            }
            for (int i = 0; i < _Rounds; i++)
            {
                Assert.Equal(tmp[i].I, cache.Get<testObj>("rw", i.ToString()).I);
            }
        }

        [Fact]
        public void Remove_Subkey()
        {
            var cache = GetProvider();
            cache.Cache("rm", "tst", new testObj());
            cache.Remove("rm", "tst");
            Assert.Null(cache.Get<testObj>("rm", "tst"));
        }

        [Fact]
        public void HasKey_Subkey()
        {
            var cache = GetProvider();
            cache.Cache("hs", "tst", new testObj(), TimeOutMode.AccessCount, 1);
            Assert.True(cache.HasKey("hs", "tst"));
            cache.Get<testObj>("hs", "tst");
            Assert.False(cache.HasKey("hs", "tst"));

            cache.Cache("hs", "tst", new testObj(), TimeOutMode.FromCreate, 1);
            Assert.True(cache.HasKey("hs", "tst"));
            Thread.Sleep(1001);
            Assert.False(cache.HasKey("hs", "tst"));

            cache.Cache("hs", "tst", new testObj(), TimeOutMode.LastUse, 1);
            Assert.True(cache.HasKey("hs", "tst"));
            Thread.Sleep(1001);
            Assert.False(cache.HasKey("hs", "tst"));
        }
    }
}
