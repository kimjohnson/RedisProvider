using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace RedisProvider.Tests {
  [TestClass]
  public class RedisSortedSetTests
  {
    public static RedisConnection _redisConnection;
    public RedisContainer _container;

    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
      _redisConnection = new RedisConnection("127.0.0.1:6379,abortConnect=false", null);
    }

    [TestInitialize]
    public void Init()
    {
      _container = new RedisContainer(_redisConnection, "zset");
    }

    [TestCleanup()]
    public void Cleanup()
    {
      _container.DeleteTrackedKeys().Wait();
    }


    [TestMethod]
    public async Task SortedSetAddMisc()
    {
      var zset = new RedisSortedSet<string>("key1");
      _container.AddToContainer(zset);

      await zset.Add("one", 1);
      await zset.Add("uno", 1);
      await zset.AddRange(new[] { (element: "two", score: 2.0), (element: "three", score: 3.0) });

      Assert.IsTrue((await zset.Count()) == 4);
      Assert.IsTrue((await zset.CountByScore(2)) == 2);

      var elements = await zset.RangeWithScores();
      foreach (var pair in elements) Console.WriteLine($"{pair.element} - {pair.score}");

      var values = await zset.RangeByValue();
      foreach (var v in values) Console.WriteLine(v);

      var valuesDesc = await zset.RangeByScore(order: StackExchange.Redis.Order.Descending);
      foreach (var v in valuesDesc) Console.WriteLine(v);
    }


    public async Task AsyncAssertTrue<T>(Func<Task<T>> task, T comp) where T:IComparable{
      T result = await task();
      Assert.AreEqual(result, comp);
    }


    [TestMethod]
    public async Task SortedSetRanges() {
      var zset = new RedisValueSortedSet("key2");
      _container.AddToContainer(zset);

      foreach (var i in System.Linq.Enumerable.Range(97, 10)) await zset.Add(Convert.ToChar(i).ToString(), 0);

      Console.WriteLine($"set1 expiration is {(await zset.TimeToLive())}");

      Assert.IsTrue((await zset.Rank("b")) == 1);
      await zset.IncrementScore("b", 2);
      Assert.IsTrue((await zset.Score("b")) == 2);
      Assert.IsTrue((await zset.Rank("b")) == 9);

      var range1 = await zset.RangeByValue("b", "f");
      var n = await zset.CountByValue("b", "f");
      Assert.IsTrue(range1.Count == n);
      Assert.IsTrue((await zset.RemoveRangeByScore(1)) == 1);

      // not supported in my version of redis cli
      //var popmin = setNumbers.Pop();
      //var popmax = setNumbers.Pop(StackExchange.Redis.Order.Descending);

      await foreach (var i in zset)
        Console.WriteLine($"returned {i}");
    }

    [TestMethod]
    public async Task SortedSetOps() {
      var zset1 = new RedisSortedSet<string>("key1");
      _container.AddToContainer(zset1);
      await zset1.Add("one", 1);
      await zset1.Add("two", 2);

      var zset2 = new RedisSortedSet<string>("key2");
      _container.AddToContainer(zset2);
      await zset2.Add("one", 1);
      await zset2.Add("two", 2);
      await zset2.Add("three", 3);

      var outset1 = new RedisSortedSet<string>("key3");
      _container.AddToContainer(outset1);

      var n = await zset1.IntersectStore(outset1, new[] { zset2 }, new double[] { 2, 3 });
      Assert.IsTrue(n == 2);
      foreach (var pair in await outset1.RangeWithScores()) Console.WriteLine($"{pair.element} - {pair.score}");

      var outset2 = new RedisSortedSet<string>("key4");
      _container.AddToContainer(outset2);

      var n2 = await zset1.UnionStore(outset2, new[] { zset2 }, new double[] { 2, 3 });
      Assert.IsTrue(n2 == 3);
      foreach (var pair in await outset2.RangeWithScores()) Console.WriteLine($"{pair.element} - {pair.score}");
    }


  }
}
