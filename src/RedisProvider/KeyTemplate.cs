using System;
using System.Collections.Generic;
using System.Text;

namespace RedisProvider {

  /// <summary>
  /// Can be used to easily create strongly-typed RedisObjects with a specified key name pattern.   
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class KeyTemplate<T> where T : RedisObject {

    private readonly RedisContainer _container;
    private readonly string _keyPattern;

    internal KeyTemplate(RedisContainer container, string keyPattern) {
      _container = container;
      _keyPattern = keyPattern;
    }

    public T GetKey(object arg1) {
      var s = string.Format(_keyPattern, arg1);
      return _container.GetKey<T>(s);
    }

    public T GetKey(params object[] args) {
      var s = string.Format(_keyPattern, args);
      return _container.GetKey<T>(s);
    }

  }
}
