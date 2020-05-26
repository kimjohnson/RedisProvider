using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedisProvider {

  /// <summary>
  /// A RedisItem containing a RedisValue element.
  /// </summary>
  public class RedisValueItem : RedisItem<RedisValue> {
    public RedisValueItem(string keyName) : base(keyName) {}
  }

  /// <summary>
  /// Encapsulate commands for the Redis "string" data type.  Use RedisBitmap if bit operations are required,
  /// and RedisValueItem if serialization/deserialization is not required.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class RedisItem<T> : RedisObject {

    public RedisItem(string keyName) : base(keyName) {}

    public new RedisItem<T> WithTx(RedisTransactionProxy proxy) {
      return base.WithTx(proxy) as RedisItem<T>;
    }

    public new RedisItem<T> WithBatch(RedisBatchProxy proxy) {
      return base.WithBatch(proxy) as RedisItem<T>;
    }

    /// <summary>
    /// Performs Redis GET command.
    /// </summary>
    /// <returns></returns>
    public Task<T> Get() {
      return Executor.StringGetAsync(KeyName)
        .ContinueWith<T>(r => ToElement<T>(r.Result), TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Performs Redis SET, SETEX, or SETNX command.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="expiration"></param>
    /// <param name="when"></param>
    /// <returns></returns>
    public Task<bool> Set(T value, TimeSpan? expiration = null, StackExchange.Redis.When when = StackExchange.Redis.When.Always) {
      return Executor.StringSetAsync(KeyName, ToRedisValue(value), expiration, when);
    }

    /// <summary>
    /// Performs Redis APPEND command.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public Task<long> Append(T value) {
      return Executor.StringAppendAsync(KeyName, ToRedisValue(value));
    }

    /// <summary>
    /// Performs Redis STRLEN command.
    /// </summary>
    /// <returns></returns>
    public Task<long> StringLength() {
      return Executor.StringLengthAsync(KeyName);
    }

    /// <summary>
    /// Performs Redis GETRANGE command to return a substring of the stored value.
    /// </summary>
    /// <param name="start">Offset start index</param>
    /// <param name="end">Offset end index</param>
    /// <returns></returns>
    public Task<T> GetRange(long start, long end) {
      return Executor.StringGetRangeAsync(KeyName, start, end)
            .ContinueWith<T>(r => ToElement<T>(r.Result), TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Performs Redis SETRANGE command to overwrite part of the stored value.
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Task<long> SetRange(long offset, T value) {
      return Executor.StringSetRangeAsync(KeyName, offset, ToRedisValue(value))
             .ContinueWith<long>(r => long.Parse(r.Result), TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Performs Redis INCR or INCRBY command.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public Task<long> Increment(long value = 1) {
      return Executor.StringIncrementAsync(KeyName, value);
    }

    /// <summary>
    /// Performs Redis INCRBYFLOAT command.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public Task<double> Increment(double value) {
      return Executor.StringIncrementAsync(KeyName, value);
    }

    /// <summary>
    /// Performs Redis DECR or DECRBY command.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public Task<long> Decrement(long value = 1) {
      return Executor.StringDecrementAsync(KeyName, value);
    }

    /// <summary>
    /// Performs Redis DECR or DECRBY command.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public Task<double> Decrement(double value) {
      return Executor.StringDecrementAsync(KeyName, value);
    }

    /// <summary>
    /// Performs Redis GETSET command.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public Task<T> GetSet(T value) {
      return Executor.StringGetSetAsync(KeyName, ToRedisValue(value))
          .ContinueWith<T>(r => ToElement<T>(r.Result), TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Performs Redis MGET command for multiple keys.
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    public static Task<IList<T>> GetMultiple(IList<RedisItem<T>> items) {
      var keys = items?.Select(i => (RedisKey)i.KeyName).ToArray();
      if (keys.Count() == 0) throw new ArgumentException("No keys passed");
      return items[0].Executor.StringGetAsync(keys)
             .ContinueWith<IList<T>>(r => r.Result.Select(v => ToElement<T>(v)).ToList(),
             TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Performs Redis MSET or MSETNX command for multiple keys.
    /// </summary>
    /// <param name="keyValuePairs"></param>
    /// <returns></returns>
    public static Task<bool> SetMultiple(IList<KeyValuePair<RedisItem<T>, T>> keyValuePairs, StackExchange.Redis.When when = When.Always) {
      var items = keyValuePairs?.Select(i => {
        var keyName = i.Key.KeyName;
        var keyValue = ToRedisValue(i.Value);
        return new KeyValuePair<RedisKey, RedisValue>(keyName, keyValue);
      }).ToArray();
      if (items.Count() == 0) throw new ArgumentException("No keys passed");
      return keyValuePairs[0].Key.Executor.StringSetAsync(items, when);
    }
  }
}
