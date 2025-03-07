﻿namespace Reunion;

using System.Windows.Input;

public class CommandViewModel : NotifyPropertyChangedBase
{
    private string? text;

    public string Text
    {
        get => text;
        set
        {
            text = value;
            NotifyPropertyChanged();
        }
    }

    private ICommand? command;

    public ICommand Command
    {
        get => command;
        set
        {
            command = value;
            NotifyPropertyChanged();
        }
    }
}