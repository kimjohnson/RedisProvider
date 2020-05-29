using Microsoft.AspNetCore.Blazor.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Twit.Client.Services;

namespace Twit.Client {

  public class Program {
    public static async Task Main(string[] args) {
      var builder = WebAssemblyHostBuilder.CreateDefault(args);

      builder.Services.AddSingleton<AppState>();

      builder.RootComponents.Add<App>("app");

      await builder.Build().RunAsync();
    }
  }
}
