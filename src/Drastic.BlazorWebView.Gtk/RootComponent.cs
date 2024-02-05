// <copyright file="RootComponent.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

namespace Microsoft.AspNetCore.Components.WebView.Gtk;

/// <summary>
/// Describes a root component that can be added to a <see cref="BlazorWebView"/>.
/// </summary>
public class RootComponent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RootComponent"/> class.
    /// Constructs an instance of <see cref="RootComponent"/>.
    /// </summary>
    /// <param name="selector">The CSS selector string that specifies where in the document the component should be placed. This must be unique among the root components within the <see cref="BlazorWebView"/>.</param>
    /// <param name="componentType">The type of the root component. This type must implement <see cref="IComponent"/>.</param>
    /// <param name="parameters">An optional dictionary of parameters to pass to the root component.</param>
    public RootComponent(string selector, Type componentType, IDictionary<string, object?>? parameters)
    {
        if (string.IsNullOrWhiteSpace(selector))
        {
            throw new ArgumentException($"'{nameof(selector)}' cannot be null or whitespace.", nameof(selector));
        }

        this.Selector = selector;
        this.ComponentType = componentType ?? throw new ArgumentNullException(nameof(componentType));
        this.Parameters = parameters;
    }

    /// <summary>
    /// Gets the CSS selector string that specifies where in the document the component should be placed.
    /// This must be unique among the root components within the <see cref="BlazorWebView"/>.
    /// </summary>
    public string Selector { get; }

    /// <summary>
    /// Gets the type of the root component. This type must implement <see cref="IComponent"/>.
    /// </summary>
    public Type ComponentType { get; }

    /// <summary>
    /// Gets an optional dictionary of parameters to pass to the root component.
    /// </summary>
    public IDictionary<string, object?>? Parameters { get; }

    internal Task AddToWebViewManagerAsync(WebViewManager webViewManager)
    {
        if (string.IsNullOrWhiteSpace(this.Selector))
        {
            throw new InvalidOperationException($"{nameof(RootComponent)} requires a value for its {nameof(this.Selector)} property, but no value was set.");
        }

        if (this.ComponentType is null)
        {
            throw new InvalidOperationException($"{nameof(RootComponent)} requires a value for its {nameof(this.ComponentType)} property, but no value was set.");
        }

        var parameterView = this.Parameters == null ? ParameterView.Empty : ParameterView.FromDictionary(this.Parameters);
        return webViewManager.AddRootComponentAsync(this.ComponentType, this.Selector, parameterView);
    }

    internal Task RemoveFromWebViewManagerAsync(WebViewManager webviewManager)
    {
        return webviewManager.RemoveRootComponentAsync(this.Selector);
    }
}