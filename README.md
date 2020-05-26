# RedisProvider
.NET Redis container and strongly typed data objects

Strongly-typed data objects encapsulate the commands specific to the common [data types](https://redis.io/topics/data-types-intro):
| Class | Redis data type |
| ----  | ----|
| RedisItem&lt;T> | "string" |
| RedisBitmap  |  bit array |
| RedisList&lt;T> | LIST |
| RedisSet&lt;T>  | SET |
| RedisSortedSet&lt;T> | ZSET |
| RedisHash&lt;T> | HASH |
| RedisDtoHash&lt;T> | maps a hash to a DTO |
| RedisObject | Base class for all key types |

The **RedisContainer** provides a key namespace and allows for an intutive model of the Redis keys used within an application.  The container optionally keeps track of keys used, but does not cache any data.

The strongly-typed objects do not hold state, but instead provide wrappers around only the Redis commands allowed for the key's data type.    

Uses asynchronous I/O exclusively.

StackExchange.Redis dependency.

# Usage

## Basics 

    // Create a connection and container
    var cn = new RedisConnection("127.0.0.1:6379,abortConnect=false");
    var container = new RedisContainer(cn, "test");

    // Keys are managed by the container.  The key may already exist in the db.  Or not. 
    var key1 = container.GetKey<RedisItem<string>>("key1");
    await key1.Set("Hello world");
    // key1.KeyName is "test:key1"

    var key2 = container.GetKey<RedisItem<int>>("key2");
    await key2.Set(100);
    await key2.Increment(1);

    // Automatic json serialization/deserialization
    var key3 = container.GetKey<RedisItem<Customer>>("key3");
    await key3.Set(new Customer { Id = 1, Name = "freddie" });
    var aCust = await key3.Get();
    
    // All key types support basic commands:
    key1.DeleteKey()
    key1.Expire(30)
    key1.ExpireAt(DateTime.Now.AddHours(1))
    key1.IdleTime()
    key1.KeyExists()
    key1.Persist()
    key1.TimeToLive()
    
    // The generic parameter for any type can be an IConvertible, byte[] or POCO/DTO. Examples:
    var longitem = container.GetKey<RedisItem<long>>("longitem");
    var intlist = container.GetKey<RedisList<int>("intlist");
    var customers = container.GetKey<RedisHash<string, Customer>>("customers");
    var cust1 = container.GetKey<RedisDtoHash<Customer>>("cust1");
    

## RedisItem 

      var key1 = _container.GetKey<RedisItem<string>>("key1");

      // Get/set
      await key1.Set("hello");
      var a = await key1.Get();

      // String commands
      await key1.SetRange(6, "Dolly");
      var b = key1.GetRange(6, 10);
      await key1.Append("!");
      var l = await key1.StringLength();

      // Numeric commands
      var key2 = _container.GetKey<RedisItem<int>>("key2");
      await key2.Set(1);
      await key2.Increment(10);
      await key2.Decrement();
      var n = await key2.Get();

## RedisBitmap

      // Basic bit ops
      var bits = container.GetKey<RedisBitmap>("bits");
  
      await bits.Set(new byte[] { 0xff, 0xf0, 0x00 });
      var ct = await bits.BitCount();
      var p0 = await bits.BitPosition(false);
      var x = await bits.GetBit(9);
      await bits.SetBit(9, !x);
      var bytes = bits.Get();
      
      // Bitwise ops 
      var bitmap1 = _container.GetKey<RedisBitmap>("key1");
      var bitmap2 = _container.GetKey<RedisBitmap>("key2");
      var destmap = _container.GetKey<RedisBitmap>("dest");

      await bitmap1.Set(new byte[] { 0x11 });
      await bitmap2.Set(new byte[] { 0x22 });

      // OR
      await RedisBitmap.BitwiseOp(Bitwise.Or, destmap, new [] { bitmap1, bitmap2 });
      var d1bytes = await destmap.Get();

      // AND
      await RedisBitmap.BitwiseOp(Bitwise.And, destmap, new [] { bitmap1, bitmap2 });
      var xd2bytes = await destmap.Get();
  
  
  ## RedisList
  
      var numbers = _container.GetKey<RedisList<short>>("numbers");

      // Add 
      await numbers.AddFirst(1);
      await numbers.AddLast(5, 10, 15);
      await numbers.AddAfter(1, 3);
      await numbers.AddBefore(15, 12);

      // Access by index
      var ct = await numbers.Count();
      var i2 = await numbers.Index(2);
      var i0 = await numbers.First();
      var ix = await numbers.Last();
      await numbers.Set(0, 2);

      // Remove
      await numbers.RemoveFirst();
      await numbers.RemoveLast();
      await numbers.Remove(10);

      // Enumerate
      await foreach (var i in numbers) Console.Write(i);

      // Misc
      var sorted = await numbers.Sort(Order.Descending);
      var n3 = await numbers.Range(0, -1);
      await numbers.Trim(1, -1);

      var list2 = _container.GetKey<RedisList<short>>("destlist2");
      await numbers.SortAndStore(list2, Order.Descending);

      var list3 = _container.GetKey<RedisList<short>>("destlist3");
      await numbers.PopPush(list3);

## RedisSet

      var set1 = _container.GetKey<RedisSet<string>>("set1");

      // Add 
      await set1.Add("a");
      await set1.AddRange(new[] { "b", "c", "d", "e" });
      var has2 = await set1.Contains("2");
      var ct = await set1.Count();
      var allItems = await set1.ToList();

      // Enumerate
      await foreach (var i in set1) Console.Write(i);

      // Peek and pop
      var el1 = await set1.Peek();
      var el2 = await set1.Pop();

      // Remove
      await set1.Remove("b");
      await set1.RemoveRange(new[] { "c", "d" });

      // Set operators 

      var set2 = _container.GetKey<RedisSet<string>>("set2");
      await set2.AddRange(new[] { "a", "b", "c" });

      var differ = await set2.Difference(set1);
      var inter = await set1.Intersect(set2);
      var sort = await set2.Sort(Order.Descending, SortType.Alphabetic);

      var destSet = _container.GetKey<RedisSet<string>>("set3");
      await set1.UnionStore(destSet, set2);

## RedisSortedSet

      var zset = _container.GetKey<RedisSortedSet<string>>("key1");

      // Add
      await zset.Add("one", 1);
      await zset.Add("uno", 1);
      await zset.AddRange(new[] { (element: "two", score: 2.0), (element: "three", score: 3.0) });

      // Count
      var ct1 = await zset.Count();
      var ct2 = await zset.CountByScore(0, 1);
      var ct3 = await zset.CountByValue("ta", "zz");

      // Range
      var r1 = await zset.Range();
      var r2 = await zset.RangeByScore(0, 1);
      var r3 = await zset.RangeByValue("ta", "zz");
      var r4 = await zset.RangeWithScores(0, 1);

      // Enumerate
      await foreach (var item in zset) Console.WriteLine(item);

      // Misc
      var news1 = await zset.IncrementScore("three", 1);
      var rk = await zset.Rank("three");
      var s = await zset.Score("uno");
      //var el0 = await zset.Pop();

      // Sort
      var list = await zset.Sort(Order.Ascending, SortType.Alphabetic);

      // Remove
      await zset.Remove("one");
      await zset.RemoveRange(0, 0);
      await zset.RemoveRangeByScore(3);
      await zset.RemoveRangeByValue("two");

      // Union and intersect
      
## RedisHash

     // 1 - As a dictionary

      var key1 = _container.GetKey<RedisHash<string, int>>("key1");

      // Set
      await key1.Set("0", 0);
      var list = new List<KeyValuePair<string, int>>();
      for (int i = 1; i <= 10; i++) list.Add(new KeyValuePair<string, int>(i.ToString(), i));
      await key1.SetRange(list);

      // Misc
      await key1.Decrement("1");
      await key1.Increment("0");

      var ct = await key1.Count();
      var has = await key1.ContainsKey("2");

      var keys = await key1.Keys();
      var values = await key1.Values();
      var items = await key1.ToList();

      // get 
      var f1 = await key1.Get("2");
      var f2 = await key1.GetRange(new[] { "1", "5" });
      
      // Remove
      await key1.Remove("0");
      await key1.RemoveRange(new[] { "1", "2" });
 
      // Enumerate
      await foreach (var item in key1) Console.WriteLine($"{item.Key} - {item.Value}");

      //
      // 2 - As a DTO
      //
      var custKey = _container.GetKey<RedisDtoHash<Customer>>("cust:1");

      var cust1 = new Customer { Id = 1, Name = "safeway" };
      await custKey.FromDto(cust1);

      var cust1copy = await custKey.ToDto();
      
      //  
      // 3 - As untyped key-value pair hash
      // 
      var item1 = _container.GetKey<RedisValueHash>("key3");

      await item1.Set("title", "goto statement considered harmful");
      await item1.Set("link", "http:go.com");
      await item1.Set("poster", "user:123");
      await item1.Set("time", DateTime.Now.Ticks);
      await item1.Set("votes", 122);

      await foreach (var field in item1) Console.WriteLine($"{field.Key} = {field.Value}");
      
## Key creation

      // Easiest- - ask the container.  If the container is tracking key creation and the key was already
      // added to the container then that object is returned, otherwise a new object is created.
      // This does not create the key in the Redis database.
      
      var itemkey1 = _container.GetKey<RedisItem<string>>("key1");

      // Create a new object and add to the container.  This also does not create the key in the Redis database.
      
      var itemkey2 = _container.AddToContainer(new RedisItem<string>("key2"));

      // Templated key creation with KeyTemplate<T>
      // If using the common pattern of including the object ID in the key name, for example "user:1" or "user:1234", 
      // manually creating each key and ensuring both the data type and key name format are correct can be error prone.  
      // The KeyTemplate<T> acts as a factory for keys of the specified type and key name pattern.
      
      var doccreator = _container.GetKeyTemplate<RedisItem<string>>("doc:{0}");
      var doc1 = doccreator.GetKey(1);
      await doc1.Set("first document is doc:1");

      var doc2 = doccreator.GetKey(2);
      await doc2.Set("second document is doc:2");

      foreach (var k in _container.TrackedKeys) Console.WriteLine(k);
      
## Transactions and batches
Transactions and batches allow a group of commands to be sent to the Redis server as a unit and processed as a unit.  The commands
in a batch may not be processed in order.  Note that there is no concept of commit/rollback.  You must check the Task.Result of 
individual commands after execution for returned values.

      // A simple batch
      var key1 = _container.GetKey<RedisSet<string>>("key1");
      var batch = _container.CreateBatch();
      key1.WithBatch(batch).Add("a");
      key1.WithBatch(batch).Add("b");
      await batch.Execute();

      // A simple transaction
      var keyA = _container.GetKey<RedisItem<string>>("keya");
      var keyB = _container.GetKey<RedisItem<string>>("keyb");

      await keyA.Set("abc");
      await keyB.Set("def");

      var tx = _container.CreateTransaction();
      var t1 = keyA.WithTx(tx).Get();
      var t2 = keyB.WithTx(tx).Get();
      await tx.Execute();
      var a = t1.Result;
      var b = t2.Result;


