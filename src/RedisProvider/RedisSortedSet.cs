using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisProvider {

  /// <summary>
  /// A RedisSortedSet with RedisValue elements.  The RedisValueSortedSet allows the set to contain different data types.
  /// </summary>
  public class RedisValueSortedSet : RedisSortedSet<RedisValue> {
    public RedisValueSortedSet(string keyName) : base(keyName) {}
  }

  /// <summary>
  /// Encapsulate commands for the Redis ZSET data type. Every element in the sorted set has an associated score.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class RedisSortedSet<T> : RedisObject, IAsyncEnumerable<T> {

    public RedisSortedSet(string keyName) : base(keyName) {}

    public new RedisSortedSet<T> WithTx(RedisTransactionProxy proxy) {
      return base.WithTx(proxy) as RedisSortedSet<T>;
    }

    public new RedisSortedSet<T> WithBatch(RedisBatchProxy proxy) {
      return base.WithBatch(proxy) as RedisSortedSet<T>;
    }

    /// <summary>
    /// Performs Redis ZADD command.
    /// </summary>
    /// <returns></returns>
    public Task<bool> Add(T element, double score) {
      return Executor.SortedSetAddAsync(KeyName, ToRedisValue(element), score);
    }

    /// <summary>
    /// Performs Redis ZADD command.
    /// </summary>
    /// <param name="entries"></param>
    /// <returns></returns>
    public Task<long> AddRange(IEnumerable<(T element, double score)> entries) {
      var values = entries.Select(e => new SortedSetEntry(ToRedisValue(e.element), e.score)).ToArray();
      return Executor.SortedSetAddAsync(KeyName, values);
    }

    /// <summary>
    /// Performs Redis ZCARD command to return the number of items in the set.
    /// </summary>
    public Task<long> Count() {
      return Executor.SortedSetLengthAsync(KeyName);
    }

    /// <summary>
    /// Performs Redis ZCOUNT command to return the number of items with scores within the given values.
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <param name="exclude"></param>
    /// <returns></returns>
    public Task<long> CountByScore(double min = Double.NegativeInfinity, double max = Double.PositiveInfinity, Exclude exclude = Exclude.None) {
      return Executor.SortedSetLengthAsync(KeyName, min, max, exclude);
    }

    /// <summary>
    /// Performs Redis ZLEXCOUNT.
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <param name=""></param>
    /// <param name="exclude"></param>
    /// <returns></returns>
    public Task<long> CountByValue(T min = default, T max = default, Exclude exclude = Exclude.None) {
      var rmin = ToRedisValue(min);
      var rmax = ToRedisValue(max);
      return Executor.SortedSetLengthByValueAsync(KeyName, rmin, rmax, exclude);
    }

    /// <summary>
    /// Performs Redist ZRANGE, or ZREVRANGE if descending order is specified.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="stop"></param>
    /// <param name="order"></param>
    /// <returns></returns>
    public Task<IList<T>> Range(long start = 0, long stop = -1, Order order = Order.Ascending) {
      return Executor.SortedSetRangeByRankAsync(KeyName, start, stop, order)
             .ContinueWith<IList<T>>(r => r.Result.Select(v => ToElement<T>(v)).ToList(),
              TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Performs Redis ZRANGE, or ZREVRANGE if descending order is specified, with the WITHSCORES option.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="stop"></param>
    /// <param name="order"></param>
    /// <returns></returns>
    public Task<IList<(T element, double score)>> RangeWithScores(long start = 0, long stop = -1, Order order = Order.Ascending) {
      return Executor.SortedSetRangeByRankWithScoresAsync(KeyName, start, stop, order)
              .ContinueWith<IList<(T element, double score)>>(r => r.Result.Select(e => (ToElement<T>(e.Element), e.Score)).ToList(),
              TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Performs Redis ZRANGEBYLEX, or ZREVRANGEBYLEX if descending order is specified.
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <param name="exclude"></param>
    /// <param name="order"></param>
    /// <param name="skip"></param>
    /// <param name="take"></param>
    /// <returns></returns>
    public Task<IList<T>> RangeByValue(T min = default, T max = default, Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0, long take = -1) {
      var rmin = ToRedisValue(min);
      var rmax = ToRedisValue(max);

      return Executor.SortedSetRangeByValueAsync(KeyName, rmin, rmax, exclude, order, skip, take)
             .ContinueWith<IList<T>>(r => r.Result.Select(v => ToElement<T>(v)).ToList(),
              TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Performs Redis ZRANGEBYSCORE, or ZREVRANGEBYSCORE if descending order is specified.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="stop"></param>
    /// <param name="exclude"></param>
    /// <param name="order"></param>
    /// <param name="skip"></param>
    /// <param name="take"></param>
    /// <returns></returns>
    public Task<IList<T>> RangeByScore(double start = Double.NegativeInfinity, double stop = Double.PositiveInfinity, Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0, long take = -1) {
      return Executor.SortedSetRangeByScoreAsync(KeyName, start, stop, exclude, order, skip, take)
             .ContinueWith<IList<T>>(r => r.Result.Select(v => ToElement<T>(v)).ToList(),
              TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Performs Redis ZRANK, or ZREVRANK if descending order is specified.
    /// </summary>
    /// <param name="element"></param>
    /// <param name="order"></param>
    /// <returns></returns>
    public Task<long?> Rank(T element, Order order = Order.Ascending) {
      return Executor.SortedSetRankAsync(KeyName, ToRedisValue(element), order);
    }

    /// <summary>
    /// Performs Redis ZSCORE command.
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    public Task<double?> Score(T element) {
      return Executor.SortedSetScoreAsync(KeyName, ToRedisValue(element));
    }

    /// <summary>
    /// Performs Redis ZINCRBY command.
    /// </summary>
    /// <param name="element"></param>
    /// <param name="incrValue"></param>
    /// <returns></returns>
    public Task<double> IncrementScore(T element, double incrValue) {
      return Executor.SortedSetIncrementAsync(KeyName, ToRedisValue(element), incrValue);
    }

    /// <summary>
    /// Performs Redis ZREM command.
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    public Task<bool> Remove(T element) {
      return Executor.SortedSetRemoveAsync(KeyName, ToRedisValue(element));
    }

    /// <summary>
    /// Performs Redis ZREM command. 
    /// </summary>
    /// <param name="elements"></param>
    /// <returns></returns>
    public Task<long> RemoveRange(IEnumerable<T> elements) {
      var values = elements.Select(e => (RedisValue)ToRedisValue(e)).ToArray();
      return Executor.SortedSetRemoveAsync(KeyName, values);
    }

    /// <summary>
    /// Performs Reds ZREMRANGEBYLEX.
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <param name="exclude"></param>
    /// <returns></returns>
    public Task<long> RemoveRangeByValue(T min = default, T max = default, Exclude exclude = Exclude.None) {
      var rmin = ToRedisValue(min);
      var rmax = ToRedisValue(max);
      return Executor.SortedSetRemoveRangeByValueAsync(KeyName, rmin, rmax, exclude);
    }

    /// <summary>
    /// Performs Redis ZREMRANGEBYRANK to remove all members of the set within the given indexes.
    /// </summary>
    /// <param name="start">Offset index</param>
    /// <param name="stop">Offset index</param>
    /// <returns></returns>
    public Task<long> RemoveRange(long start = 0, long stop = -1) {
      return Executor.SortedSetRemoveRangeByRankAsync(KeyName, start, stop);
    }

    /// <summary>
    /// Performs Redis ZREMRANGEBYSCORE.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="stop"></param>
    /// <param name="exclude"></param>
    /// <returns></returns>
    public Task<long> RemoveRangeByScore(double start = Double.NegativeInfinity, double stop = Double.PositiveInfinity, Exclude exclude = Exclude.None) {
      return Executor.SortedSetRemoveRangeByScoreAsync(KeyName, start, stop, exclude);
    }

    /// <summary>
    /// Performs Redis ZPOPMIN, or ZPOPMAX if descending order is specified.
    /// </summary>
    /// <returns></returns>
    public Task<(T element, double score)> Pop(Order order = Order.Ascending) {
      return Executor.SortedSetPopAsync(KeyName, order)
        .ContinueWith<(T element, double score)>(r => {
          if (r.Result.HasValue) {
            var element = ToElement<T>(r.Result.Value.Element);
            return (element, r.Result.Value.Score);
          }
          return (default, 0);
        }, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
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
    /// <param name="destination"></param>
    /// <param name="order"></param>
    /// <param name="sortType"></param>
    /// <param name="skip"></param>
    /// <param name="take"></param>
    /// <param name="byKeyNamePattern"></param>
    /// <param name="getKeyNamePattern"></param>
    /// <returns></returns>
    public Task<long> SortAndStore(RedisSortedSet<T> destination, Order order = Order.Ascending, SortType sortType = SortType.Numeric, long skip = 0, long take = -1, string byKeyNamePattern = null, string[] getKeyNamePattern = null) {
      var getKeys = getKeyNamePattern == null ? null : getKeyNamePattern.Select(s => (RedisValue)s).ToArray();
      return Executor.SortAndStoreAsync(destination.KeyName, KeyName, skip, take, order, sortType, byKeyNamePattern, getKeys);
    }

    /// <summary>
    /// Performs Redis ZINTERSTORE command.
    /// </summary>
    /// <param name="destination"></param>
    /// <param name="otherSets"></param>
    /// <returns></returns>
    public Task<long> IntersectStore(RedisSortedSet<T> destination, ICollection<RedisSortedSet<T>> otherSets, double[] weights = null, Aggregate aggregate = Aggregate.Sum) {
      var keys = (new RedisKey[] { (RedisKey)KeyName }).Concat(otherSets.Select(s => (RedisKey)s.KeyName)).ToArray();
      return Executor.SortedSetCombineAndStoreAsync(SetOperation.Intersect, destination.KeyName, keys, weights, aggregate);
    }


    /// <summary>
    /// Performs Redis ZUNIONSTORE command.
    /// </summary>
    /// <param name="destination"></param>
    /// <param name="otherSets"></param>
    /// <returns></returns>
    public Task<long> UnionStore(RedisSortedSet<T> destination, ICollection<RedisSortedSet<T>> otherSets, double[] weights = null, Aggregate aggregate = Aggregate.Sum) {
      if (destination == null) throw new ArgumentException("Destination is required");
      var keys = (new RedisKey[] { (RedisKey)KeyName }).Concat(otherSets.Select(s => (RedisKey)s.KeyName)).ToArray();
      return destination.Executor.SortedSetCombineAndStoreAsync(SetOperation.Union, destination.KeyName, keys, weights, aggregate);
    }

    /// <summary>
    /// Performs Redis ZSCAN to asynchronously enumerate the set.
    /// </summary>
    /// <returns></returns>
    public async IAsyncEnumerator<T> GetAsyncEnumerator() {
      await foreach (var entry in Executor.SortedSetScanAsync(KeyName)) {
        yield return ToElement<T>(entry.Element);
      }
    }

    IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken) {
      return GetAsyncEnumerator();
    }
  }
}
