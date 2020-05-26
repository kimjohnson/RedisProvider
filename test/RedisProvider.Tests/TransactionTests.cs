using Microsoft.VisualStudio.TestTools.UnitTesting;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RedisProvider.Tests {

  [TestClass]
  public class TransactionTests {

    public static RedisConnection _redisConnection;
    public RedisContainer _container;

    [ClassInitialize]
    public static void ClassInit(TestContext context) {
      _redisConnection = new RedisConnection("127.0.0.1:6379,abortConnect=false", null);
    }

    [TestInitialize]
    public void Init() {
      _container = new RedisContainer(_redisConnection, "tx");
    }


    [TestMethod]
    public async Task CreateTransaction() {
      var myKey = _container.GetKey<RedisList<string>>("mykey");

      var tx = _container.CreateTransaction("a");
      var t1 = tx.DeleteKey("mykey");
      var t2 = myKey.WithTx(tx).AddLast("a");
      var t3 = myKey.WithTx(tx).AddLast("b");
      var t4 = myKey.WithTx(tx).AddLast("c");
      var t5 = myKey.WithTx(tx).Index(1);
      await tx.Execute();

      await foreach (var c in myKey) Console.WriteLine(c);
    }

    [TestMethod]
    public async Task TransactionWithCondition() {
      var myKey = _container.GetKey<RedisList<string>>("mykey");
      await myKey.DeleteKey();

      var tx = _container.CreateTransaction();
      tx.AddCondition(Condition.KeyNotExists(myKey.KeyName));

      var t1 = myKey.WithTx(tx).AddLast("a");
      var t3 = myKey.WithTx(tx).AddLast("b");
      var t4 = myKey.WithTx(tx).AddLast("c");
      await tx.Execute();

      await foreach (var c in myKey) Console.WriteLine(c);
    }

    [TestMethod]
    public async Task TransactionReuse() {
      var key1 = _container.GetKey<RedisItem<string>>("key1");
      var key2 = _container.GetKey<RedisItem<string>>("key2");

      await key1.Set("abc");
      await key2.Set("def");

      var tx = _container.CreateTransaction("state1");

      var t1 = key1.WithTx(tx).Get();
      await tx.Execute();

      var t2 = key2.WithTx(tx).Get();
      await tx.Execute();

      Assert.IsTrue(t1.Result == "abc");
      Assert.IsTrue(t2.Result == "def");
    }

    [TestMethod]
    public async Task MutlipleTransactions() {

      var key1 = _container.GetKey<RedisItem<string>>("key1");
      var key2 = _container.GetKey<RedisItem<string>>("key2");

      await key1.Set("abc");
      await key2.Set("def");

      var tx1 = _container.CreateTransaction("a");
      var t1 = key1.WithTx(tx1).Get();

      var tx2 = _container.CreateTransaction("b");
      var t2 = key2.WithTx(tx1).Get();

      await Task.WhenAll(tx1.Execute(), tx2.Execute());

      Assert.IsTrue(t1.Result == "abc");
      Assert.IsTrue(t2.Result == "def");
    }


    [TestMethod]
    public async Task TransactionInterleave() {

      var key1 = _container.GetKey<RedisItem<string>>("key1");
      var key2 = _container.GetKey<RedisItem<string>>("key2");
      var key3 = _container.GetKey<RedisItem<string>>("key3");

      await key1.Set("abc");
      await key2.Set("def");

      var tx = _container.CreateTransaction();
      var t1 = key1.WithTx(tx).Get();
      var t2 = key3.WithTx(tx).Set("ghi");

      var t3 = key2.Get();

      await Task.WhenAll(t3, tx.Execute());
      Assert.IsTrue(t3.Result == "def");
    }


    [TestMethod]
    public async Task TransactionAlternate() {
      var key1 = _container.GetKey<RedisItem<string>>("key1");
      var key2 = _container.GetKey<RedisItem<string>>("key2");

      await key1.Set("abc");
      await key2.Set("def");

      var tx = _container.CreateTransaction();

      tx.AddTask(() => key1.Get());
      tx.AddTask(() => key2.Get());

      await tx.Execute();
      var tasks = tx.Tasks;
      var t1 = tasks[0] as Task<string>;
      var t2 = tasks[1] as Task<string>;

      Assert.IsTrue(t1.Result == "abc");
      Assert.IsTrue(t2.Result == "def");
    }

    [TestMethod]
    public async Task CreateBatch() {
      var myKey = _container.GetKey<RedisSet<string>>("mybatchkey");

      var batch = _container.CreateBatch();
      var t2 = myKey.WithBatch(batch).Add("a");
      var t3 = myKey.WithBatch(batch).Add("b");
      var t4 = myKey.WithBatch(batch).Add("c");
      await batch.Execute();

      await foreach (var c in myKey) Console.WriteLine(c);
    }

    [TestMethod]
    public async Task BatchAlternate() {
      var myKey = _container.GetKey<RedisSet<string>>("mybatchkey");

      var batch = _container.CreateBatch();
      batch.AddTask(() => myKey.Add("a"));
      batch.AddTask(() => myKey.Add("b"));
      batch.AddTask(() => myKey.Add("c"));
      await batch.Execute();

      var tasks = batch.Tasks;
      var t1 = tasks[0] as Task<string>;
      var t2 = tasks[1] as Task<string>;
      var t3 = tasks[2] as Task<string>;

      await foreach (var c in myKey) Console.WriteLine(c);
    }


    private Task Dosomething() {
      return Task.Run(() => Console.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId));
    }

    [TestCleanup()]
    public void Cleanup() {
      _container.DeleteTrackedKeys().Wait();
    }
  }
}
