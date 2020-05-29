using Twit.Shared;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Twit.Client.Services {
  public class AppState {

    private readonly HttpClient http;

    public AppState(HttpClient httpInstance) {
      http = httpInstance;
    }

    public event Action OnChange;

    public bool IsLoggedIn { get; private set; }
    public UserDto User { get; private set; }

    public async Task Register(UserDto user) {
      User = await http.PostJsonAsync<UserDto>("/index/register", user);
      IsLoggedIn = (string.IsNullOrEmpty(User?.Ticket) == false);
      NotifyStateChanged();
    }

    public async Task Login(UserDto user) {
      User = await http.PostJsonAsync<UserDto>("/index/login", user);
      IsLoggedIn = (string.IsNullOrEmpty(User?.Ticket) == false);
      NotifyStateChanged();
    }

    public void Logout() {
      User = null;
      IsLoggedIn = false;
      NotifyStateChanged();
    }

    public async Task<IList<PostDto>> GetTimeline(string location) {
      string url;
      if (string.IsNullOrEmpty(location))
        url = "/index/timeline";
      else if (location == "home")
        url = $"/index/timeline/{this.User.UserName}";
      else
        url = $"/index/profile/{location}";

      var posts = await http.GetJsonAsync<IList<PostDto>>(url);
      return posts;
    }

    public async Task CreatePost(string message) {
      var post = new PostDto { Message = message, UserName = this.User.UserName };
      await http.PostJsonAsync("/index/post", post);
    }

    public async Task Follow(string otherUser) {
      var info = new FollowDto { UserName = this.User.UserName, OtherUserName = otherUser };
      await http.PostJsonAsync("/index/follow", info);
    }

    public async Task<IList<UserToFollow>> GetUsers() {
      var users = await http.GetJsonAsync<IList<UserToFollow>>("index/users");
      return users;
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
  }
}
