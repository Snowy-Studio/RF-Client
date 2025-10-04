using System;

namespace Rampastring.XNAUI;

/// <summary>
/// Event args for handling input.
/// </summary>
public class InputEventArgs : EventArgs
{
    /// <summary>
    /// Determines whether this input has been handled already. 
    /// If yes, the input event is not forwarded on to parent controls.
    /// </summary>
    public bool Handled { get; set; }
}
