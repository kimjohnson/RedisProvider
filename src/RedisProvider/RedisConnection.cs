using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;

namespace RedisProvider {

  public class RedisConnection {

    private static Lazy<ConnectionMultiplexer> _connection;
    private readonly ILogger _logger;

    /// <summary>
    /// </summary>
    /// <param name="configuration">A StackExchange.Redis configuration string</param>
    /// <param name="logger"></param>
    public RedisConnection(string configuration, ILogger logger = null) {
      _logger = logger;
      _connection = new Lazy<ConnectionMultiplexer>(() => {
        var mp = ConnectionMultiplexer.Connect(configuration);
        mp.ConnectionFailed += Mp_ConnectionFailed;
        mp.ConnectionRestored += Mp_ConnectionRestored;
        if (!mp.IsConnected) {
          _logger?.LogWarning("Cannot connect to Redis.  Will retry.");
        }
        return mp;
      });
    }

    private void Mp_ConnectionRestored(object sender, ConnectionFailedEventArgs e) {
      _logger?.LogInformation($"Redis connection restored at {DateTime.Now}");
    }

    private void Mp_ConnectionFailed(object sender, ConnectionFailedEventArgs e) {
      _logger?.LogWarning($"Redis connection failed:  {e.Exception.Message}");
    }

    public ConnectionMultiplexer Connection() {
      return _connection.Value;
    }
  }
}
