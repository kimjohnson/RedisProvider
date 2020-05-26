using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Threading.Tasks;

namespace RedisProvider.Tests {
  [TestClass]
  public class RedisItemTests
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
      _container = new RedisContainer(_redisConnection, "item");
    }

    [TestCleanup()]
    public void Cleanup()
    {
      _container.DeleteTrackedKeys().Wait();
    }


    [TestMethod]
    public async Task ItemStringValue()
    {
      var item1 = new RedisItem<string>("key1");
      _container.AddToContainer(item1);

      await item1.Set("Hello world");
      await item1.SetRange(6, "Dolly");
      var s1 = await item1.Get();
      Assert.IsTrue(s1 == "Hello Dolly");
      await item1.Append("!");
      var l = await item1.StringLength();
      Assert.IsTrue(l == 12);
    }

    [TestMethod]
    public async Task ItemNumericValue()
    {
      var item1 = new RedisValueItem("key2");
      _container.AddToContainer(item1);

      await item1.Set(100);
      Assert.IsTrue((await item1.Decrement(1)) == 99);
      Assert.IsTrue((await item1.Increment(1)) == 100);
      Assert.IsTrue((await item1.GetSet(200)) == 100);
      Assert.IsTrue((await item1.Get()) == 200);
    }

    [TestMethod]
    public async Task ItemWithPOCO()
    {
      var onething = new RedisItem<TestPOCO>("onething");
      var anotherthing = new RedisItem<TestPOCO>("anotherthing");
      _container.AddToContainer(onething);
      _container.AddToContainer(anotherthing);

      // can set
      await onething.Set(new TestPOCO { Id = 1, Name = "freddie", BirthDate = DateTime.Parse("1/1/1990") });
      await anotherthing.Set(new TestPOCO { Id = 2, Name = "susie", BirthDate = DateTime.Parse("12/31/1999") });

      // can get
      var t1 = await onething.Get();
      var t2 = await anotherthing.Get();
      Assert.IsTrue(t1.Id == 1);
      Assert.IsTrue(t2.Name == "susie");
    }

    [TestMethod]
    public async Task BitmapBasics() {
      var bitmap = new RedisBitmap("bit1");
      _container.AddToContainer(bitmap);

      byte[] bytes = new byte[] { 0xff, 0xf0, 0x00 };
      await bitmap.Set(bytes);

      var b1 = await bitmap.Get();
      CollectionAssert.AreEquivalent(b1, bytes);

      Assert.IsTrue((await bitmap.BitPosition(false)) == 12);
      Assert.IsTrue((await bitmap.GetBit(9)));
      Assert.IsTrue((await bitmap.BitCount()) == 12);

      await bitmap.SetBit(9, false);
      Assert.IsTrue((await bitmap.BitCount()) == 11);
    }

    [TestMethod]
    public async Task BitmapOps() {
      var bitmap1 = new RedisBitmap("key1");
      _container.AddToContainer(bitmap1);
      var bitmap2 = new RedisBitmap("key2");
      _container.AddToContainer(bitmap2);
      var bitmap3 = new RedisBitmap("key3");
      _container.AddToContainer(bitmap3);

      await bitmap1.Set(new byte[] { 0x11 });
      await bitmap2.Set(new byte[] { 0x22 });
      await bitmap3.Set(new byte[] { 0x44 });

      var destmap = new RedisBitmap("destkey");
      _container.AddToContainer(destmap);

      // or
      await RedisBitmap.BitwiseOp(StackExchange.Redis.Bitwise.Or, destmap, new RedisBitmap[] { bitmap1, bitmap2, bitmap3 });
      var x = await destmap.Get();
      Assert.IsTrue(x[0] == 0x77);
    }

    [TestMethod]
    public async Task BitmapOpsPart2() {
      var bitmap1 = new RedisBitmap("key1");
      _container.AddToContainer(bitmap1);
      var bitmap2 = new RedisBitmap("key2");
      _container.AddToContainer(bitmap2);

      await bitmap1.Set(Encoding.Default.GetBytes("foobar"));
      await bitmap2.Set(Encoding.Default.GetBytes("abcdef"));

      var destmap = new RedisBitmap("destkey");
      _container.AddToContainer(destmap);

      // and
      var r = await RedisBitmap.BitwiseOp(StackExchange.Redis.Bitwise.And, destmap, new RedisBitmap[] { bitmap1, bitmap2 });
      var x = await destmap.Get();
      var s = Encoding.Default.GetString(x);
      Assert.IsTrue(s == "`bc`ab");
    }

  }
}
