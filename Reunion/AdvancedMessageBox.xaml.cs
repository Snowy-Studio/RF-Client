﻿// <copyright file="AdvancedMessageBox.xaml.cs" company="Maple Studio">
// Copyright (c) Maple Studio. All rights reserved.
// </copyright>

namespace Reunion;

using System.Windows;

/// <summary>
/// Interaction logic for AdvancedMessageBox.xaml.
/// </summary>
public partial class AdvancedMessageBox : Window
{
    public AdvancedMessageBox() => InitializeComponent();

    public object? Result { get; set; }
}