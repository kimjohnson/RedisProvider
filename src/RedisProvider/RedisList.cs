using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisProvider {
  /// <summary>
  /// A RedisList holding RedisValue elements.
  /// </summary>
  public class RedisValueList : RedisList<RedisValue> {
    public RedisValueList(string keyName) : base(keyName) {}
  }

  /// <summary>
  /// Encapsulate commands for the Redis LIST data type.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class RedisList<T> : RedisObject, IAsyncEnumerable<T> {

    public RedisList(string keyName) : base(keyName) { }

    public new RedisList<T> WithTx(RedisTransactionProxy proxy) {
      return base.WithTx(proxy) as RedisList<T>;
    }

    public new RedisList<T> WithBatch(RedisBatchProxy proxy) {
      return base.WithBatch(proxy) as RedisList<T>;
    }

    /// <summary>
    /// Performs Redis LLEN command.
    /// </summary>
    /// <returns></returns>
    public Task<long> Count() {
      return Executor.ListLengthAsync(KeyName);
    }

    /// <summary>
    /// Returns the first item in the list.
    /// </summary>
    /// <returns></returns>
    public async Task<T> First() {
      return await Index(0);
    }

    /// <summary>
    /// Returns the last item in the list.
    /// </summary>
    /// <returns></returns>
    public async Task<T> Last() {
      return await Index(-1);
    }

    /// <summary>
    /// Performs Redis LINDEX command.
    /// </summary>
    /// <param name="ix"></param>
    /// <returns></returns>
    public Task<T> Index(long ix) {
      return Executor.ListGetByIndexAsync(KeyName, ix)
            .ContinueWith<T>(i => ToElement<T>(i.Result), TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Performs Redis LINSERT command with 'before' option.
    /// </summary>
    /// <param name="pivot"></param>
    /// <param name="toAdd"></param>
    /// <returns></returns>
    public Task<long> AddBefore(T pivot, T toAdd) {
      return Executor.ListInsertBeforeAsync(KeyName, ToRedisValue(pivot), ToRedisValue(toAdd));
    }

    /// <summary>
    /// Performs Redis LINSERT command with 'after' option.
    /// </summary>
    /// <param name="pivot"></param>
    /// <param name="toAdd"></param>
    /// <returns></returns>
    public Task<long> AddAfter(T pivot, T toAdd) {
      return Executor.ListInsertAfterAsync(KeyName, ToRedisValue(pivot), ToRedisValue(toAdd));
    }

    /// <summary>
    /// Performs Redis LPUSH command.
    /// </summary>
    /// <param name="elements"></param>
    /// <returns></returns>
    public Task<long> AddFirst(params T[] elements) {
      var values = elements.Select(e => ToRedisValue(e)).ToArray();
      return Executor.ListLeftPushAsync(KeyName, values);
    }

    /// <summary>
    /// Performs Redis RPUSH command.
    /// </summary>
    /// <param name="elements"></param>
    /// <returns></returns>
    public Task<long> AddLast(params T[] elements) {
      var values = elements.Select(e => ToRedisValue(e)).ToArray();
      return Executor.ListRightPushAsync(KeyName, values);
    }


    /// <summary>
    /// Performs Redis LPOP command.
    /// </summary>
    /// <returns></returns>
    public Task<T> RemoveFirst() {
      return Executor.ListLeftPopAsync(KeyName)
             .ContinueWith<T>(r => ToElement<T>(r.Result), TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Performs Redis RPOP command.
    /// </summary>
    /// <returns></returns>
    public Task<T> RemoveLast() {
      return Executor.ListRightPopAsync(KeyName)
            .ContinueWith<T>(r => ToElement<T>(r.Result), TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Performs Redis RPOPLPUSH command. Removes last element from this list and inserts it to the head of the target list.
    /// </summary>
    /// <param name="targetKey"></param>
    public Task<T> PopPush(RedisList<T> targetList) {
      var targetKey = targetList.KeyName;
      return PopPush(targetKey);
    }

    /// <summary>
    /// Performs Redis RPOPLPUSH command. Removes last element from this list and inserts it to the head of the target list.
    /// </summary>
    /// <param name="targetListKeyName">Fully-qualified key name of target list.</param>
    /// <returns></returns>
    public Task<T> PopPush(string targetListKeyName) {
      return Executor.ListRightPopLeftPushAsync(KeyName, targetListKeyName)
             .ContinueWith<T>(r => ToElement<T>(r.Result), TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Performs Redis LRANGE command
    /// </summary>
    /// <param name="start">Offset index</param>
    /// <param name="end">Offset index</param>
    /// <returns></returns>
    public Task<IList<T>> Range(long start, long end) {
      return Executor.ListRangeAsync(KeyName, start, end)
         .ContinueWith<IList<T>>(r => r.Result.Select(v => ToElement<T>(v)).ToList()
         , TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }


    /// <summary>
    /// Performs Redis LREM command. 
    /// </summary>
    /// <param name="element"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public Task<long> Remove(T element, long count = 1) {
      return Executor.ListRemoveAsync(KeyName, ToRedisValue(element), count);
    }

    /// <summary>
    /// Performs Redis LSET command.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="element"></param>
    public Task Set(long index, T element) {
      var v = ToRedisValue(element);
      return Executor.ListSetByIndexAsync(KeyName, index, v);
    }

    /// <summary>
    /// Performs Redis LTRIM command.
    /// </summary>
    /// <param name="start">Offset index</param>
    /// <param name="stop">Offset index</param>
    public Task Trim(int start, int stop) {
      return Executor.ListTrimAsync(KeyName, start, stop);
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
    public Task<IList<T>> Sort(Order order = Order.Ascending, SortType sortType = SortType.Numeric, int skip = 0, int take = -1, string byKeyNamePattern = null, string[] getKeyNamePattern = null) {
      var getKeys = getKeyNamePattern == null ? null : getKeyNamePattern.Select(s => (RedisValue)s).ToArray();
      return Executor.SortAsync(KeyName, skip, take, order, sortType, byKeyNamePattern, getKeys)
             .ContinueWith<IList<T>>(r => r.Result.Select(v => ToElement<T>(v)).ToList()
             , TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Performs Redis SORT command with STORE option. 
    /// </summary>
    /// <param name="destinationList"></param>
    /// <param name="order"></param>
    /// <param name="sortType"></param>
    /// <param name="skip"></param>
    /// <param name="take"></param>
    /// <param name="byKeyNamePattern"></param>
    /// <param name="getKeyNamePattern"></param>
    /// <returns></returns>
    public Task<long> SortAndStore(RedisList<T> destinationList, Order order = Order.Ascending, SortType sortType = SortType.Numeric, int skip = 0, int take = -1, string byKeyNamePattern = null, string[] getKeyNamePattern = null) {
      var getKeys = getKeyNamePattern == null ? null : getKeyNamePattern.Select(s => (RedisValue)s).ToArray();
      return Executor.SortAndStoreAsync(destinationList.KeyName, KeyName, skip, take, order, sortType, byKeyNamePattern, getKeys);
    }

    /// <summary>
    /// Enumerate the list as an asynchronous stream.
    /// </summary>
    /// <returns></returns>
    public async IAsyncEnumerator<T> GetAsyncEnumerator() {
      var count = await Count();
      for (long i = 0; i < count; i++) {
        var element = await Index(i);
        yield return element;
      }
    }

    IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken) {
      return GetAsyncEnumerator();
    }
  }
}
