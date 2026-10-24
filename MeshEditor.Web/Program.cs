using MeshEditor.Web;
using MeshEditor.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var settings = new AppSettings();
builder.Configuration.Bind(settings);
builder.Services.AddSingleton(settings);
builder.Services.AddSingleton<RemoteStorage>();

await builder.Build().RunAsync();
