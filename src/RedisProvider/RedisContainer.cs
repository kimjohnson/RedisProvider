using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedisProvider {

  /// <summary>
  /// A container for sub-classed RedisObject's representing keys in a Redis database.
  /// The container does not itself serve as a cache - the only state it holds is the
  /// various RedisObjects it tracks. 
  /// </summary>
  public class RedisContainer : IProxy {
    private readonly RedisConnection _connection;
    private readonly bool _trackObjects;
    private Dictionary<string, RedisObject> _trackedObjects = new Dictionary<string, RedisObject>();

    public RedisContainer(RedisConnection cn, string keyNameSpace = "", bool trackObjects = true) {
      _connection = cn;
      Database = cn.Connection().GetDatabase();
      KeyNameSpace = keyNameSpace ?? "";
      _trackObjects = trackObjects;
    }

    public IDatabase Database { get; private set; }

    IDatabaseAsync IProxy.DB => Database;

    public string KeyNameSpace { get; }

    /// <summary>
    /// Add a strongly-typed RedisObject to the container.  
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    public T AddToContainer<T>(T obj) where T : RedisObject {
      obj.Container = this;
      obj.KeyName = string.IsNullOrWhiteSpace(KeyNameSpace) ? $"{obj.BaseKeyName}" : $"{KeyNameSpace}:{obj.BaseKeyName}";

      if (_trackObjects) {
        if (!TrackedKeys.Contains(obj.KeyName))
          _trackedObjects.Add(obj.KeyName, obj);
      }
      return obj;
    }

    /// <summary>
    /// Returns a strongly-typed RedisObject for the key name.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="keyName"></param>
    /// <returns></returns>
    /// <remarks>
    /// <para>This will not create the key in Redis, but the key might already exist in Redis independent of this object.</para>
    /// <para>If the container was created with the 'TrackObjects' option (the default)
    /// and the RedisObject was already created then that object is returned.  Otherwise a new instance is created.</para>
    /// </remarks>
    public T GetKey<T>(string keyName) where T : RedisObject {
      return GetKey(typeof(T), keyName) as T;
    }

    /// <summary>
    /// Returns a strongly-typed RedisObject for the key name.
    /// </summary>
    /// <param name="keyType"></param>
    /// <param name="keyName"></param>
    /// <returns></returns>
    /// <remarks>
    /// <para>This will not create the key in Redis, but the key might already exist in Redis independent of this object.</para>
    /// <para>If the container was created with the 'TrackObjects' option (the default)
    /// and the RedisObject was already created then that object is returned.  Otherwise a new instance is created.</para>
    /// </remarks>
    public RedisObject GetKey(Type keyType, string keyName) {

      RedisObject obj;
      var fullKeyName = $"{KeyNameSpace}:{keyName}";
      if (_trackObjects) {
        if (_trackedObjects.TryGetValue(fullKeyName, out obj)) return obj;
      }

      var instance = Activator.CreateInstance(keyType, keyName) as RedisObject;
      this.AddToContainer(instance);
      return instance;
    }

    /// <summary>
    /// Returns a KeyTemplate for the specified strongly-typed RedisObject and key pattern.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="keyNamePattern"></param>
    /// <returns></returns>
    public KeyTemplate<T> GetKeyTemplate<T>(string keyNamePattern) where T : RedisObject {
      return new KeyTemplate<T>(this, keyNamePattern);
    }

    /// <summary>
    /// Returns a RedisTransactionProxy for the Redis transaction.
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    public RedisTransactionProxy CreateTransaction(object state = null) {
      return new RedisTransactionProxy(Database.CreateTransaction(state), KeyNameSpace);
    }

    /// <summary>
    /// Returns a RedisBatchProxy for the Redis batch.  
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    public RedisBatchProxy CreateBatch(object state = null) {
      return new RedisBatchProxy(Database.CreateBatch(state), KeyNameSpace);
    }

    /// <summary>
    /// Delete all keys from Redis which are tracked by this container.
    /// </summary>
    /// <returns></returns>
    public async Task DeleteTrackedKeys() {
      var keys = TrackedKeys.Select(k => (RedisKey)k).ToArray();
      await Database.KeyDeleteAsync(keys);
      _trackedObjects.Clear();
    }

    /// <summary>
    /// Returns the list of key names tracked within this container.
    /// </summary>
    public IList<string> TrackedKeys {
      get { return _trackedObjects.Select(p => p.Key).ToList(); }
    }

    /// <summary>
    /// Returns the list of RedisObjects tracked within this container.
    /// </summary>
    public IList<RedisObject> TrackedObjects {
      get { return _trackedObjects.Select(p => p.Value).ToList(); }
    }

  }

}

