using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;

namespace Rampastring.XNAUI;

/// <summary>
/// A pool for render targets. Safe for multithreaded use.
/// </summary>
public class RenderTargetPool
{
    private readonly GraphicsDevice graphicsDevice;

    private static readonly object locker = new object();

    private List<RenderTarget2D> renderTargets = new List<RenderTarget2D>();

    public RenderTargetPool(GraphicsDevice graphicsDevice)
    {
        this.graphicsDevice = graphicsDevice;
    }

    /// <summary>
    /// Adds a render target to the pool.
    /// </summary>
    /// <param name="renderTarget">The render target to add.</param>
    public void Add(RenderTarget2D renderTarget)
    {
        lock (locker)
        {
            renderTargets.Add(renderTarget);
        }
    }

    /// <summary>
    /// Creates and adds a new render target to the pool.
    /// Returns the created render target.
    /// </summary>
    /// <param name="width">The width of the render target.</param>
    /// <param name="height">The height of the render target.</param>
    public RenderTarget2D Create(int width, int height)
    {
        lock (locker)
        {
            var renderTarget = new RenderTarget2D(graphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            renderTargets.Add(renderTarget);
            return renderTarget;
        }
    }

    /// <summary>
    /// Gets the smallest render target from the pool that is at least as large as the given size.
    /// If a suitable render target is not found, then creates and returns
    /// a new render target. The returned render target can be larger
    /// than requested if no render targets of the exact requested size are available.
    /// Removes the returned render target from the pool.
    /// </summary>
    /// <param name="width">The width of the render target.</param>
    /// <param name="height">The height of the render target.</param>
    public RenderTarget2D Get(int width, int height)
    {
        lock (locker)
        {
            RenderTarget2D bestRenderTarget = null;
            int bestRenderTargetIndex = -1;

            for (int i = 0; i < renderTargets.Count; i++)
            {
                var renderTarget = renderTargets[i];
                if (renderTarget.Width < width || renderTarget.Height < height)
                    continue;

                if (bestRenderTarget != null)
                {
                    if (renderTarget.Width * renderTarget.Height > bestRenderTarget.Width * bestRenderTarget.Height)
                        continue;
                }

                bestRenderTarget = renderTarget;
                bestRenderTargetIndex = i;
            }

            if (bestRenderTarget == null)
            {
                Logger.Log($"RenderTargetPool.Get: Creating new render target of size {width}x{height}");
                bestRenderTarget = new RenderTarget2D(graphicsDevice, width, height,
                    false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            }
            else
            {
                renderTargets.RemoveAt(bestRenderTargetIndex);
            }

            return bestRenderTarget;
        }
    }

    /// <summary>
    /// Removes a render target from the pool.
    /// Returns a value that determines whether 
    /// the given render target was found and 
    /// removed from the pool.
    /// Does NOT call Dispose on the render target,
    /// so do it after calling this if you're not using the render
    /// target afterwards.
    /// </summary>
    /// <param name="renderTarget">The render target to remove.</param>
    public bool Remove(RenderTarget2D renderTarget)
    {
        lock (locker)
        {
            return renderTargets.Remove(renderTarget);
        }
    }
}
