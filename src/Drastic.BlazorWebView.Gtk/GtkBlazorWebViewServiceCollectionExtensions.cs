// <copyright file="GtkBlazorWebViewServiceCollectionExtensions.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using System.Runtime.Versioning;
using Microsoft.AspNetCore.Components.WebView.Gtk;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to <see cref="IServiceCollection"/>.
/// </summary>
public static class GtkBlazorWebViewServiceCollectionExtensions
{
    /// <summary>
    /// Enables Developer tools on the underlying WebView controls.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddBlazorWebViewDeveloperTools(this IServiceCollection services)
    {
        return services.AddSingleton<GtkBlazorWebViewDeveloperTools>(new GtkBlazorWebViewDeveloperTools { Enabled = true });
    }
}