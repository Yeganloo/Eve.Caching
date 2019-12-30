using Eve.Caching;
using Eve.Caching.Redis;
using System.Threading;
using Xunit;

namespace Eve.Caching.RedisTest
{
    public class RedisCacheTest
    {
        private const int _Rounds = 10;
        public class testObj
        {
            public int I;
        }

        public ICacheProvider<string, testObj> GetProvider()
        {
            string cnn = "127.0.0.1:6379,defaultDatabase=3";
            return new RedisCacheProvider<testObj>(StackExchange.Redis.ConfigurationOptions.Parse(cnn));
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
            cache.Cache("tst", new testObj());
            Assert.True(cache.HasKey("tst"));
            cache.Remove("tst");
            Assert.False(cache.HasKey("tst"));
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
            cache.Cache("hs", "tst", new testObj());
            Assert.True(cache.HasKey("hs", "tst"));
            cache.Remove("hs", "tst");
            Assert.False(cache.HasKey("hs", "tst"));
        }

    }
}
