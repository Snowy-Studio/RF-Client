using System;
using System.Collections.Generic;
using System.Reflection;
using Rampastring.XNAUI.XNAControls;

namespace Rampastring.XNAUI;

/// <summary>
/// The exception that is thrown when the <see cref="GUICreator"/> fails to find a matching
/// constructor for a GUI control type.
/// </summary>
public class ConstructorNotFoundException : Exception
{
    public ConstructorNotFoundException(string message) : base(message)
    {
    }
}

/// <summary>
/// Allows creating controls based on their internal names with the help 
/// of reflection.
/// </summary>
public class GUICreator
{
    private List<Type> controlTypes = new List<Type>()
    {
        typeof(XNAControl),
        typeof(XNAButton),
        typeof(XNACheckBox),
        typeof(XNADropDown),
        typeof(XNALabel),
        typeof(XNALinkLabel),
        typeof(XNAListBox),
        typeof(XNAMultiColumnListBox),
        typeof(XNAPanel),
        typeof(XNAProgressBar),
        typeof(XNASuggestionTextBox),
        typeof(XNATextBox),
        typeof(XNATrackbar),
    };

    /// <summary>
    /// Adds a control type to the list of available control types.
    /// </summary>
    /// <param name="type">The control type to add. Needs to be a class type derived from XNAControl.</param>
    public void AddControl(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (!type.IsSubclassOf(typeof(XNAControl)))
            throw new ArgumentException("GUICreator.AddControl: Type needs to be a class type derived from XNAControl.");

        if (controlTypes.Contains(type))
            throw new InvalidOperationException("GUICreator.AddControl: The type " + type.Name + " is already added to the control type list!");

        controlTypes.Add(type);
    }

    /// <summary>
    /// Creates and returns a control.
    /// </summary>
    /// <param name="windowManager">The WindowManager.</param>
    /// <param name="controlTypeName">The name of the control class type to create.</param>
    /// <returns>A control of the specified type.</returns>
    public XNAControl CreateControl(WindowManager windowManager, string controlTypeName)
    {
        if (windowManager == null)
            throw new ArgumentNullException(nameof(windowManager));

        if (controlTypeName == null)
            throw new ArgumentNullException(nameof(controlTypeName));

        Type type = controlTypes.Find(c => c.Name == controlTypeName);

        if (type == null)
            throw new ArgumentException("GUICreator.CreateControl: Cannot find control type " + controlTypeName);

        ConstructorInfo constructor = type.GetConstructor(new Type[] { typeof(WindowManager) });

        if (constructor == null)
            throw new ConstructorNotFoundException("GUICreator.CreateControl: Cannot find constructor accepting only WindowManager for control type " + controlTypeName);

        return (XNAControl)constructor.Invoke(new object[] { windowManager });
    }
}
