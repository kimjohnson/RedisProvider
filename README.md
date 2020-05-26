# RedisProvider
.NET Redis container and strongly typed data objects

Strongly-typed data objects encapsulate the commands specific to the common data types:
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

The **RedisContainer** can track RedisObjects, and optionally provide a namespace for all keys.

Uses asynchronous I/O exclusively.

# Usage

## Basics 

    // Create a connection and container
    var cn = new RedisConnection("127.0.0.1:6379,abortConnect=false");
    var container = new RedisContainer(cn, "test");

    // Keys are managed by the container.
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

## RedisBitMap

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
  
  
