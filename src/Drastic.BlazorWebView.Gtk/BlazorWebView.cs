// <copyright file="BlazorWebView.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Versioning;
using Gtk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components.WebView.Gtk;

/// <summary>
/// GTK Blazor Webview.
/// Partially based on the WinForms version.
/// https://github.com/dotnet/maui/blob/main/src/BlazorWebView/src/WindowsForms/BlazorWebView.cs.
/// </summary>
[UnsupportedOSPlatform("OSX")]
[UnsupportedOSPlatform("Windows")]
public sealed class BlazorWebView : WebKit.WebView
{
    private bool isDisposed;
    private WebViewManager? webviewManager;
    private Dispatcher componentsDispatcher;
    private IServiceProvider? services;
    private string? hostPage;
    private string? startPath;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BlazorWebView"/> class.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="hostPage">HostPage for the application, defaults to wwwroot/index.html.</param>
    /// <param name="startPath">Start path for the application, defaults to /.</param>
    public BlazorWebView(IServiceProvider services, string hostPage = "wwwroot/index.html", string? startPath = "/")
    {
        this.services = services;
        this.hostPage = hostPage;
        this.startPath = startPath;
        this.componentsDispatcher = Dispatcher.CreateDefault();
        this.RootComponents.CollectionChanged += this.HandleRootComponentsCollectionChanged;
        this.OnInitialize();
    }

    private bool RequiredStartupPropertiesSet =>
        this.hostPage != null &&
        this.services != null;

    /// <summary>
    /// Gets a collection of <see cref="RootComponent"/> instances that specify the Blazor <see cref="IComponent"/> types
    /// to be used directly in the specified <see cref="HostPage"/>.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RootComponentsCollection RootComponents { get; } = new();

    /// <summary>
    /// Creates a file provider for static assets used in the <see cref="BlazorWebView"/>. The default implementation
    /// serves files from disk. Override this method to return a custom <see cref="IFileProvider"/> to serve assets such
    /// as <c>wwwroot/index.html</c>. Call the base method and combine its return value with a <see cref="CompositeFileProvider"/>
    /// to use both custom assets and default assets.
    /// </summary>
    /// <param name="contentRootDir">The base directory to use for all requested assets, such as <c>wwwroot</c>.</param>
    /// <returns>Returns a <see cref="IFileProvider"/> for static assets.</returns>
    private IFileProvider CreateFileProvider(string contentRootDir)
    {
        if (Directory.Exists(contentRootDir))
        {
            // Typical case after publishing, or if you're copying content to the bin dir in development for some nonstandard reason
            return new PhysicalFileProvider(contentRootDir);
        }
        else
        {
            // Typical case in development, as the files come from Microsoft.AspNetCore.Components.WebView.StaticContentProvider
            // instead and aren't copied to the bin dir
            return new NullFileProvider();
        }
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        this.isDisposed = true;
        base.Dispose();
        this.webviewManager?.DisposeAsync();
    }

    private void HandleRootComponentsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        this.CheckDisposed();

        // If we haven't initialized yet, this is a no-op
        if (this.webviewManager != null)
        {
            // Dispatch because this is going to be async, and we want to catch any errors
            _ = this.componentsDispatcher.InvokeAsync(async () =>
            {
                var newItems = (eventArgs.NewItems ?? Array.Empty<object>()).Cast<RootComponent>();
                var oldItems = (eventArgs.OldItems ?? Array.Empty<object>()).Cast<RootComponent>();

                foreach (var item in newItems.Except(oldItems))
                {
                    await item.AddToWebViewManagerAsync(this.webviewManager);
                }

                foreach (var item in oldItems.Except(newItems))
                {
                    await item.RemoveFromWebViewManagerAsync(this.webviewManager);
                }
            });
        }
    }

    private void CheckDisposed()
    {
        ObjectDisposedException.ThrowIf(this.isDisposed, this);
    }

    private void OnInitialize()
    {
        // If we don't have all the required properties, or if there's already a WebViewManager, do nothing
        if (!this.RequiredStartupPropertiesSet || this.webviewManager != null)
        {
            return;
        }

        var logger = this.services!.GetService<ILogger<BlazorWebView>>() ?? NullLogger<BlazorWebView>.Instance;

        // We assume the host page is always in the root of the content directory, because it's
        // unclear there's any other use case. We can add more options later if so.
        var entryAssemblyLocation = Assembly.GetEntryAssembly()?.Location;
        string appRootDir = !string.IsNullOrEmpty(entryAssemblyLocation) ? Path.GetDirectoryName(entryAssemblyLocation)! : Environment.CurrentDirectory;

        var hostPageFullPath = Path.GetFullPath(Path.Combine(appRootDir, this.hostPage!)); // HostPage is nonnull because RequiredStartupPropertiesSet is checked above
        var contentRootDirFullPath = Path.GetDirectoryName(hostPageFullPath)!;
        var contentRootRelativePath = Path.GetRelativePath(appRootDir, contentRootDirFullPath);
        var hostPageRelativePath = Path.GetRelativePath(contentRootDirFullPath, hostPageFullPath);

        this.webviewManager = new WebViewManager(
            this,
            this.services!,
            this.componentsDispatcher,
            this.CreateFileProvider(hostPageFullPath),
            this.RootComponents.JSComponents,
            contentRootRelativePath,
            hostPageRelativePath,
            logger);

        foreach (var rootComponent in this.RootComponents)
        {
            // Since the page isn't loaded yet, this will always complete synchronously
            _ = rootComponent.AddToWebViewManagerAsync(this.webviewManager);
        }

        this.webviewManager.Navigate(this.startPath);
    }
}