// <copyright file="RootComponentsCollection.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Components.WebView.Gtk;

/// <summary>
/// A collection of <see cref="RootComponent"/> items.
/// </summary>
public class RootComponentsCollection : ObservableCollection<RootComponent>, IJSComponentConfiguration
{
    /// <inheritdoc />
    public JSComponentConfigurationStore JSComponents { get; } = new();
}