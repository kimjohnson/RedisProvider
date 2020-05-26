using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace RedisProvider.Tests {
  [TestClass]
  public class RedisListTests
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
      _container = new RedisContainer(_redisConnection, "list");
    }

    [TestCleanup()]
    public void Cleanup()
    {
      _container.DeleteTrackedKeys().Wait();
    }


    [TestMethod]
    public async Task ListDates()
    {
      var listDates = new RedisList<DateTime>("keydates");
      _container.AddToContainer(listDates);

      for (int i = 0; i < 10; i++) await listDates.AddLast(DateTime.Now.AddDays(i));
      await listDates.AddFirst(DateTime.Now.AddDays(-1));

      Assert.IsTrue((await listDates.Count()) == 11); 

      // can pop
      var first = await listDates.RemoveFirst();
      var last = await listDates.RemoveLast();

      // can sort
      var sorted = await listDates.Sort(StackExchange.Redis.Order.Descending, StackExchange.Redis.SortType.Alphabetic);
      foreach (var d in sorted)
      {
        Console.WriteLine(d.ToShortDateString());
      }
    }


    [TestMethod]
    public async Task ListMixed()
    {
      var alist = new RedisValueList("alist");
      _container.AddToContainer(alist);

      await alist.AddFirst("a", "b", "c");
      await alist.AddLast(1, 2, 3);

      Assert.IsTrue((await alist.Count()) == 6);

      var anotherList = _container.AddToContainer(new RedisValueList("anotherList"));
      await alist.SortAndStore(anotherList, sortType: StackExchange.Redis.SortType.Alphabetic);
      await foreach (var item in anotherList) Console.WriteLine(item);
    }

    [TestMethod]
    public async Task ListMisc()
    {
      var blist = new RedisList<string>("blist");
      _container.AddToContainer(blist);

      await blist.AddFirst("a", "b", "c");
      await blist.AddLast("x", "y", "z");

      var b1 = await blist.Index(0);
      Assert.IsTrue((await blist.Count()) == 6);
      Assert.IsTrue((await blist.Index(0)) == "c");
      await blist.Trim(1, 4);
      var items = await blist.Range(0, -1);
      
      var anotherList = _container.AddToContainer(new RedisList<string>("anotherList"));
      await blist.PopPush(anotherList);
      var b2 = await anotherList.Last();
      Assert.IsTrue((await anotherList.Last()) == "y");
    }

  }
}
