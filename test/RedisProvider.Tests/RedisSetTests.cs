using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Threading.Tasks;

namespace RedisProvider.Tests {
  [TestClass]
  public class RedisSetTests {
    public static RedisConnection _redisConnection;
    public RedisContainer _container;

    [ClassInitialize]
    public static void ClassInit(TestContext context) {
      _redisConnection = new RedisConnection("127.0.0.1:6379,abortConnect=false", null);
    }

    [TestInitialize]
    public void Init() {
      _container = new RedisContainer(_redisConnection, "set");
    }

    [TestCleanup()]
    public void Cleanup() {
      _container.DeleteTrackedKeys().Wait();
    }


    [TestMethod]
    public async Task SetNumbersMisc() {
      var setNumbers = new RedisSet<long?>("myNumbers");
      _container.AddToContainer(setNumbers);

      for (int i = 1; i <= 10; i++) await setNumbers.Add(i);

      Assert.IsTrue((await setNumbers.Count()) == 10);
      Assert.IsTrue((await setNumbers.TimeToLive()) == -1);

      // can peek
      var result = await setNumbers.Peek(2);
      foreach (var i in result) Console.WriteLine($"peeked {i}");

      // can enumerate
      await foreach (var i in setNumbers)
        Console.WriteLine($"returned {i}");

    }

    [TestMethod]
    public async Task SetAlphaMisc() {
      var setAlpha = new RedisValueSet("myStrings");
      _container.AddToContainer(setAlpha);

      for (int i = 1; i <= 10; i++) await setAlpha.Add(i.ToString());

      // can set expiration
      await setAlpha.Expire(300);

      // can get count and expiration
      Console.WriteLine($"set contains {(await setAlpha.Count())}");
      Console.WriteLine($"set ttl is {(await setAlpha.TimeToLive())}");

      // can remove and pop
      await setAlpha.Remove("2");
      var removed = await setAlpha.Pop(2);
      foreach (var i in removed) Console.WriteLine($"removed {i}");

      // can enumerate
      await foreach (var i in setAlpha)
        Console.WriteLine($"returned {i}");
    }

    [TestMethod]
    public async Task Difference() {
      var s1 = _container.AddToContainer(new RedisSet<string>("key1"));
      await s1.AddRange(new[] { "a", "b", "c" });

      var s2 = _container.AddToContainer(new RedisSet<string>("key2"));
      await s2.AddRange(new[] { "c", "d", "e" });

      var diff1 = await s1.Difference(s2);
      CollectionAssert.AreEquivalent(new string[] { "a", "b" }, (ICollection)diff1);

      var diff2 = await s2.Difference(s1);
      CollectionAssert.AreEquivalent(new string[] { "d", "e" }, (ICollection)diff2);
    }

    [TestMethod]
    public async Task DifferenceStore() {
      var s1 = _container.AddToContainer(new RedisSet<string>("key1"));
      await s1.AddRange(new[] { "a", "b", "c" });

      var s2 = _container.AddToContainer(new RedisSet<string>("key2"));
      await s2.AddRange(new[] { "c", "d", "e" });

      var s3 = _container.AddToContainer(new RedisSet<string>("key3"));

      await s1.DifferenceStore(s3, s2);

      var diffElements = await s3.ToList();
      CollectionAssert.AreEquivalent((ICollection)diffElements, new string[] { "a", "b" });

    }

    [TestMethod]
    public async Task Sort() {
      var s1 = _container.AddToContainer(new RedisSet<string>("key1"));
      await s1.AddRange(new[] { "a", "c", "d", "b", "f", "e" });

      var result = await s1.Sort(sortType: StackExchange.Redis.SortType.Alphabetic);
      CollectionAssert.AreEquivalent((ICollection)result, new string[] { "a", "b", "c", "d", "e", "f" });

    }
  }
}
