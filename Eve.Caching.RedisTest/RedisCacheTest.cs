using Eve.Caching.Redis;
using System;
using System.Threading;
using Xunit;

namespace Eve.Caching.RedisTest
{
    public class RedisCacheTest
    {
        private static ICacheProvider<string, testObj> cache = 
            new RedisCacheProvider<testObj>(
                StackExchange.Redis.ConfigurationOptions.Parse("127.0.0.1:6379,defaultDatabase=3"));
        private const int _Rounds = 10000;
        public class testObj
        {
            public int I;
        }

        [Fact]
        public void ReadWrite()
        {
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
            cache.Cache("tst", new testObj());
            cache.Remove("tst");
            Assert.Null(cache.Get<testObj>("tst"));
        }

        [Fact]
        public void HasKey()
        {
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
            cache.Cache("tst", new testObj(), TimeOutMode.FromCreate, 1);
            Thread.Sleep(500);
            Assert.NotNull(cache.Get<testObj>("tst"));
            Thread.Sleep(300);
            Assert.NotNull(cache.Get<testObj>("tst"));
            Thread.Sleep(900);
            Assert.Null(cache.Get<testObj>("tst"));
        }

        [Fact]
        public void ExpierDate()
        {
            cache.Cache("tst", new testObj(), DateTime.UtcNow.AddSeconds(2));
            Thread.Sleep(500);
            Assert.NotNull(cache.Get<testObj>("tst"));
            Thread.Sleep(300);
            Assert.NotNull(cache.Get<testObj>("tst"));
            Thread.Sleep(900);
            Assert.Null(cache.Get<testObj>("tst"));
        }

        [Fact]
        public void ExpierAccess()
        {
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
            cache.Cache("tst", new testObj(), TimeOutMode.AccessCount, 2);
            Assert.NotNull(cache.Get<testObj>("tst"));
            Assert.NotNull(cache.Get<testObj>("tst"));
            Assert.Null(cache.Get<testObj>("tst"));
        }

        [Fact]
        public void ReadWrite_Subkey()
        {
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
            cache.Cache("rm", "tst", new testObj(), TimeOutMode.AccessCount, 1);
            Assert.True(cache.HasKey("rm", "tst"));
            cache.Remove("rm", "tst");
            Assert.False(cache.HasKey("rm", "tst"));
        }

        [Fact]
        public void HasKey_Subkey()
        {
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
