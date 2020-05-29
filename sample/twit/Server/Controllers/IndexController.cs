using Twit.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Twit.Server.Services;
using Twit.Server.Data;

namespace Twit.Server.Controllers {

  [ApiController]
  [Route("[controller]")]
  public class IndexController : ControllerBase {

    private readonly ILogger<IndexController> logger;
    private readonly CacheService _cacheService;

    public IndexController(ILogger<IndexController> logger, CacheService cacheService) {
      this.logger = logger;
      _cacheService = cacheService;
    }

    [HttpPost("register")]
    public async Task<UserDto> Register(UserDto user) {
      user.Ticket = await _cacheService.RegisterUser(user.UserName, user.Password);
      return user;
    }

    [HttpPost("login")]
    public async Task<UserDto> Login(UserDto user) {
      user.Ticket = await _cacheService.LoginUser(user.UserName, user.Password);
      return user;
    }

    [HttpGet("timeline")]
    public async Task<IList<PostDto>> GetDefaultTimeline() {
      var posts = await _cacheService.GetDefaultTimeline();
      return posts.Select(p => new PostDto { UserName = p.UserName, Posted = p.Posted, Message = p.Message }).ToList();
    }

    [HttpGet("timeline/{user}")]
    public async Task<IList<PostDto>> GetUserTimeline(string user) {
      var posts = await _cacheService.GetTimeline(user);
      return posts.Select(p => new PostDto { UserName = p.UserName, Posted = p.Posted, Message = p.Message }).ToList();
    }

    [HttpGet("profile/{user}")]
    public async Task<IList<PostDto>> GetUserProfile(string user) {
      var posts = await _cacheService.GetTimeline(user, "profile");
      return posts.Select(p => new PostDto { UserName = p.UserName, Posted = p.Posted, Message = p.Message }).ToList();
    }

    [HttpPost("follow")]
    public async Task Follow(FollowDto info) {
      await _cacheService.FollowUser(info.UserName, info.OtherUserName);
    }

    [HttpPost("post")]
    public async Task CreatePost(PostDto post) {
      await _cacheService.CreatePost(post.UserName, post.Message);
    }

    [HttpGet("users")]
    public async Task<IList<UserToFollow>> GetUsers() {
      var users = await _cacheService.UserList();
      return users.Select(u => new UserToFollow { UserName = u, ButtonText = "Follow", IsFollowing = false }).ToList(); ;
    }

  }
}
