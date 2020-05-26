using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedisProvider {

  /// <summary>
  /// Redis key-value pair for byte arrays where bit manipulation is needed.
  /// </summary>
  public class RedisBitmap : RedisItem<byte[]> {
    public RedisBitmap(string keyName) : base(keyName) {
    }

    public new RedisBitmap WithTx(RedisTransactionProxy proxy) {
      return base.WithTx(proxy) as RedisBitmap;
    }

    public new RedisBitmap WithBatch(RedisBatchProxy proxy) {
      return base.WithBatch(proxy) as RedisBitmap;
    }

    /// <summary>
    /// Performs Redis SETBIT command.
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="bit"></param>
    /// <returns></returns>
    public Task<bool> SetBit(long offset, bool bit) {
      return Executor.StringSetBitAsync(KeyName, offset, bit);
    }

    /// <summary>
    /// Performs Redis GETBIT command.
    /// </summary>
    /// <param name="offset"></param>
    /// <returns></returns>
    public Task<bool> GetBit(long offset) {
      return Executor.StringGetBitAsync(KeyName, offset);
    }

    /// <summary>
    /// Performs Redis BITCOUNT command.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public Task<long> BitCount(long start = 0, long end = -1) {
      return Executor.StringBitCountAsync(KeyName, start, end);
    }

    /// <summary>
    /// Performs Redis BITPOS command.
    /// </summary>
    /// <param name="bit"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public Task<long> BitPosition(bool bit, long start = 0, long end = -1) {
      return Executor.StringBitPositionAsync(KeyName, bit, start, end);
    }

    /// <summary>
    /// Performs Redis BITOP command.
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="destination"></param>
    /// <param name="bitmaps"></param>
    /// <returns></returns>
    public static Task<long> BitwiseOp(Bitwise operation, RedisBitmap destination, ICollection<RedisBitmap> bitmaps) {
      if (destination == null) throw new ArgumentException("Destination is required");
      var destKey = destination.KeyName;
      var keys = bitmaps.Select(b => (RedisKey)b.KeyName).ToArray();
      return destination.Executor.StringBitOperationAsync(operation, destKey, keys);
    }

  }
}
