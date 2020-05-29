using Twit.Server.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RedisProvider;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Twit.Tests {
  [TestClass]
  public class Seed {

    public static RedisConnection _redisConnection;
    public static CacheService _cache;

    [ClassInitialize]
    public static void ClassInit(TestContext context) {
      _redisConnection = new RedisConnection("127.0.0.1:6379,abortConnect=false", null);
    }

    [TestInitialize]
    public void Init() {
      _cache = new CacheService(_redisConnection, "twit");
    }

    [TestMethod]
    public async Task SeedDb() {
      var users = new List<string> { "kim", "patty", "hootie", "rocky", "echo" };
      var messages = new List<string> { "it's hot today", "that was a stupid thing to say!", "lockdown day 100", "i'm hungry", 
        "i'm thirsty", "that's new", "it's 5:00 somewhere", "that's nice", "that's not nice", "im really tired" };

      foreach (var name in users) {
        await _cache.RegisterUser(name, name);
        await _cache.LoginUser(name, name);
      }
      var r = new Random();

      for (int i = 0; i < 4; i++) {
        foreach (var name in users) {
          var x = r.Next(0, messages.Count);
          await _cache.CreatePost(name, messages[x]);
        }
      }
    }

  }
}
