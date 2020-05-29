using System;

namespace Twit.Server.Data {

  public class User {

    public long Id { get; set; }
    public string  UserName { get; set; }
    public int Followers { get; set; }
    public int Following { get; set; }
    public int Posts { get; set; }
    public DateTime Signup { get; set; }
    public string Password { get; set; }
    public string Ticket { get; set; }

  }
}
