using Twit.Server.Data;
using RedisProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Twit.Server.Services {

  // Loosely based on https://redislabs.com/ebook/part-2-core-concepts/chapter-8-building-a-simple-social-network/
  // and https://redis.io/topics/twitter-clone


  public class CacheService  {

    public const int Home_Timeline_Max = 50;

    private readonly RedisContainer _container;
    private RedisItem<long> NextUserId;
    private RedisItem<long> NextPostId;
    private RedisHash<string, long> Users; 
    private RedisHash<string, long> Auths; 
    private RedisList<Post> Timeline;

    private KeyTemplate<RedisDtoHash<User>> UserTemplate;
    private KeyTemplate<RedisDtoHash<Post>> PostTemplate;
    private KeyTemplate<RedisSortedSet<long>> UserProfileTemplate;
    private KeyTemplate<RedisSortedSet<long>> UserFollowersTemplate;
    private KeyTemplate<RedisSortedSet<long>> UserFollowingTemplate;
    private KeyTemplate<RedisSortedSet<long>> UserHomeTLTemplate;

    public CacheService(RedisConnection cn, string keyNameSpace = "") {

      _container = new RedisContainer(cn, keyNameSpace);
      NextUserId = _container.GetKey<RedisItem<long>>("nextUserId");
      NextPostId = _container.GetKey<RedisItem<long>>("nextPostId");

      Users = _container.GetKey<RedisHash<string, long>>("users");
      Auths = _container.GetKey<RedisHash<string, long>>("auths");

      Timeline = _container.GetKey<RedisList<Post>>("timeline");

      UserTemplate = _container.GetKeyTemplate<RedisDtoHash<User>>("user:{0}");
      PostTemplate = _container.GetKeyTemplate<RedisDtoHash<Post>>("post:{0}");
      UserProfileTemplate = _container.GetKeyTemplate<RedisSortedSet<long>>("profile:{0}");
      UserFollowersTemplate = _container.GetKeyTemplate<RedisSortedSet<long>>("followers:{0}");
      UserFollowingTemplate = _container.GetKeyTemplate<RedisSortedSet<long>>("following:{0}");
      UserHomeTLTemplate = _container.GetKeyTemplate<RedisSortedSet<long>>("home:{0}");
    }

    public async Task<string> RegisterUser(string name, string pwd) {

      if ((await Users.ContainsKey(name))) throw new Exception("User name already exists");

      var id = await NextUserId.Increment();
      var userKey = UserTemplate.GetKey(id);
      var ticket = Guid.NewGuid().ToString();
      var data = new User { Id = id, UserName = name, Signup = DateTime.Now, Password = pwd, Ticket = ticket };

      var tx = _container.CreateTransaction();
      var t1 = userKey.WithTx(tx).FromDto(data);
      var t2 = Users.WithTx(tx).Set(name, id);
      var t3 = Auths.WithTx(tx).Set(ticket, id);
      await tx.Execute();
      return ticket;
    }

    public async Task<string> LoginUser(string name, string pwd) {
      string ticket = null;

      var userId = await Users.Get(name);
      var userKey = UserTemplate.GetKey(userId);
      var data = await userKey.ToDto();
      if (data.Password == pwd) ticket = data.Ticket;
      return ticket;
    }

    public async Task CreatePost(string userName, string message) {

      var tx = _container.CreateTransaction();

      var t1 = Users.WithTx(tx).Get(userName);
      var t2 = NextPostId.WithTx(tx).Increment();
      await tx.Execute();
      var userid = t1.Result;
      var id = t2.Result;

      // Create the post
      var postKey = PostTemplate.GetKey(id);
      var data = new Post { Id = id, Uid = userid, UserName = userName, Posted = DateTime.Now, Message = message };
      postKey.WithTx(tx).FromDto(data);

      // increment our post count
      var thisuser = UserTemplate.GetKey(userid); 
      thisuser.WithTx(tx).Increment("posts");
      await tx.Execute();

      // Add the post to our profile tl
      var tl = UserProfileTemplate.GetKey(userid);
      await tl.Add(data.Id, data.Posted.Ticks);

      // Add the post to our home tl
      var tlhome = UserHomeTLTemplate.GetKey(userid);
      await tlhome.Add(data.Id, data.Posted.Ticks);

      // Tell our followers - assume we have very few - haha
      var followers = UserFollowersTemplate.GetKey(userid);
      await foreach (var fid in followers) {
        var ftl = UserHomeTLTemplate.GetKey(fid);
        await ftl.Add(data.Id, data.Posted.Ticks);
        await ftl.RemoveRange(stop: (Home_Timeline_Max - 1) * -1);
      }

      // add to global timeline and trim
      await Timeline.AddFirst(data);
      await Timeline.Trim(0, Home_Timeline_Max);
    }

    public async Task<IList<Post>> GetDefaultTimeline() {
      return await Timeline.Range(0, -1);
    }

    public async Task<IList<Post>> GetTimeline(string userName, string timeline = "home") {

      var userid = await Users.Get(userName);
      var tl = (timeline == "home") ? UserHomeTLTemplate.GetKey(userid) : UserProfileTemplate.GetKey(userid);
      var postIds = await tl.Range(order: StackExchange.Redis.Order.Descending);
      var posts = new List<Post>();

      foreach (var id in postIds) {
        var data = await PostTemplate.GetKey(id).ToDto();
        posts.Add(data);
      }

      return posts;
    }

    public async Task<IList<string>> UserList() {
      return (await Users.Keys()).ToList();
    }

    public async Task FollowUser(string userName, string otherUserName) {

      var userid = await Users.Get(userName);
      var otherUserId = await Users.Get(otherUserName);
      var following = UserFollowingTemplate.GetKey(userid);
      var followers = UserFollowersTemplate.GetKey(otherUserId);

      // if already following just ignore
      if ((await following.Score(otherUserId)) > 0) return;

      var dt = DateTime.Now.Ticks;
      var tx = _container.CreateTransaction();

      following.WithTx(tx).Add(otherUserId, dt);
      followers.WithTx(tx).Add(userid, dt);

      var user = UserTemplate.GetKey(userid);
      user.WithTx(tx).Increment("following");

      var otherUser = UserTemplate.GetKey(otherUserId);
      otherUser.WithTx(tx).Increment("followers");

      // Add other user's posts to our timeline
      var tl = UserHomeTLTemplate.GetKey(userid);
      var set1 = UserProfileTemplate.GetKey(otherUserId);
      var otherPostsT = set1.WithTx(tx).RangeWithScores(order: StackExchange.Redis.Order.Descending);

      await tx.Execute();
      var otherPosts = otherPostsT.Result;

      tl.WithTx(tx).AddRange(otherPosts);
      tl.WithTx(tx).RemoveRange(0, Home_Timeline_Max * -1);
      await tx.Execute();
    }

    public async Task UnfollowUser(string userName, string otherUserName) {

      var userid = await Users.Get(userName);
      var otherUserId = await Users.Get(otherUserName);
      var following = UserFollowingTemplate.GetKey(userid);
      var followers = UserFollowersTemplate.GetKey(otherUserId);

      // if not following just ignore
      if ((await following.Score(otherUserId)) == 0) return;

      var tx = _container.CreateTransaction();

      following.WithTx(tx).Remove(otherUserId);
      followers.WithTx(tx).Remove(userid);

      var user = UserTemplate.GetKey(userid);
      user.WithTx(tx).Decrement("following");

      var otherUser = UserTemplate.GetKey(otherUserId);
      otherUser.WithTx(tx).Decrement("followers");

      var tl = UserHomeTLTemplate.GetKey(userid);
      var set1 = UserProfileTemplate.GetKey(otherUserId);
      var otherPostsT = set1.WithTx(tx).Range(order: StackExchange.Redis.Order.Descending);
      await tx.Execute();
      await tl.RemoveRange(otherPostsT.Result);
    }
  }
}
