using StackExchange.Redis;

namespace RedisProvider {

  /// <summary>
  /// Represents an object holding a database, transaction or batch.
  /// </summary>
    public interface IProxy {

      string KeyNameSpace { get; }
      IDatabaseAsync DB { get;  }
    }
}