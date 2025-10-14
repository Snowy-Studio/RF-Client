using System;
using Rampastring.XNAUI.XNAControls;

namespace Rampastring.XNAUI.Input;

/// <summary>
/// Interface for outside components implementing Input Method Editor (IME) support.
/// </summary>
public interface IIMEHandler
{
    /// <summary>
    /// Determines whether IME is allowed to compose text.
    /// </summary>
    bool TextCompositionEnabled { get; }

    void RegisterXNATextBox(XNATextBox sender, Action<char> handleCharInput);

    void KillXNATextBox(XNATextBox sender);

    void OnSelectedChanged(XNATextBox sender);
    void OnTextChanged(XNATextBox sender);

    bool HandleCharInput(XNATextBox sender, char input);

    bool HandleScrollLeftKey(XNATextBox sender);
    bool HandleScrollRightKey(XNATextBox sender);

    bool HandleBackspaceKey(XNATextBox sender);
    bool HandleDeleteKey(XNATextBox sender);
    bool HandleEnterKey(XNATextBox sender);
    bool HandleEscapeKey(XNATextBox sender);

    bool GetDrawCompositionText(XNATextBox sender, out string composition, out int compositionCursorPosition);
}
