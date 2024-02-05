# Drastic.BlazorWebView.Gtk

[![NuGet Version](https://img.shields.io/nuget/v/BlazorWebView.Gtk.svg)](https://www.nuget.org/packages/Drastic.BlazorWebView.Gtk/) ![License](https://img.shields.io/badge/License-MIT-blue.svg)

Drastic.BlazorWebView.Gtk is an *__experimental__* wrapping of Microsoft.AspNetCore.Components.WebView with a WebKit-based GTK View, bound by [Gir.Core](https://github.com/gircore/gir.core). Based on [JinShil/BlazorWebView](https://github.com/JinShil/BlazorWebView), this project tries to match the maintained [Microsoft](https://github.com/dotnet/maui/tree/main/src/BlazorWebView) implementation for WPF and WinForms. It's a proof-of-concept for proposing adding support for this to .NET and .NET MAUI directly.

![image](https://raw.githubusercontent.com/drasticactions/Drastic.BlazorWebView.Gtk/main/.github/images/gtkwebview.png)

__IMPORTANT__: This project only supports Linux, as it depends on GirCore.WebKit which only supports Linux. 

__IMPORTANT__: This project is built with GirCore `0.4.x`. The `0.5.x` previews builds include breaking changes to APIs and dependencies that would make it hard to support both. 

__IMPORTANT__: This is *__experimental__*. Do not depend on this project for anything serious. If you do use this, please file issues, but understand that support is limited to none and your issues may not be fixed by me. When it doubt, fork.

### Dependencies
- [GirCore.Gtk-4.0](https://www.nuget.org/packages/GirCore.Gtk-4.0)
- [GirCore.WebKit-6.0](https://www.nuget.org/packages/GirCore.WebKit-6.0)
- [Microsoft.AspNetCore.Components.WebView](https://www.nuget.org/packages/Microsoft.AspNetCore.Components.WebView)

While not a source dependency, you will most likely need [GirCore.Adw-1](https://www.nuget.org/packages/GirCore.Adw-1) to host your application.

## Setup
- Install the `Drastic.BlazorWebView.Gtk` nuget to your application.
- If you wish to host your Razor pages inside of your application project, be sure to set your `csproj` SDK value to `Microsoft.NET.Sdk.Razor`.

```csharp
internal class Program
{
    private static int Main(string[] args)
    {
        WebKit.Module.Initialize();

        // Using GirCore.Adw-1
        var application = Adw.Application.New("org.gir.core", Gio.ApplicationFlags.FlagsNone);

        application.OnActivate += (sender, args) =>
        {
            var window = Gtk.ApplicationWindow.New((Adw.Application)sender);
            window.Title = "BlazorWebView.GTK";
            window.SetDefaultSize(800, 600);

            // Create your service provider...
            var serviceProvider = new ServiceCollection();

            // ... and add the BlazorWebView.
            serviceProvider.AddBlazorWebView();

            // ... and add the developer tools for F12 support.
            serviceProvider.AddBlazorWebViewDeveloperTools();

            // Build the service provider.
            var provider = serviceProvider.BuildServiceProvider();

            // Add the BlazorWebView
            // Then the root component with the selector for your page.
            // This will most likely be whatever page hosts the Router.
            var webView = new BlazorWebView(provider) { };
            webView.RootComponents.Add<App>("#app");
            window.SetChild(webView);
            window.Show();
        };

        return application.Run();
    }
}
```
- View the `samples` directory for complete examples.

## Known Limitations
- No `UrlLoadingEventArgs`, `BlazorWebViewInitializingEventArgs`, or `BlazorWebViewInitializedEventArgs` events.
- No Starter Templates. Creating a Blazor Hybrid app with its templates, porting that code to a shared library or into the project directly, and running it should work.
- Probably will have issues with hosted class libraries and shared components, or "complex" Blazor Hybrid apps beyond the template.
- Probably more, file a bug! 