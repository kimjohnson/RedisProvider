using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisProvider {

  /// <summary>
  /// A RedisSet holding RedisValue elements.  The RedisValueSet allows the set to contain different data types.
  /// </summary>
  public class RedisValueSet : RedisSet<RedisValue> {
    public RedisValueSet(string keyName) : base(keyName) {}
  }

  /// <summary>
  /// Encapsulate commands for the Redis SET data type.  A SET holds unique, unsorted elements.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class RedisSet<T> : RedisObject, IAsyncEnumerable<T> {

    public RedisSet(string keyName) : base(keyName) {}

    public new RedisSet<T> WithTx(RedisTransactionProxy proxy) {
      return base.WithTx(proxy) as RedisSet<T>;
    }

    public new RedisSet<T> WithBatch(RedisBatchProxy proxy) {
      return base.WithBatch(proxy) as RedisSet<T>;
    }

    /// <summary>
    /// Performs Redis SCARD command.  
    /// </summary>
    public Task<long> Count() {
      return Executor.SetLengthAsync(KeyName);
    }

    /// <summary>
    /// Performs Redis SADD command.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public Task Add(T element) {
      return Executor.SetAddAsync(KeyName, ToRedisValue(element));
    }

    /// <summary>
    /// Performs Redis SADD command.
    /// </summary>
    /// <param name="elements"></param>
    /// <returns></returns>
    public Task<long> AddRange(IEnumerable<T> elements) {
      var values = elements.Select(e => ToRedisValue(e)).ToArray();
      return Executor.SetAddAsync(KeyName, values);
    }

    /// <summary>
    /// Performs Redis SREM command.
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    public Task<bool> Remove(T element) {
      return Executor.SetRemoveAsync(KeyName, ToRedisValue(element));
    }

    /// <summary>
    /// Performs Redis SREM command. 
    /// </summary>
    /// <param name="elements"></param>
    /// <returns></returns>
    public Task<long> RemoveRange(IEnumerable<T> elements) {
      var values = elements.Select(e => ToRedisValue(e)).ToArray();
      return Executor.SetRemoveAsync(KeyName, values);
    }

    /// <summary>
    /// Performs Redis SPOP command.  Remove and return one or more random elements.
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public Task<IList<T>> Pop(long count = 1) {
      return Executor.SetPopAsync(KeyName, count)
        .ContinueWith<IList<T>>(r => r.Result.Select(v => ToElement<T>(v)).ToList(),
        TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Performs Redis SRANDMEMBER command.  Return one or more random elements.
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public Task<IList<T>> Peek(long count = 1) {
      return Executor.SetRandomMembersAsync(KeyName, count)
             .ContinueWith<IList<T>>(r => r.Result.Select(v => ToElement<T>(v)).ToList(),
              TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Performs Redist SISMEMBER command.
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    public Task<bool> Contains(T element) {
      return Executor.SetContainsAsync(KeyName, ToRedisValue(element));
    }

    /// <summary>
    /// Performs Redis SORT command.
    /// </summary>
    /// <param name="order"></param>
    /// <param name="sortType"></param>
    /// <param name="skip"></param>
    /// <param name="take"></param>
    /// <param name="byKeyNamePattern"></param>
    /// <param name="getKeyNamePattern"></param>
    public Task<IList<T>> Sort(Order order = Order.Ascending, SortType sortType = SortType.Numeric, long skip = 0, long take = -1, string byKeyNamePattern = null, string[] getKeyNamePattern = null) {
      var getKeys = getKeyNamePattern == null ? null : getKeyNamePattern.Select(s => (RedisValue)s).ToArray();
      return Executor.SortAsync(KeyName, skip, take, order, sortType, byKeyNamePattern, getKeys)
             .ContinueWith<IList<T>>(r => r.Result.Select(v => ToElement<T>(v)).ToList(),
              TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Performs Redis SORT command with STORE option. 
    /// </summary>
    /// <param name="destinationSet"></param>
    /// <param name="order"></param>
    /// <param name="sortType"></param>
    /// <param name="skip"></param>
    /// <param name="take"></param>
    /// <param name="byKeyNamePattern"></param>
    /// <param name="getKeyNamePattern"></param>
    /// <returns></returns>
    public Task<long> SortAndStore(RedisSet<T> destinationSet, Order order = Order.Ascending, SortType sortType = SortType.Numeric, long skip = 0, long take = -1, string byKeyNamePattern = null, string[] getKeyNamePattern = null) {
      var getKeys = getKeyNamePattern == null ? null : getKeyNamePattern.Select(s => (RedisValue)s).ToArray();
      return Executor.SortAndStoreAsync(destinationSet.KeyName, KeyName, skip, take, order, sortType, byKeyNamePattern, getKeys);
    }

    /// <summary>
    /// Performs Redis SDIFF command.
    /// </summary>
    /// <param name="otherSets"></param>
    /// <returns></returns>
    public Task<IList<T>> Difference(params RedisSet<T>[] otherSets) {
      var keys = (new RedisKey[] { (RedisKey)KeyName }).Concat(otherSets.Select(s => (RedisKey)s.KeyName)).ToArray();
      return Executor.SetCombineAsync(SetOperation.Difference, keys)
             .ContinueWith<IList<T>>(r => r.Result.Select(v => ToElement<T>(v)).ToList(),
              TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Performs Redis SDIFFSTORE command.
    /// </summary>
    /// <param name="destination"></param>
    /// <param name="otherSets"></param>
    /// <returns></returns>
    public Task<long> DifferenceStore(RedisSet<T> destination, params RedisSet<T>[] otherSets) {
      var keys = (new RedisKey[] { (RedisKey)KeyName }).Concat(otherSets.Select(s => (RedisKey)s.KeyName)).ToArray();
      return Executor.SetCombineAndStoreAsync(SetOperation.Difference, destination.KeyName, keys);
    }

    /// <summary>
    /// Performs Redis SINTER command.
    /// </summary>
    /// <param name="otherSets"></param>
    /// <returns></returns>
    public Task<IList<T>> Intersect(params RedisSet<T>[] otherSets) {
      var keys = (new RedisKey[] { (RedisKey)KeyName }).Concat(otherSets.Select(s => (RedisKey)s.KeyName)).ToArray();
      return Executor.SetCombineAsync(SetOperation.Intersect, keys)
             .ContinueWith<IList<T>>(r => r.Result.Select(v => ToElement<T>(v)).ToList(),
              TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Performs Redis SINTERSTORE command.
    /// </summary>
    /// <param name="destination"></param>
    /// <param name="otherSets"></param>
    /// <returns></returns>
    public Task<long> IntersectStore(RedisSet<T> destination, params RedisSet<T>[] otherSets) {
      var keys = (new RedisKey[] { (RedisKey)KeyName }).Concat(otherSets.Select(s => (RedisKey)s.KeyName)).ToArray();
      return Executor.SetCombineAndStoreAsync(SetOperation.Intersect, destination.KeyName, keys);
    }

    /// <summary>
    /// Performs Redis SUNION command.
    /// </summary>
    /// <param name="otherSets"></param>
    /// <returns></returns>
    public Task<IList<T>> Union(params RedisSet<T>[] otherSets) {
      var keys = (new RedisKey[] { (RedisKey)KeyName }).Concat(otherSets.Select(s => (RedisKey)s.KeyName)).ToArray();
      return Executor.SetCombineAsync(SetOperation.Union, keys)
             .ContinueWith<IList<T>>(r => r.Result.Select(v => ToElement<T>(v)).ToList(),
              TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Performs Redis SUNIONSTORE command.
    /// </summary>
    /// <param name="destination"></param>
    /// <param name="otherSets"></param>
    /// <returns></returns>
    public Task<long> UnionStore(RedisSet<T> destination, params RedisSet<T>[] otherSets) {
      if (destination == null) throw new ArgumentException("Destination set is required");
      var keys = (new RedisKey[] { (RedisKey)KeyName }).Concat(otherSets.Select(s => (RedisKey)s.KeyName)).ToArray();
      return destination.Executor.SetCombineAndStoreAsync(SetOperation.Union, destination.KeyName, keys);
    }

    /// <summary>
    /// Performs Redis SMEMBERS command.
    /// </summary>
    /// <returns></returns>
    public Task<IList<T>> ToList() {
      return Executor.SetMembersAsync(KeyName)
             .ContinueWith<IList<T>>(r => r.Result.Select(v => ToElement<T>(v)).ToList(),
              TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Perform Redis SSCAN to asynchronously enumerate the set.
    /// </summary>
    /// <returns></returns>
    public async IAsyncEnumerator<T> GetAsyncEnumerator() {
      await foreach (var entry in Executor.SetScanAsync(KeyName)) {
        yield return ToElement<T>(entry);
      }
    }

    IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken) {
      return GetAsyncEnumerator();
    }


  }
}
