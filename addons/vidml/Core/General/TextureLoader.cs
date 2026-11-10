using Godot;

namespace ViDML.Core;

public static class TextureLoader
{
    /// <summary>
    /// Loads a Texture2D from a file path. 
    /// Supports "res://" and "user://" paths as well as absolute system paths.
    /// </summary>
    public static Texture2D LoadTextureFromFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return null;

        // 1. Internal Godot project resource (e.g., "res://assets/dialog_box.png")
        if (filePath.StartsWith("res://") || filePath.StartsWith("user://"))
        {
            if (ResourceLoader.Exists(filePath))
            {
                return ResourceLoader.Load<Texture2D>(filePath);
            }
            GD.PrintErr($"[ViDML] Resource not found at path: {filePath}");
            return null;
        }

        // 2. External file on the OS file system (e.g., absolute disk paths)
        if (Godot.FileAccess.FileExists(filePath))
        {
            var image = Image.LoadFromFile(filePath);
            if (image != null)
            {
                return ImageTexture.CreateFromImage(image);
            }
            GD.PrintErr($"[ViDML] Failed to decode image file at: {filePath}");
            return null;
        }

        GD.PrintErr($"[ViDML] File does not exist at path: {filePath}");
        return null;
    }
    /// <summary>
/// Loads a Font (FontFile / SystemFont) from a file path (.ttf, .otf, .tres).
/// </summary>
    public static Font LoadFontFromFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return null;

        if (filePath.StartsWith("res://") || filePath.StartsWith("user://"))
        {
            if (ResourceLoader.Exists(filePath))
            {
                return ResourceLoader.Load<Font>(filePath);
            }
            GD.PrintErr($"[ViDML] Font resource not found at path: {filePath}");
            return null;
        }

        // External disk font file
        if (Godot.FileAccess.FileExists(filePath))
        {
            var fontFile = new FontFile();
            var err = fontFile.LoadDynamicFont(filePath);
            if (err == Error.Ok)
            {
                return fontFile;
            }
            GD.PrintErr($"[ViDML] Failed to load external font file at: {filePath}, error: {err}");
        }

        return null;
    }
}