using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RedisProvider.Tests
{
  [TestClass]
  public class RedisHashTests
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
      _container = new RedisContainer(_redisConnection, "hash");
    }

    [TestCleanup()]
    public void Cleanup()
    {
      _container.DeleteTrackedKeys().Wait();
    }


    [TestMethod]
    public async Task HashMisc()
    {
      var item1 = new RedisValueHash("key1");
      _container.AddToContainer(item1);

      await item1.Set("title", "goto statement considered harmful");
      await item1.Set("link", "http:go.bz");
      await item1.Set("poster", "user:123");
      await item1.Set("time", DateTime.Now.Ticks);
      await item1.Set("votes", 122);

      Assert.IsTrue((await item1.ContainsKey("poster")));
      Assert.IsTrue((await item1.Get("votes")) == 122);
      await foreach (var field in item1) Console.WriteLine($"{field.Key} = {field.Value}");


      Assert.IsTrue((await item1.Increment("votes")) == 123);
      Assert.IsTrue((await item1.Decrement("votes", 5)) == 118);

      await item1.Remove("link");
      Assert.IsTrue((await item1.Count()) == 4);
    }

    [TestMethod]
    public async Task HashAsDto() {
      var item1 = new RedisDtoHash<TestPOCO>("key2");
      _container.AddToContainer(item1);

      var poco1 = new TestPOCO { Id = 1, BirthDate = DateTime.Now, Name = "baby boy" };

      await item1.FromDto(poco1);
      var poco2 = await item1.ToDto();
      Assert.IsTrue(poco2.Name == "baby boy");
      Assert.IsTrue((await item1.ContainsKey("id")));

      await item1.Set(nameof(poco1.BirthDate).ToLower(), RedisObject.ToRedisValue(DateTime.Parse("1/1/2020")));
    }

    [TestMethod]
    public async Task HashAsDictionary() {
      var numbers = new RedisHash<string, int>("key3");
      _container.AddToContainer(numbers);

      var list = new List<KeyValuePair<string, int>>();
      for (int i = 1; i <= 10; i++) list.Add(new KeyValuePair<string, int>(i.ToString(), i));
      await numbers.SetRange(list);

      var values = await numbers.Values();
      var keys = await numbers.Keys();
      Assert.IsTrue(values.Count == keys.Count);
      Assert.IsTrue((await numbers.ContainsKey("2")));
      Assert.IsTrue((await numbers.Count()) == 10);

      await foreach (var kvp in numbers) Console.WriteLine($"{kvp.Key} - {kvp.Value}");

      var results = await numbers.GetRange(new[] { "2", "4" });
      CollectionAssert.AreEquivalent((ICollection)results, new[] { 2, 4 });

      Assert.IsTrue((await numbers.Increment("1", 4)) == 5);
    }

    [TestMethod]
    public async Task HashWithPoco() {
      var pocos = new RedisHash<string, TestPOCO>("key4");
      _container.AddToContainer(pocos);

      var susie = new TestPOCO { Id = 1, Name = "susie", BirthDate = DateTime.Parse("12/31/1999") };
      var freddie = new TestPOCO { Id = 2, Name = "freddie", BirthDate = DateTime.Parse("1/1/1990") };

      await pocos.Set("susie", susie);
      await pocos.Set("freddie", freddie);

      Assert.IsTrue((await pocos.Count()) == 2);

      var p1 = await pocos.Get("susie");
      Assert.IsTrue(susie.Id == p1.Id);

    }
  }
}
