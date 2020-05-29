using System;

namespace Twit.Server.Data {

  public class Post {
    public long Id { get; set; }
    public long Uid { get; set; }
    public string UserName { get; set; }
    public DateTime Posted { get; set; }
    public string Message { get; set; }
  }

}
