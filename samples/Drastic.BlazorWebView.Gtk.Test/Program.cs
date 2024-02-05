using System.Runtime.Versioning;
using Drastic.BlazorWebView.Gtk.Test;
using Microsoft.AspNetCore.Components.WebView.Gtk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Based on https://github.com/JinShil/BlazorWebView/tree/main/WebKitGtk.Test
[UnsupportedOSPlatform("OSX")]
[UnsupportedOSPlatform("Windows")]
internal class Program
{
    private static int Main(string[] args)
    {
        WebKit.Module.Initialize();

        var application = Adw.Application.New("org.gir.core", Gio.ApplicationFlags.FlagsNone);

        application.OnActivate += (sender, args) =>
        {
            var window = Gtk.ApplicationWindow.New((Adw.Application)sender);
            window.Title = "BlazorWebView.GTK";
            window.SetDefaultSize(800, 600);

            // Create your service provider...
            var serviceProvider = new ServiceCollection()
                .AddLogging((lb) =>
                {
                    lb.AddSimpleConsole(options =>
                        {
                            options.TimestampFormat = "hh:mm:ss ";
                        })
                        .SetMinimumLevel(LogLevel.Information);
                });

            // ... and add the BlazorWebView.
            serviceProvider.AddBlazorWebView();

            // ... and add the developer tools.
            serviceProvider.AddBlazorWebViewDeveloperTools();

            // Build the service provider.
            var provider = serviceProvider.BuildServiceProvider();

            // Add the BlazorWebView
            // Then the root component with the selector for your page.
            var webView = new BlazorWebView(provider) { };
            webView.RootComponents.Add<App>("#app");
            window.SetChild(webView);
            window.Show();
        };

        return application.Run();
    }
}