using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Rampastring.XNAUI.XNAControls;

/// <summary>
/// A part of a potentially long line of text.
/// </summary>
public struct XNATextPart
{
    public XNATextPart(string text, int fontIndex, float scale, Color? color, bool underlined)
    {
        _text = text;
        _fontIndex = fontIndex;
        _scale = scale;
        _color = color;
        Underlined = underlined;
        Size = Point.Zero;
        UpdateSize();
    }

    public XNATextPart(string text) : this(text, 0, 1.0f, null, false) { }

    public XNATextPart(string text, int fontIndex, Color? color) : this(text, fontIndex, 1.0f, color, false) { }

    private string _text;

    public string Text
    {
        get => _text;
        set { _text = value; UpdateSize(); }
    }

    private int _fontIndex;

    public int FontIndex
    {
        get => _fontIndex;
        set { _fontIndex = value; UpdateSize(); }
    }

    private float _scale;

    public float Scale
    {
        get => _scale;
        set { _scale = value; UpdateSize(); }
    }

    private Color? _color;

    public Color Color
    {
        get => _color ?? UISettings.ActiveSettings.TextColor;
        set => _color = value;
    }

    public bool Underlined { get; set; }

    public Point Size { get; private set; }

    private void UpdateSize()
    {
        Vector2 size = Renderer.GetTextDimensions(_text, FontIndex) * Scale;
        Size = new Point((int)size.X, (int)size.Y);
    }

    public int Width => (int)(Size.X * Scale);

    public int Height => (int)(Size.Y * Scale);

    public void Draw(Point point)
    {
        Renderer.DrawStringWithShadow(Text, FontIndex, new Vector2(point.X, point.Y), Color, Scale);
        if (Underlined)
        {
            Renderer.DrawRectangle(new Rectangle(point.X, point.Y, (int)(Size.X * Scale), 1), Color, 1);
        }
    }
}

/// <summary>
/// A text line.
/// </summary>
internal struct XNATextLine
{
    public XNATextLine(List<XNATextPart> parts)
    {
        Parts = parts;
    }

    public List<XNATextPart> Parts { get; private set; }

    public int Width
    {
        get
        {
            int width = 0;
            Parts.ForEach(p => width += p.Width);
            return width;
        }
    }

    public int Height
    {
        get
        {
            int height = 0;
            Parts.ForEach(p => height = p.Height > height ? p.Height : height);
            return height;
        }
    }

    public void AddPart(XNATextPart part)
    {
        Parts.Add(part);
    }
}

/// <summary>
/// A text renderer, practically an enhanced label control.
/// Takes <see cref="XNATextPart"/>s, automatically formats them, applies line
/// breaks if necessary and renders them.
/// </summary>
public class XNATextRenderer : XNAControl
{
    public XNATextRenderer(WindowManager windowManager) : base(windowManager)
    {
    }

    public int Padding { get; set; } = 3;
    public int SpaceBetweenLines { get; set; } = 0;

    private List<XNATextPart> originalTextParts = new List<XNATextPart>();

    private List<XNATextLine> renderedTextLines = new List<XNATextLine>();

    public void AddTextPart(XNATextPart text)
    {
        originalTextParts.Add(text);
    }

    public void AddTextLine(XNATextPart text)
    {
        originalTextParts.Add(new XNATextPart(Environment.NewLine + text.Text,
            text.FontIndex, text.Scale, text.Color, text.Underlined));
    }

    public void ClearTextParts()
    {
        originalTextParts.Clear();
        renderedTextLines.Clear();
    }

    public int GetTextPartCount() => originalTextParts.Count;

    public XNATextPart GetTextPart(int index) => originalTextParts[index];

    /// <summary>
    /// Processes the text parts fed to this text renderer.
    /// </summary>
    public void PrepareTextParts()
    {
        renderedTextLines.Clear();
        
        var preparedTextLines = new List<XNATextLine>();

        // Create a clone of the original text parts.
        // Look for newlines and pre-process them by splitting parts
        // that contain newlines.
        var partsToProcess = new List<XNATextPart>(originalTextParts);
        for (int i = 0; i < partsToProcess.Count; i++)
        {
            var part = partsToProcess[i];

            int newLineIndex = part.Text.IndexOf(Environment.NewLine);
            if (newLineIndex == -1)
            {
                // There is no newline. Simply add this part to the existing line, or if
                // no line exists, create one.

                if (preparedTextLines.Count == 0)
                    preparedTextLines.Add(new XNATextLine(new List<XNATextPart>() { part }));
                else
                    preparedTextLines[preparedTextLines.Count - 1].AddPart(part);

                continue;
            }

            // There is a newline in this text part. Split this part from the index of the newline.

            string postNewlineText = part.Text.Substring(newLineIndex + Environment.NewLine.Length);
            string remainingTextForThisPart = part.Text.Substring(0, newLineIndex);

            // Process the part before the newline as part of an existing line. If no line exists, create one.
            var textPart = new XNATextPart(remainingTextForThisPart, part.FontIndex, part.Scale, part.Color, part.Underlined);
            if (preparedTextLines.Count == 0)
                preparedTextLines.Add(new XNATextLine(new List<XNATextPart>() { textPart }));
            else
                preparedTextLines[preparedTextLines.Count - 1].AddPart(textPart);

            var newTextPart = new XNATextPart(postNewlineText, part.FontIndex, part.Scale, part.Color, part.Underlined);
            partsToProcess.Insert(i + 1, newTextPart);
            preparedTextLines.Add(new XNATextLine(new List<XNATextPart>(1)));
        }

        // Look for lines that contain no text parts, or only empty text parts.
        // If ones exist, add a space to them so they still contain some height.
        // If there are empty text parts in a line that otherwise contains text,
        // remove the empty parts.
        for (int i = 0; i < preparedTextLines.Count; i++)
        {
            if (preparedTextLines[i].Parts.Count == 0)
            {
                preparedTextLines[i].AddPart(new XNATextPart(" "));
            }
            else if (preparedTextLines[i].Parts.TrueForAll(p => p.Text == ""))
            {
                var existingPart = preparedTextLines[i].Parts[0];
                preparedTextLines[i].Parts[0] = new XNATextPart(" ", existingPart.FontIndex, existingPart.Scale, existingPart.Color, existingPart.Underlined);
            }
            else
            {
                while (true)
                {
                    int emptyTextPartIndex = preparedTextLines[i].Parts.FindIndex(p => p.Text == string.Empty);
                    if (emptyTextPartIndex < 0)
                        break;

                    preparedTextLines[i].Parts.RemoveAt(emptyTextPartIndex);

                }
            }
        }

        // For each line, process their text parts.
        for (int i = 0; i < preparedTextLines.Count; i++)
        {
            var line = new XNATextLine(new List<XNATextPart>());
            renderedTextLines.Add(line);

            var lineOriginalTextParts = new List<XNATextPart>(preparedTextLines[i].Parts);
            int remainingWidth = Width - (Padding * 2);

            foreach (XNATextPart textPart in lineOriginalTextParts) 
            {
                string remainingText = textPart.Text;
                var currentOutputPart = new XNATextPart("", textPart.FontIndex, textPart.Scale, textPart.Color, textPart.Underlined);

                while (true)
                {
                    var words = remainingText.Split(new[] { ' ' }, StringSplitOptions.None);
                    foreach (string word in words)

                    {
                        if (word == "")
                        {
                            currentOutputPart.Text += " ";
                            remainingWidth -= (int)Renderer.GetTextDimensions(" ", textPart.FontIndex).X;
                            continue;
                        }

                        string wordToProcess = word;

                        string wordWithSpace = wordToProcess + " ";
                        int wordWidth = (int)Renderer.GetTextDimensions(wordToProcess, textPart.FontIndex).X;
                        int wordWidthWithSpace = (int)Renderer.GetTextDimensions(wordWithSpace, textPart.FontIndex).X;
                        if (wordWidth < remainingWidth)
                        {
                            remainingWidth -= wordWidthWithSpace;
                            currentOutputPart.Text += wordWithSpace;
                        }
                        else
                        {
                            line.Parts.Add(currentOutputPart);

                            remainingWidth = Width - (Padding * 2) - wordWidthWithSpace;
                            currentOutputPart = new XNATextPart(wordWithSpace, textPart.FontIndex, textPart.Scale, textPart.Color, textPart.Underlined);
                            line = new XNATextLine(new List<XNATextPart>());
                            renderedTextLines.Add(line);
                        }
                    }

                    line.Parts.Add(currentOutputPart);
                    break;
                }
            }
        }

        ClientRectangleUpdated -= XNATextRenderer_ClientRectangleUpdated;
        Height = renderedTextLines.Sum(l => l.Height);
        if (renderedTextLines.Count > 1)
            Height += (renderedTextLines.Count - 1) * SpaceBetweenLines;

        ClientRectangleUpdated += XNATextRenderer_ClientRectangleUpdated;
    }

    private void XNATextRenderer_ClientRectangleUpdated(object sender, EventArgs e)
    {
        PrepareTextParts();
    }

    public override void Draw(GameTime gameTime)
    {
        int y = 0;

        foreach (XNATextLine line in renderedTextLines)
        {
            int x = 0;

            foreach (XNATextPart part in line.Parts)
            {
                DrawStringWithShadow(part.Text, part.FontIndex, new Vector2(x, y), part.Color, 1f);
                x += part.Width + 1;
            }

            y += line.Height + SpaceBetweenLines;
        }

        base.Draw(gameTime);
    }
}
