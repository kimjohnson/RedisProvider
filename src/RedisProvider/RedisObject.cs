using StackExchange.Redis;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace RedisProvider {

  /// <summary>
  /// The RedisObject is the base type for any strongly-typed Redis key.
  /// </summary>
  public abstract class RedisObject {

    private string _keyName;
    private RedisContainer _container;
    private IProxy RedisMulti;

    public RedisObject(string keyName) => BaseKeyName = keyName;

    public RedisContainer Container {
      get {
        if (_container == null) throw new InvalidOperationException("Must add this object to a RedisContainer first.");
        return _container;
      }
      internal set {
        _container = value;
      }
    }

    /// <summary>
    /// The database, transaction or batch in use with this RedisObject.
    /// </summary>
    public IDatabaseAsync Executor {
      get {
        if (RedisMulti != null) return RedisMulti.DB;
        return Container.Database;
      }
    }

    /// <summary>
    /// Use the supplied transaction when executing tasks with this RedisObject.
    /// </summary>
    /// <param name="proxy"></param>
    /// <returns></returns>
    public virtual RedisObject WithTx(RedisTransactionProxy proxy) {
      var copy = this.MemberwiseClone() as RedisObject;
      copy.RedisMulti = proxy;
      return copy;
    }

    /// <summary>
    /// Use the supplied batch when executing tasks with this RedisObject.
    /// </summary>
    /// <param name="proxy"></param>
    /// <returns></returns>
    public virtual RedisObject WithBatch(RedisBatchProxy proxy) {
      var copy = this.MemberwiseClone() as RedisObject;
      copy.RedisMulti = proxy;
      return copy;
    }

    /// <summary>
    /// The keyName provided in the constructor, without the KeyNamespace.
    /// </summary>
    public string BaseKeyName { get; }

    /// <summary>
    /// The KeyName is the KeyNamespace (if specified) plus the keyName provided in the constructor.
    /// </summary>
    public string KeyName {
      get => _keyName ?? BaseKeyName;
      internal set => _keyName = value;
    }

    /// <summary>
    /// Performs Redis TTL command.
    /// </summary>
    /// <returns></returns>
    public Task<long> TimeToLive() {
      return Executor.KeyTimeToLiveAsync(KeyName)
             .ContinueWith<long>(r => r.Result.HasValue ? r.Result.Value.Seconds : -1, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Performs Redis EXPIRE command.
    /// </summary>
    /// <param name="seconds"></param>
    /// <returns></returns>
    public async Task<bool> Expire(int seconds) {
      var ts = TimeSpan.FromSeconds(seconds);
      return await Executor.KeyExpireAsync(KeyName, ts);
    }

    /// <summary>
    /// Performs Redis EXPIREAT command.
    /// </summary>
    /// <param name="seconds"></param>
    /// <returns></returns>
    public Task<bool> ExpireAt(DateTime dt) {
      return Executor.KeyExpireAsync(KeyName, dt);
    }

    /// <summary>
    /// Performs the Redis OBJECT IDLETIME command.
    /// </summary>
    /// <returns></returns>
    public Task<long> IdleTime() {
      return Executor.KeyIdleTimeAsync(KeyName)
             .ContinueWith<long>(r => r.Result.HasValue ? r.Result.Value.Seconds : -1, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>
    /// Performs Redis PERSIST command.
    /// </summary>
    public Task<bool> Persist() {
      return Executor.KeyPersistAsync(KeyName);
    }

    /// <summary>
    /// Performs Redis EXISTS command.
    /// </summary>
    /// <returns></returns>
    public Task<bool> KeyExists() {
      return Executor.KeyExistsAsync(KeyName);
    }

    /// <summary>
    /// Performs Redis DEL command.
    /// </summary>
    /// <returns></returns>
    public Task<bool> DeleteKey() {
      return Executor.KeyDeleteAsync(KeyName);
    }

    public static RedisValue ToRedisValue(object element) {
      if (element == null) return RedisValue.Null;
      if (element is byte[] b) return b;
      if (element is RedisValue x) return x;
      if (element is IConvertible _) return ConvertToRedisValue(element);
      return JsonSerializer.Serialize(element);
    }

    public static T ToElement<T>(RedisValue value) {
      if (value.HasValue == false) return default;
      if (typeof(byte[]) == typeof(T)) return (T)Convert.ChangeType(value, typeof(T));
      if (typeof(RedisValue) == typeof(T)) return (T)Convert.ChangeType(value, typeof(T));
      if (typeof(IConvertible).IsAssignableFrom(typeof(T))) return (T)ConvertFromRedisValue(typeof(T), value);
      return JsonSerializer.Deserialize<T>(value);
    }

    public static object ToElement(Type type, RedisValue value) {
      if (value.HasValue == false) return default;
      if (typeof(byte[]) == type) return Convert.ChangeType(value, type);
      if (typeof(RedisValue) == type) return Convert.ChangeType(value, type);
      if (typeof(IConvertible).IsAssignableFrom(type)) return ConvertFromRedisValue(type, value);
      return JsonSerializer.Deserialize(value, type);
    }

    private static object ConvertFromRedisValue(Type t, RedisValue value) {
      switch (Type.GetTypeCode(t)) {
        case TypeCode.Boolean: return bool.Parse(value);
        case TypeCode.Char: return char.Parse(value);
        case TypeCode.DateTime: return new DateTime(long.Parse(value));
        case TypeCode.Decimal: return decimal.Parse(value);
        case TypeCode.Double: return double.Parse(value);
        case TypeCode.Int16: return short.Parse(value);
        case TypeCode.Int32: return int.Parse(value);
        case TypeCode.Int64: return long.Parse(value);
        case TypeCode.SByte: return sbyte.Parse(value);
        case TypeCode.Single: return float.Parse(value);
        case TypeCode.String: return value.ToString();
        case TypeCode.UInt16: return ushort.Parse(value);
        case TypeCode.UInt32: return uint.Parse(value);
        case TypeCode.UInt64: return ulong.Parse(value);
        default:
          throw new Exception("Unsupported type");
      }
    }

    private static RedisValue ConvertToRedisValue(object value) {
      switch (value) {
        case RedisValue b: return b;
        case bool b: return b;
        case char b: return b.ToString();
        case DateTime b: return b.Ticks;
        case decimal b: return (double)b;
        case double b: return b;
        case Int16 b: return b;
        case Int32 b: return b;
        case Int64 b: return b;
        case sbyte b: return b;
        case Single b: return b;
        case string b: return b;
        case UInt16 b: return (uint)b;
        case UInt32 b: return b;
        case UInt64 b: return b;
        default:
          throw new Exception("Unsupported type");
      }
    }
  }
}
