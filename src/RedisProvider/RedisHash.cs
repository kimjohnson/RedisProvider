using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using StackExchange.Redis;
using System.Threading.Tasks;
using System.Threading;

namespace RedisProvider {

  /// <summary>
  /// A RedisHash which provides for mapping hash fields to the properties of a DTO.
  /// </summary>
  /// <typeparam name="TDto"></typeparam>
  public class RedisDtoHash<TDto> : RedisValueHash {

    public RedisDtoHash(string keyName) : base(keyName) {}

    /// <summary>
    /// Return an instance of the specified DTO built from the key-value pairs in the hash.
    /// </summary>
    /// <returns></returns>
    public Task<TDto> ToDto() {

      var props = typeof(TDto).GetProperties();
      var t1 = GetRange(props.Select(p => ToRedisValue(p.Name.ToLower())).ToArray());

      var t2 = t1.ContinueWith<TDto>((r, _) => {
        TDto dto = Activator.CreateInstance<TDto>();
        for (int i = 0; i < props.Count(); i++) {
          props[i].SetValue(dto, ToElement(props[i].PropertyType, r.Result[i]));
        }
        return dto;
      }, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
      return t2;
    }

    /// <summary>
    /// Sets key-value pairs in the hash using the DTO properties and values.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    public Task FromDto(TDto dto) {
      var props = typeof(TDto).GetProperties();
      var entries = new List<KeyValuePair<RedisValue, RedisValue>>();
      foreach (var p in props) {
        entries.Add(new KeyValuePair<RedisValue, RedisValue>(p.Name.ToLower(), ToRedisValue(p.GetValue(dto))));
      }
      return SetRange(entries);
    }

    public new RedisDtoHash<TDto> WithTx(RedisTransactionProxy proxy) {
      return base.WithTx(proxy) as RedisDtoHash<TDto>;
    }

    public new RedisDtoHash<TDto> WithBatch(RedisBatchProxy proxy) {
      return base.WithBatch(proxy) as RedisDtoHash<TDto>;
    }
  }

  /// <summary>
  /// A RedisHash containing RedisValue keys and values.  The RedisValueHash allows for different types for key-value pairs within the hash.
  /// </summary>
  public class RedisValueHash : RedisHash<RedisValue, RedisValue> {
    public RedisValueHash(string keyName) : base(keyName) {}
  }

  /// <summary>
  /// Encapsulate commands for the Redis HASH data type.
  /// </summary>
  /// <typeparam name="TKey"></typeparam>
  /// <typeparam name="TValue"></typeparam>
  public class RedisHash<TKey, TValue> : RedisObject, IAsyncEnumerable<KeyValuePair<TKey, TValue>> {

    public RedisHash(string keyName) : base(keyName) {}

    public new RedisHash<TKey, TValue> WithTx(RedisTransactionProxy proxy) {
      return base.WithTx(proxy) as RedisHash<TKey, TValue>;
    }

    public new RedisHash<TKey, TValue> WithBatch(RedisBatchProxy proxy) {
      return base.WithBatch(proxy) as RedisHash<TKey, TValue>;
    }

    /// <summary>
    /// Performs Redis HKEYS command.
    /// </summary>
    public Task<ICollection<TKey>> Keys() {
      return Executor.HashKeysAsync(KeyName)
            .ContinueWith<ICollection<TKey>>(r => r.Result.Select(k => ToElement<TKey>(k)).ToList(),
            TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Performs Redis HVALS command.
    /// </summary>
    public Task<ICollection<TValue>> Values() {
      return Executor.HashValuesAsync(KeyName)
             .ContinueWith<ICollection<TValue>>(r => r.Result.Select(v => ToElement<TValue>(v)).ToList(),
            TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Performs Redis HLEN command.
    /// </summary>
    public Task<long> Count() {
      return Executor.HashLengthAsync(KeyName);
    }

    /// <summary>
    /// Performs Redis HDEL command to delete a hash field. 
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Task<bool> Remove(TKey key) {
      var rkey = ToRedisValue(key);
      return Executor.HashDeleteAsync(KeyName, rkey);
    }

    /// <summary>
    /// Performs Redis HDEL command to delete multiple hash fields.
    /// </summary>
    /// <param name="keys"></param>
    public Task<long> RemoveRange(ICollection<TKey> keys) {
      var fields = keys.Select(k => (RedisValue)ToRedisValue(k)).ToArray();
      return Executor.HashDeleteAsync(KeyName, fields);
    }

    /// <summary>
    /// Performs Redis HEXISTS command to determine if a hash field exists.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Task<bool> ContainsKey(TKey key) {
      return Executor.HashExistsAsync(KeyName, ToRedisValue(key));
    }

    /// <summary>
    /// Performs Redis HGET command to get the value of a hash field.
    /// </summary>t
    /// <param name="key"></param>
    /// <returns></returns>
    public Task<TValue> Get(TKey key) {
      return Executor.HashGetAsync(KeyName, ToRedisValue(key))
        .ContinueWith<TValue>(r => ToElement<TValue>(r.Result),
        TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Performs Redis HMGET command to get the values of multiple hash fields.
    /// </summary>
    /// <param name="keys"></param>
    /// <returns></returns>
    public Task<IList<TValue>> GetRange(ICollection<TKey> keys) {
      var fields = keys.Select(k => (RedisValue)ToRedisValue(k)).ToArray();
      return Executor.HashGetAsync(KeyName, fields)
             .ContinueWith<IList<TValue>>(r => r.Result.Select(v => ToElement<TValue>(v)).ToList(),
              TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Performs Redis HINCRBY command.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Task<long> Increment(TKey key, long value = 1) {
      return Executor.HashIncrementAsync(KeyName, ToRedisValue(key), value);
    }

    /// <summary>
    /// Performs Redis HINCRBYFLOAT command.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Task<double> Increment(TKey key, double value) {
      return Executor.HashIncrementAsync(KeyName, ToRedisValue(key), value);
    }

    /// <summary>
    /// Performs Redis HINCRBY command with a negative integer value).
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Task<long> Decrement(TKey key, long value = 1) {
      return Executor.HashDecrementAsync(KeyName, ToRedisValue(key), value);
    }

    /// <summary>
    /// Performs Redis HINCRBYFLOAT with a negative float value.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Task<double> Decrement(TKey key, double value) {
      return Executor.HashDecrementAsync(KeyName, ToRedisValue(key), value);
    }

    /// <summary>
    /// Performs Redis HSET or HSETNX command.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public Task<bool> Set(TKey key, TValue value, StackExchange.Redis.When when = StackExchange.Redis.When.Always) {
      var rfield = ToRedisValue(key);
      var rval = ToRedisValue(value);
      return Executor.HashSetAsync(KeyName, rfield, rval, when);
    }

    /// <summary>
    /// Performs Redis HSET command for multiple key-value pairs.
    /// </summary>
    /// <param name="items"></param>
    public Task SetRange(ICollection<KeyValuePair<TKey, TValue>> items) {
      var entries = items.Select(p => new HashEntry(ToRedisValue(p.Key), ToRedisValue(p.Value))).ToArray();
      return Executor.HashSetAsync(KeyName, entries);
    }

    /// <summary>
    /// Performs Redis HGETALL command.
    /// </summary>
    public Task<IList<KeyValuePair<TKey, TValue>>> ToList() {
      return Executor.HashGetAllAsync(KeyName)
       .ContinueWith<IList<KeyValuePair<TKey, TValue>>>(r => r.Result.Select(p => new KeyValuePair<TKey, TValue>(ToElement<TKey>(p.Name), ToElement<TValue>(p.Value))).ToList(),
        TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Performs Redis HSCAN to enumerate all key-value pairs in the hash.
    /// </summary>
    /// <returns></returns>
    public async IAsyncEnumerator<KeyValuePair<TKey, TValue>> GetAsyncEnumerator() {
      await foreach (var entry in Executor.HashScanAsync(KeyName)) {
        yield return new KeyValuePair<TKey, TValue>(ToElement<TKey>(entry.Name), ToElement<TValue>(entry.Value));
      }
    }

    IAsyncEnumerator<KeyValuePair<TKey, TValue>> IAsyncEnumerable<KeyValuePair<TKey, TValue>>.GetAsyncEnumerator(CancellationToken cancellationToken) {
      return GetAsyncEnumerator();
    }

  }
}
