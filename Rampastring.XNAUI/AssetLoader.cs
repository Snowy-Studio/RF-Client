using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using Color = Microsoft.Xna.Framework.Color;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

namespace Rampastring.XNAUI;

/// <summary>
/// A static class that provides easy-to-use methods
/// for loading and generating assets such as textures and sounds.
/// </summary>
public static class AssetLoader
{
    /// <summary>
    /// A list of filesystem paths that assets are attempted to load from.
    /// </summary>
    public static List<string> AssetSearchPaths;

    private static GraphicsDevice graphicsDevice;
    private static ContentManager contentManager;

    private static List<Texture2D> textureCache;
    private static List<SoundEffect> soundCache;

    public static bool IsInitialized { get; private set; } = false;

    /// <summary>
    /// Initializes the AssetLoader.
    /// </summary>
    /// <param name="gd">The graphics device.</param>
    /// <param name="content">The game content manager.</param>
    public static void Initialize(GraphicsDevice gd, ContentManager content)
    {
        if (IsInitialized)
            throw new InvalidOperationException("AssetLoader is already initialized.");

        IsInitialized = true;

        graphicsDevice = gd;
        AssetSearchPaths = new List<string>();
        textureCache = new List<Texture2D>();
        soundCache = new List<SoundEffect>();
        contentManager = content;
    }

    public static Texture2D Base64ToTexture(string base64String)
    {

        if (base64String == null) return null;

        try
        {

            // 移除 Base64 前缀（如果存在）
            if (base64String.StartsWith("data:image"))
            {
                int commaIndex = base64String.IndexOf(",");
                if (commaIndex > 0)
                {
                    base64String = base64String.Substring(commaIndex + 1);
                }
            }

            // 解码 Base64
            byte[] imageData = Convert.FromBase64String(base64String);

            // 加载到 Texture2D
            using (MemoryStream ms = new MemoryStream(imageData))
            {
                return Texture2D.FromStream(graphicsDevice, ms);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Base64 转换失败: " + ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Loads a texture with the specific name. If the texture isn't found from any
    /// asset search path, returns a dummy texture.
    /// </summary>
    /// <param name="name">The name of the texture.</param>
    /// <returns>The texture if it was found and could be loaded, otherwise a dummy texture.</returns>
    public static Texture2D LoadTexture(string name)
    {
        var cachedTexture = textureCache.Find(t => t.Name == name);

        if (cachedTexture != null)
            return cachedTexture;

        var texture = LoadTextureInternal(name);
        if (texture != null)
        {
            textureCache.Add(texture);
            return texture;
        }

        return CreateDummyTexture();
    }

    /// <summary>
    /// Loads a texture with the specific name. Does not look at textures in 
    /// the texture cache, and doesn't add loaded textures to the texture cache.
    /// </summary>
    /// <param name="name">The name of the texture.</param>
    /// <returns>The texture if it was found and could be loaded, otherwise a dummy texture.</returns>
    public static Texture2D LoadTextureUncached(string name)
    {
        var texture = LoadTextureInternal(name);
        if (texture != null)
            return texture;

        return CreateDummyTexture();
    }

    private static Texture2D LoadTextureInternal(string name)
    {
        try
        {
            foreach (string searchPath in AssetSearchPaths)
            {
                FileInfo fileInfo = SafePath.GetFile(searchPath, name);

                if (fileInfo.Exists)
                {
                    using FileStream fs = fileInfo.OpenRead();
                    var texture = Texture2D.FromStream(graphicsDevice, fs);
                    texture.Name = name;
                    PremultiplyAlpha(texture);

                    return texture;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log("AssetLoader.LoadTextureInternal: loading texture " + name + " failed! Message: " + ex.Message);
        }

        return null;
    }

    private static void PremultiplyAlpha(Texture2D texture)
    {
        var data = new Color[texture.Width * texture.Height];
        texture.GetData(data);

        for (int i = 0; i < data.Length; i++)
        {
            data[i].R = (byte)(data[i].R * data[i].A / 255);
            data[i].G = (byte)(data[i].G * data[i].A / 255);
            data[i].B = (byte)(data[i].B * data[i].A / 255);
        }

        texture.SetData(data);
    }

    /// <summary>
    /// Creates and returns a 100x100 pink square.
    /// </summary>
    private static Texture2D CreateDummyTexture()
    {
        return CreateTexture(new Color(255, 54, 244), 100, 100);
    }

    /// <summary>
    /// Checks if a specified asset file exists.
    /// </summary>
    /// <param name="name">The name of the asset file.</param>
    /// <returns></returns>
    public static bool AssetExists(string name)
    {
        foreach (string searchPath in AssetSearchPaths)
        {
            if (SafePath.GetFile(searchPath, name).Exists)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Creates a one-colored texture.
    /// </summary>
    /// <param name="color">The color of the texture.</param>
    /// <param name="width">The width of the texture in pixels.</param>
    /// <param name="height">The height of the texture in pixels.</param>
    /// <returns>A texture.</returns>
    public static Texture2D CreateTexture(Color color, int width, int height)
    {
        var texture = new Texture2D(graphicsDevice, width, height, false, SurfaceFormat.Color);

        var colorArray = new Color[width * height];

        for (int i = 0; i < colorArray.Length; i++)
            colorArray[i] = color;

        texture.SetData(colorArray);

        return texture;
    }

    /// <summary>
    /// Creates a texture from a <see cref="Image"/>.
    /// Returns null if creating the texture fails.
    /// </summary>
    /// <param name="image">The image.</param>
    /// <returns>The created texture, or null if creating the texture fails.</returns>
    public static Texture2D TextureFromImage(Image image)
    {
        try
        {
            using var stream = new MemoryStream();
            image.Save(stream, new PngEncoder());
            var texture = Texture2D.FromStream(graphicsDevice, stream);
            PremultiplyAlpha(texture);
            return texture;
        }
        catch (Exception ex)
        {
            Logger.Log("AssetLoader.TextureFromImage: failed to create texture! Message: " + ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Loads a sound effect with the given name.
    /// </summary>
    /// <param name="name">The name of the sound effect.</param>
    /// <returns>The loaded sound effect, or null if the sound effect isn't found.</returns>
    public static SoundEffect LoadSound(string name)
    {
        SoundEffect cachedSound = soundCache.Find(se => se.Name == name);

        if (cachedSound != null)
            return cachedSound;

        foreach (string searchPath in AssetSearchPaths)
        {
            FileInfo fileInfo = SafePath.GetFile(searchPath, name);

            if (fileInfo.Exists)
            {
                using FileStream fs = fileInfo.OpenRead();
                var se = SoundEffect.FromStream(fs);
                se.Name = name;
                soundCache.Add(se);
                return se;
            }
        }

        Logger.Log("AssetLoader.LoadSound: Sound not found! " + name);

        return null;
    }

    /// <summary>
    /// Loads a <see cref="Song"/> with the specified name.
    /// </summary>
    /// <param name="name">The name of the song.</param>
    /// <returns>The loaded song, or null if loading the song fails.</returns>
    public static Song LoadSong(string name)
    {
        try
        {
            return contentManager.Load<Song>(name);
        }
        catch (Exception ex)
        {
            Logger.Log("Loading song " + name + " failed! Message: " + ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Loads an <see cref="Effect"/> with the specified name.
    /// </summary>
    /// <param name="name">The name of the effect.</param>
    /// <returns>The loaded effect, or null if loading the effect fails.</returns>
    public static Effect LoadEffect(string name)
    {
        try
        {
            return contentManager.Load<Effect>(name);
        }
        catch (Exception ex)
        {
            Logger.Log("Loading shader effect " + name + " failed! Message: " + ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Creates a color based on a color string in the form "R,G,B" or "R,G,B,A". All values must be between 0 and 255.
    /// </summary>
    /// <param name="colorString">The color string in the form "R,G,B,A". All values must be between 0 and 255.</param>
    /// <returns>A XNA Color struct based on the given string.</returns>
    public static Color GetColorFromString(string colorString)
    {
        try
        {
            string[] colorArray = colorString.Split(',');

            int alpha = 255;
            if (colorArray.Length == 4)
            {
                alpha = Convert.ToByte(colorArray[3], CultureInfo.InvariantCulture);
            }

            var color = new Color(
                Convert.ToByte(colorArray[0], CultureInfo.InvariantCulture),
                Convert.ToByte(colorArray[1], CultureInfo.InvariantCulture),
                Convert.ToByte(colorArray[2], CultureInfo.InvariantCulture),
                alpha);

            return color;
        }
        catch
        {
            throw new FormatException("AssetLoader.GetColorFromString: Failed to convert " + colorString + " to a valid color!");
        }
    }

    /// <summary>
    /// Creates a color based on a color string in the form "R,G,B". All values must be between 0 and 255.
    /// Returns a given default color if parsing the given string fails.
    /// </summary>
    /// <param name="colorString">The color string.</param>
    /// <param name="defaultColor">The default color to return if parsing the string fails.</param>
    /// <returns>A XNA Color struct.</returns>
    public static Color GetColorFromString(string colorString, Color defaultColor)
    {
        try
        {
            return GetColorFromString(colorString);
        }
        catch
        {
            return defaultColor;
        }
    }

    /// <summary>
    /// Creates a color based on a color string in the form "R,G,B,A". All values must be between 0 and 255.
    /// </summary>
    public static Color GetRGBAColorFromString(string colorString)
    {
        try
        {
            string[] colorArray = colorString.Split(',');
            var color = new Color(
                Convert.ToByte(colorArray[0], CultureInfo.InvariantCulture),
                Convert.ToByte(colorArray[1], CultureInfo.InvariantCulture),
                Convert.ToByte(colorArray[2], CultureInfo.InvariantCulture),
                Convert.ToByte(colorArray[3], CultureInfo.InvariantCulture));
            return color;
        }
        catch
        {
            throw new FormatException("AssetLoader.GetRGBAColorFromString: Failed to convert " + colorString + " to a valid color!");
        }
    }

    public static async Task<Texture2D> LoadTextureFromUrl(string url)
    {
        try
        {
            using var httpClient = new HttpClient();
            var imageData = await httpClient.GetByteArrayAsync(url);

            using var ms = new MemoryStream(imageData);
            var texture = Texture2D.FromStream(graphicsDevice, ms);
            PremultiplyAlpha(texture);
            return texture;
        }
        catch (Exception ex)
        {
            Logger.Log("AssetLoader.LoadTextureFromUrl: 下载或创建纹理失败！" + ex.Message);
            return null;
        }
    }
}
