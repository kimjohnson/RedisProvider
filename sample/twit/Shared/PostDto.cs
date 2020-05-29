using System;
using System.Collections.Generic;
using System.Text;

namespace Twit.Shared {
  public class PostDto {
    public string UserName { get; set; }
    public string Message { get; set; }
    public DateTime? Posted { get; set; }
  }
}
