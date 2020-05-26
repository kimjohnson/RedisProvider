using Microsoft.VisualStudio.TestTools.UnitTesting;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RedisProvider.Tests {

  [TestClass]
  public class ContainerTests {

    public static RedisConnection _redisConnection;
    public RedisContainer _container;

    [ClassInitialize]
    public static void ClassInit(TestContext context) {
      _redisConnection = new RedisConnection("127.0.0.1:6379,abortConnect=false", null);
    }

    [TestInitialize]
    public void Init() {
      _container = new RedisContainer(_redisConnection, "misc");
    }

    [TestMethod]
    public async Task KeyCreation() {
      var itemkey1 = _container.GetKey<RedisItem<string>>("key1");
      await itemkey1.Set("hello");

      var itemkey2 = _container.AddToContainer(new RedisItem<string>("key2"));
      await itemkey2.Set("world");

      var doccreator = _container.GetKeyTemplate<RedisItem<string>>("doc:{0}");
      var doc1 = doccreator.GetKey(1);
      await doc1.Set("first document");

      var doc2 = doccreator.GetKey(2);
      await doc2.Set("second document");

      foreach (var k in _container.TrackedKeys) Console.WriteLine(k);
    }


    [TestMethod]
    public async Task KeyMethods() {
      
      Assert.IsFalse(await _container.DeleteKey("not-exists"));
      Assert.IsFalse(await _container.KeyExists("not-exists"));
      var key = _container.GetKey<RedisItem<int>>("intkey");
      await key.Set(1);
      Assert.IsTrue(await _container.KeyExists("intkey"));
      Assert.IsTrue(await _container.DeleteKey(key.KeyName, false));
    }


    [TestCleanup()]
    public void Cleanup() {
      _container.DeleteTrackedKeys().Wait();
    }
  }
}
