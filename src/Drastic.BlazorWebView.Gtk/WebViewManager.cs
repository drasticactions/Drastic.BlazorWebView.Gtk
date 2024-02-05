// <copyright file="WebViewManager.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using System.Runtime.Versioning;
using System.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using WebKit;
using MemoryInputStream = Gio.Internal.MemoryInputStream;

namespace Microsoft.AspNetCore.Components.WebView.Gtk;

/// <summary>
/// WebView Manager.
/// Partially based on the WinForms version.
/// https://github.com/dotnet/maui/blob/main/src/BlazorWebView/src/WindowsForms/BlazorWebView.cs
/// </summary>
[UnsupportedOSPlatform("OSX")]
[UnsupportedOSPlatform("Windows")]
internal class WebViewManager : Microsoft.AspNetCore.Components.WebView.WebViewManager
{
    private const string Scheme = "app";
    private static readonly Uri BaseUri = new($"{Scheme}://localhost/");
    private readonly GtkBlazorWebViewDeveloperTools developerTools;
    private readonly WebKit.WebView webView;
    private readonly string relativeHostPath;
    private readonly ILogger<WebViewManager>? logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebViewManager"/> class.
    /// </summary>
    /// <param name="webView">A <see cref="WebKit.WebView"/> to access platform-specific GTK WebKit.WebView APIs.</param>
    /// <param name="serviceProvider">A service provider containing services to be used by this class and also by application code.</param>
    /// <param name="dispatcher">A <see cref="Dispatcher"/> instance that can marshal calls to the required thread or sync context.</param>
    /// <param name="fileProvider">Provides static content to the webview.</param>
    /// <param name="jsComponents">Describes configuration for adding, removing, and updating root components from JavaScript code.</param>
    /// <param name="contentRootRelativeToAppRoot">Path to the app's content root relative to the application root directory.</param>
    /// <param name="hostPageRelativePath">Path to the host page within the <paramref name="fileProvider"/>.</param>
    /// <param name="logger">Logger to send log messages to.</param>
    public WebViewManager(
        WebKit.WebView webView,
        IServiceProvider serviceProvider,
        Dispatcher dispatcher,
        IFileProvider fileProvider,
        JSComponentConfigurationStore jsComponents,
        string contentRootRelativeToAppRoot,
        string hostPageRelativePath,
        ILogger logger)
        : base(
        serviceProvider,
        dispatcher,
        BaseUri,
        fileProvider,
        jsComponents,
        hostPageRelativePath)
    {
        this.developerTools = serviceProvider.GetService<GtkBlazorWebViewDeveloperTools>() ?? new GtkBlazorWebViewDeveloperTools();
        this.relativeHostPath = hostPageRelativePath;
        this.logger = serviceProvider.GetService<ILogger<WebViewManager>>();

        this.webView = webView;
        this.webView.GetSettings().EnableDeveloperExtras = this.developerTools.Enabled;

        // This is necessary to automatically serve the files in the `_framework` virtual folder.
        // Using `file://` will cause the webview to look for the `_framework` files on the file system,
        // and it won't find them.
        if (this.webView.WebContext is null)
        {
            throw new Exception("WebView.WebContext is null");
        }

        this.webView.WebContext.RegisterUriScheme(Scheme, this.HandleUriScheme);

        var ucm = webView.GetUserContentManager();
        ucm.AddScript(UserScript.New(
            source:
            """
                window.__receiveMessageCallbacks = [];
            
                window.__dispatchMessageCallback = function(message) {
                    window.__receiveMessageCallbacks.forEach(function(callback) { callback(message); });
                };
            
                window.external = {
                    sendMessage: function(message) {
                        window.webkit.messageHandlers.webview.postMessage(message);
                    },
                    receiveMessage: function(callback) {
                        window.__receiveMessageCallbacks.push(callback);
                    }
                };
            """,
            injectedFrames: UserContentInjectedFrames.AllFrames,
            injectionTime: UserScriptInjectionTime.Start));

        UserContentManager.ScriptMessageReceivedSignal.Connect(ucm, (_, signalArgs) =>
        {
            var result = signalArgs.Value;
            this.MessageReceived(BaseUri, result.ToString());
        }, true, "webview");

        if (!ucm.RegisterScriptMessageHandler("webview", null))
        {
            throw new Exception("Could not register script message handler");
        }
    }

    /// <inheritdoc/>
    protected override void NavigateCore(Uri absoluteUri)
    {
        this.logger?.LogDebug($"Navigating to \"{absoluteUri}\"");
        this.webView.LoadUri(absoluteUri.ToString());
    }

    /// <inheritdoc/>
    protected override async void SendMessage(string message)
    {
        var script = $"__dispatchMessageCallback(\"{HttpUtility.JavaScriptStringEncode(message)}\")";
        this.logger?.LogDebug($"Dispatching `{script}`");
        _ = await this.webView.EvaluateJavascriptAsync(script);
    }

    private void HandleUriScheme(URISchemeRequest request)
    {
        if (request.GetScheme() != Scheme)
        {
            throw new Exception($"Invalid scheme \"{request.GetScheme()}\"");
        }

        var uri = request.GetUri();
        if (request.GetPath() == "/")
        {
            uri += this.relativeHostPath;
        }

        this.logger?.LogDebug($"Fetching \"{uri}\"");

        if (this.TryGetResponseContent(
                uri,
                false,
                out var statusCode,
                out var statusMessage,
                out var content,
                out var headers))
        {
            using var ms = new MemoryStream();
            content.CopyTo(ms);
            var streamPtr = MemoryInputStream.NewFromData(ref ms.GetBuffer()[0], (uint)ms.Length, _ => { });
            var inputStream = new InputStream(streamPtr, false);
            request.Finish(inputStream, ms.Length, headers["Content-Type"]);
        }
        else
        {
            throw new Exception($"Failed to serve \"{uri}\". {statusCode} - {statusMessage}");
        }
    }

    // Workaround for protection level access
    private class InputStream : Gio.InputStream
    {
        protected internal InputStream(IntPtr ptr, bool ownedRef)
            : base(ptr, ownedRef)
        {
        }
    }
}