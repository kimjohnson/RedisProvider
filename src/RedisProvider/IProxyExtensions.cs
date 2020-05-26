using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedisProvider {

  public static class IProxyExtensions {

    /// <summary>
    /// Returns whether the client can communicate with the server.
    /// </summary>
    /// <param name="keyName"></param>
    /// <returns></returns>
    public static bool IsConnected(this IProxy proxy, string keyName) {
      if (proxy == null) throw new ArgumentNullException("proxy");
      return proxy.DB.IsConnected(keyName);
    }

    /// <summary>
    /// Performs Redis EXISTS command for a single key name.
    /// </summary>
    /// <param name="keyName"></param>
    /// <param name="useKeyNameSpace"></param>
    /// <returns></returns>
    public static Task<bool> KeyExists(this IProxy proxy, string keyName, bool useKeyNameSpace = true) {
      if (proxy == null) throw new ArgumentNullException("proxy");
      var fullKeyName = useKeyNameSpace ? $"{proxy.KeyNameSpace}:{keyName}" : keyName;
      return proxy.DB.KeyExistsAsync(fullKeyName);
    }

    /// <summary>
    /// Performs Redis EXISTS command with multiple key names.
    /// </summary>
    /// <param name="keyNames"></param>
    /// <param name="useKeyNameSpace"></param>
    /// <returns></returns>
    public static Task<long> KeyExists(this IProxy proxy, IEnumerable<string> keyNames, bool useKeyNameSpace = true) {
      if (proxy == null) throw new ArgumentNullException("proxy");
      var keys = keyNames.Select(k => useKeyNameSpace ? $"{proxy.KeyNameSpace}:{k}" : k).Cast<RedisKey>().ToArray();
      return proxy.DB.KeyExistsAsync(keys);
    }

    /// <summary>
    /// Performs Redis DEL command.
    /// </summary>
    /// <param name="keyName"></param>
    /// <param name="useKeyNameSpace"></param>
    /// <returns></returns>
    public static Task<bool> DeleteKey(this IProxy proxy, string keyName, bool useKeyNameSpace = true) {
      if (proxy == null) throw new ArgumentNullException("proxy");
      var fullKeyName = useKeyNameSpace ? $"{proxy.KeyNameSpace}:{keyName}" : keyName;
      return proxy.DB.KeyDeleteAsync(fullKeyName);
    }

    /// <summary>
    /// Performs Redis DEL command with multiple key names.
    /// </summary>
    /// <param name="keyNames"></param>
    /// <param name="useKeyNameSpace"></param>
    /// <returns></returns>
    public static Task<long> DeleteKey(this IProxy proxy, IEnumerable<string> keyNames, bool useKeyNameSpace = true) {
      if (proxy == null) throw new ArgumentNullException("proxy");
      var keys = keyNames.Select(k => useKeyNameSpace ? $"{proxy.KeyNameSpace}:{k}" : k).Cast<RedisKey>().ToArray();
      return proxy.DB.KeyDeleteAsync(keys);
    }

  }
}