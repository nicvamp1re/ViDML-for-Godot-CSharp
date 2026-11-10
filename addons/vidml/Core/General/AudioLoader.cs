using Godot;

namespace ViDML.Core;

public static class AudioLoader
{
    /// <summary>
    /// Loads an AudioStream (.wav, .ogg, .mp3) from project resources or external disk paths.
    /// </summary>
    public static AudioStream LoadAudioFromFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return null;

        // 1. Load from Godot project paths (res:// or user://)
        if (filePath.StartsWith("res://") || filePath.StartsWith("user://"))
        {
            if (ResourceLoader.Exists(filePath))
            {
                return ResourceLoader.Load<AudioStream>(filePath);
            }
            GD.PrintErr($"[ViDML] Audio resource not found at path: {filePath}");
            return null;
        }

        // 2. Load from external disk file paths outside res://
        if (Godot.FileAccess.FileExists(filePath))
        {
            if (filePath.EndsWith(".ogg", System.StringComparison.OrdinalIgnoreCase))
            {
                return AudioStreamOggVorbis.LoadFromFile(filePath);
            }
            else if (filePath.EndsWith(".wav", System.StringComparison.OrdinalIgnoreCase))
            {
                // For external WAV files on disk
                var audioStreamWav = new AudioStreamWav();
                // Godot's audio stream wav loading can be handled via resource loading or file buffers,
                // but keeping assets inside res:// is recommended for automatic import compression.
                GD.PrintErr("[ViDML] External raw .wav files from disk require res:// paths or custom parsing. Recommend using res:// paths.");
            }
        }

        return null;
    }
}