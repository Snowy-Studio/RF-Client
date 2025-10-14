using System;
using Microsoft.Xna.Framework.Graphics;

namespace Rampastring.XNAUI;

public class RendererException : Exception
{
    public RendererException(string message) : base(message)
    {
    }
}

/// <summary>
/// Handles render targets.
/// </summary>
internal static class RenderTargetStack
{
    public static void Initialize(RenderTarget2D finalRenderTarget, GraphicsDevice _graphicsDevice)
    {
        FinalRenderTarget = finalRenderTarget;
        graphicsDevice = _graphicsDevice;

        CurrentContext = new RenderContext(finalRenderTarget, null);
    }

    internal static void InitDetachedScaledControlRenderTarget(int renderWidth, int renderHeight)
    {
        if (DetachedScaledControlRenderTarget != null)
        {
            DetachedScaledControlRenderTarget.Dispose();
        }

        DetachedScaledControlRenderTarget = new RenderTarget2D(graphicsDevice,
            renderWidth, renderHeight,
            false, SurfaceFormat.Color,
            DepthFormat.None, 0,
            RenderTargetUsage.DiscardContents);
    }

    public static RenderTarget2D FinalRenderTarget { get; internal set; }

    /// <summary>
    /// A render target for controls that
    /// are detached and have scaling applied to them.
    /// </summary>
    public static RenderTarget2D DetachedScaledControlRenderTarget { get; internal set; }

    public static RenderContext CurrentContext;

    private static GraphicsDevice graphicsDevice;

    public static void PushRenderTarget(RenderTarget2D renderTarget, SpriteBatchSettings newSettings)
    {
        var context = new RenderContext(renderTarget, CurrentContext);
        SetRenderContext(newSettings, context);
    }

    public static void PushRenderTargets(SpriteBatchSettings newSettings, RenderTarget2D renderTarget1, RenderTarget2D renderTarget2)
    {
        var context = new RenderContext(renderTarget1, renderTarget2, CurrentContext);
        SetRenderContext(newSettings, context);
    }

    public static void PushRenderTargets(SpriteBatchSettings newSettings, RenderTarget2D renderTarget1, RenderTarget2D renderTarget2, RenderTarget2D renderTarget3)
    {
        var context = new RenderContext(renderTarget1, renderTarget2, renderTarget3, CurrentContext);
        SetRenderContext(newSettings, context);
    }

    public static void PushRenderTargets(SpriteBatchSettings newSettings, RenderTarget2D renderTarget1,
        RenderTarget2D renderTarget2, RenderTarget2D renderTarget3, RenderTarget2D renderTarget4)
    {
        var context = new RenderContext(renderTarget1, renderTarget2, renderTarget3, renderTarget4, CurrentContext);
        SetRenderContext(newSettings, context);
    }

    private static void SetRenderContext(SpriteBatchSettings newSettings, RenderContext context)
    {
        Renderer.EndDraw();
        CurrentContext = context;
        SetRenderTargetsFromContext(context);
        Renderer.PushSettingsInternal();
        Renderer.CurrentSettings = newSettings;
        Renderer.BeginDraw();
    }

    private static void SetRenderTargetsFromContext(RenderContext context)
    {
        switch (context.RenderTargetCount)
        {
            case 1:
                graphicsDevice.SetRenderTarget(context.RenderTarget); break;
            case 2:
                graphicsDevice.SetRenderTargets(context.RenderTarget, context.RenderTarget2); break;
            case 3:
                graphicsDevice.SetRenderTargets(context.RenderTarget, context.RenderTarget2, context.RenderTarget3); break;
            case 4:
                graphicsDevice.SetRenderTargets(context.RenderTarget, context.RenderTarget2, context.RenderTarget3, context.RenderTarget4); break;
            default:
                throw new RendererException($"Unable to process render context with {context.RenderTargetCount} render targets");
        }
    }

    public static void PopRenderTarget()
    {
        CurrentContext = CurrentContext.PreviousContext;
        if (CurrentContext == null)
        {
            throw new InvalidOperationException("No render context left! This usually " +
                "indicates that a control with an unique render target has " +
                "double-popped their render target.");
        }

        Renderer.EndDraw();
        Renderer.PopSettingsInternal();
        SetRenderTargetsFromContext(CurrentContext);
        Renderer.BeginDraw();
    }
}

internal class RenderContext
{
    internal RenderContext(RenderTarget2D renderTarget, RenderContext previousContext)
    {
        RenderTargetCount = 1;
        RenderTarget = renderTarget;
        PreviousContext = previousContext;
    }

    internal RenderContext(RenderTarget2D renderTarget, RenderTarget2D renderTarget2, RenderContext previousContext)
    {
        RenderTargetCount = 2;
        RenderTarget = renderTarget;
        RenderTarget2 = renderTarget2;
        PreviousContext = previousContext;
    }

    internal RenderContext(RenderTarget2D renderTarget, RenderTarget2D renderTarget2,
        RenderTarget2D renderTarget3, RenderContext previousContext)
    {
        RenderTargetCount = 3;
        RenderTarget = renderTarget;
        RenderTarget2 = renderTarget2;
        RenderTarget3 = renderTarget3;
        PreviousContext = previousContext;
    }

    internal RenderContext(RenderTarget2D renderTarget, RenderTarget2D renderTarget2,
        RenderTarget2D renderTarget3, RenderTarget2D renderTarget4, RenderContext previousContext)
    {
        RenderTargetCount = 4;
        RenderTarget = renderTarget;
        RenderTarget2 = renderTarget2;
        RenderTarget3 = renderTarget3;
        RenderTarget4 = renderTarget4;
        PreviousContext = previousContext;
    }

    public int RenderTargetCount { get; }

    public RenderTarget2D RenderTarget { get; }
    public RenderTarget2D RenderTarget2 { get; }
    public RenderTarget2D RenderTarget3 { get; }
    public RenderTarget2D RenderTarget4 { get; }
    public RenderContext PreviousContext { get; }
}
