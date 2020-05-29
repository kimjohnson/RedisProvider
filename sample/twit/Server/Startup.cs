using Twit.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RedisProvider;
using System.Linq;

namespace Twit.Server {
  public class Startup {
    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services) {

      var cn = new RedisConnection("127.0.0.1:6379,abortConnect=false", null);
      var cache = new CacheService(cn, "twit");
      services.AddSingleton<CacheService>(cache);
      services.AddMvc();
      services.AddResponseCompression(opts => {
        opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
            new[] { "application/octet-stream" });
      });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
      app.UseResponseCompression();

      if (env.IsDevelopment()) {
        app.UseDeveloperExceptionPage();
        app.UseBlazorDebugging();
      }

      app.UseStaticFiles();
      app.UseClientSideBlazorFiles<Client.Program>();

      app.UseRouting();

      app.UseEndpoints(endpoints => {
        endpoints.MapDefaultControllerRoute();
        endpoints.MapFallbackToClientSideBlazor<Client.Program>("index.html");
      });
    }
  }
}
