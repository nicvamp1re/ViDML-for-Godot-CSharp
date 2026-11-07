using System;
using System.Threading.Tasks;
using Godot;
using ViDML.Core;
using ViDML.Core.Models;

namespace ViDML.UI;

public partial class ViDMLDialogBox : Control
{
    [Export] private NinePatchRect _backgroundBox;
    [Export] private TextureRect _portraitSprite;
    [Export] private RichTextLabel _textLabel;
    [Export] private AudioStreamPlayer _audioPlayer;

    private bool _isPrinting = false;
    private bool _skipRequested = false;
    private int _soundIndex = 0;
    private readonly RandomNumberGenerator _rng = new();
    private TaskCompletionSource _dialogueTcs;
    private readonly RegEx _bbcodeRegex = new();

    public override void _Ready()
    {
        // Compile regex to strip BBCode tags so typing audio syncs with visible text
        _bbcodeRegex.Compile("\\[.*?\\]");
        if (_textLabel != null)
        {
            _textLabel.BbcodeEnabled = true;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_accept") && _isPrinting)
        {
            _skipRequested = true;
        }
        else if (@event.IsActionPressed("ui_accept") && !_isPrinting && _dialogueTcs != null)
        {
            _dialogueTcs.TrySetResult();
        }
    }
    
    private void ApplyTextStyles(ResolvedPhraseData data)
    {
        if (_textLabel == null) return;

        // 1. Color & Opacity
        Color textColor = Colors.White;
        if (!string.IsNullOrEmpty(data.Color))
        {
            textColor = Color.FromString(data.Color, Colors.White);
        }
        textColor.A = (float)data.Opacity;
        _textLabel.AddThemeColorOverride("default_color", textColor);

        // 2. Font Size
        if (data.FontSize > 0)
        {
            _textLabel.AddThemeFontSizeOverride("normal_font_size", data.FontSize);
        }

        // 3. Custom Font Loading
        if (!string.IsNullOrEmpty(data.Font))
        {
            var fontResource = TextureLoader.LoadFontFromFile(data.Font);
            if (fontResource != null)
            {
                _textLabel.AddThemeFontOverride("normal_font", fontResource);
            }
        }

        // 4. Text Alignment
        _textLabel.HorizontalAlignment = data.TextAlign?.ToLower() switch
        {
            "center" => HorizontalAlignment.Center,
            "right" or "end" => HorizontalAlignment.Right,
            "fill" or "justify" => HorizontalAlignment.Fill,
            _ => HorizontalAlignment.Left
        };
        
        _textLabel.AddThemeConstantOverride("line_separation", data.VerticalSpacing);
    }

    /// <summary>
    /// Plays a resolved phrase, managing typewriter speed, BBCode, audio, and UI elements.
    /// </summary>
    public async Task PlayPhraseAsync(ResolvedPhraseData data)
    {
        Visible = true;
        _skipRequested = false;

        // 1. Configure Background Box Size & Texture
        if (data.BoxWidth > 0 && data.BoxHeight > 0)
        {
            CustomMinimumSize = new Vector2(data.BoxWidth, data.BoxHeight);
            Size = CustomMinimumSize;
        }

        if (!string.IsNullOrEmpty(data.DialogueBoxPath))
        {
            var texture = TextureLoader.LoadTextureFromFile(data.DialogueBoxPath);
            if (texture != null && _backgroundBox != null)
            {
                _backgroundBox.Texture = texture;
            }
        }

        // 2. Apply Text Styles, Position & Size
        ApplyTextStyles(data);

        if (_textLabel != null)
        {
            _textLabel.Position = new Vector2(data.TextPosX, data.TextPosY);
            _textLabel.CustomMinimumSize = new Vector2(data.TextWidth, data.TextHeight);
            _textLabel.Size = _textLabel.CustomMinimumSize;
        }

        // 3. Configure Portrait Sprite & Position
        if (_portraitSprite != null)
        {
            if (!string.IsNullOrEmpty(data.SpritePath))
            {
                var spriteTex = TextureLoader.LoadTextureFromFile(data.SpritePath);
                _portraitSprite.Texture = spriteTex;
                _portraitSprite.Visible = spriteTex != null;

                if (data.SpriteWidth > 0 && data.SpriteHeight > 0)
                {
                    _portraitSprite.CustomMinimumSize = new Vector2(data.SpriteWidth, data.SpriteHeight);
                }

                _portraitSprite.Position = new Vector2(data.SpritePosX, data.SpritePosY);
            }
            else
            {
                _portraitSprite.Visible = false;
            }
        }

        // 4. Set Text & Run Typewriter Effect with BBCode & Audio Support
        if (_textLabel != null)
        {
            _textLabel.Text = data.Text ?? string.Empty;
            _textLabel.VisibleCharacters = 0;

            // Strip BBCode tags to find true visible character length for the sound trigger
            string plainText = _bbcodeRegex.Sub(_textLabel.Text, "", true);
            int totalCharacters = plainText.Length;
            if (totalCharacters == 0) totalCharacters = 1;

            _isPrinting = true;

            if (data.InstantCharacters)
            {
                _textLabel.VisibleCharacters = -1; // -1 reveals all characters in Godot RichTextLabel
            }
            else
            {
                double charInterval = data.CharSpeed > 0 ? (1.0 / data.CharSpeed) : 0.03;
                double timer = 0.0;

                while (_textLabel.VisibleCharacters < totalCharacters)
                {
                    if (_skipRequested && data.AllowManualSkip)
                    {
                        _textLabel.VisibleCharacters = -1;
                        break;
                    }

                    timer += GetProcessDeltaTime();
                    if (timer >= charInterval)
                    {
                        timer = 0;
                        int prevVisible = _textLabel.VisibleCharacters;
                        _textLabel.VisibleCharacters += 1;

                        // Play typing sound for the newly revealed plain-text character
                        if (_textLabel.VisibleCharacters > prevVisible && prevVisible >= 0 && prevVisible < plainText.Length)
                        {
                            char revealedChar = plainText[prevVisible];
                            PlayTypingSound(revealedChar, data.ResolvedSoundboard);
                        }
                    }

                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                }
            }

            _isPrinting = false;
        }

        // 5. Handle Auto-Advance or Wait for Player Input
        _dialogueTcs = new TaskCompletionSource();

        if (data.AutoMoveNext)
        {
            if (data.AutoMoveNextDelay > 0)
            {
                await ToSignal(GetTree().CreateTimer(data.AutoMoveNextDelay), SceneTreeTimer.SignalName.Timeout);
            }
        }
        else if (data.AllowManualMoveNext)
        {
            await _dialogueTcs.Task;
        }
        else
        {
            await ToSignal(GetTree().CreateTimer(1.5), SceneTreeTimer.SignalName.Timeout);
        }

        _dialogueTcs = null;
    }

    public void SetPosition(Vector2 globalPosition)
    {
        GlobalPosition = globalPosition;
    }

    public void AttachToNode(Node2D targetNode, Vector2 offset = default)
    {
        if (targetNode == null) return;
        
        Vector2 screenPos = targetNode.GetGlobalTransformWithCanvas().Origin;
        
        if (offset == Vector2.Zero)
        {
            offset = new Vector2(-Size.X / 2f, -Size.Y - 20f);
        }
        
        GlobalPosition = screenPos + offset;
    }

    private void PlayTypingSound(char currentChar, SoundboardDto soundboard)
    {
        if (soundboard == null || soundboard.Sounds == null || soundboard.Sounds.Count == 0)
            return;

        if (soundboard.IgnoreSpaces)
        {
            if (char.IsWhiteSpace(currentChar) || char.IsPunctuation(currentChar))
                return;
        }

        SoundDto targetSound = null;
        if (soundboard.Order?.ToLower() == "random")
        {
            int randomIndex = _rng.RandiRange(0, soundboard.Sounds.Count - 1);
            targetSound = soundboard.Sounds[randomIndex];
        }
        else
        {
            targetSound = soundboard.Sounds[_soundIndex % soundboard.Sounds.Count];
            _soundIndex++;
        }

        if (targetSound != null && !string.IsNullOrEmpty(targetSound.Src))
        {
            var stream = AudioLoader.LoadAudioFromFile(targetSound.Src);
            if (stream != null)
            {
                if (_audioPlayer == null)
                {
                    _audioPlayer = new AudioStreamPlayer();
                    AddChild(_audioPlayer);
                }

                _audioPlayer.Stream = stream;
                _audioPlayer.VolumeDb = targetSound.Volume;
                _audioPlayer.Play();
            }
        }
    }
}