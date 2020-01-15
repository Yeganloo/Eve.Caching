using Eve.Caching;
using System;
using System.Threading;
using Xunit;

namespace Eve.CachingTest
{
    public class SimpleCache
    {
        private const int _Rounds = 10000;
        public class testObj
        {
            public int I;
        }

        public ICacheProvider<string, testObj> GetProvider()
        {
            return new SimpleCacheProvider<testObj>();
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
                Assert.Equal(tmp[i], cache.Get<testObj>(i.ToString()));
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
        public void ExpierAccess()
        {
            var cache = GetProvider();
            cache.Cache("tst", new testObj(), TimeOutMode.LastUse, 1);
            Thread.Sleep(900);
            Assert.NotNull(cache.Get<testObj>("tst"));
            Thread.Sleep(900);
            Assert.NotNull(cache.Get<testObj>("tst"));
            Thread.Sleep(1001);
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
        public void ExpierCreate()
        {
            var cache = GetProvider();
            cache.Cache("tst", new testObj(), TimeOutMode.FromCreate, 1);
            Thread.Sleep(500);
            Assert.NotNull(cache.Get<testObj>("tst"));
            Thread.Sleep(300);
            Assert.NotNull(cache.Get<testObj>("tst"));
            Thread.Sleep(300);
            Assert.Null(cache.Get<testObj>("tst"));
        }

        [Fact]
        public void ExpierDate()
        {
            var cache = GetProvider();
            cache.Cache("tst", new testObj(), DateTime.UtcNow.AddSeconds(2));
            Thread.Sleep(500);
            Assert.NotNull(cache.Get<testObj>("tst"));
            Thread.Sleep(300);
            Assert.NotNull(cache.Get<testObj>("tst"));
            Thread.Sleep(900);
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
                Assert.Equal(tmp[i], cache.Get<testObj>("rw", i.ToString()));
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
