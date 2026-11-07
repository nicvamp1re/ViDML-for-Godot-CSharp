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
    private RandomNumberGenerator _rng = new();    private TaskCompletionSource _dialogueTcs;

    public override void _Input(InputEvent @event)
    {
        // Allow manual skip when player presses the accept action (e.g., Space, Enter, Return)
        if (@event.IsActionPressed("ui_accept") && _isPrinting)
        {
            _skipRequested = true;
        }
        else if (@event.IsActionPressed("ui_accept") && !_isPrinting && _dialogueTcs != null)
        {
            // Complete current phrase waiting on input
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
            // Safely parses HTML hex codes (e.g., "#00ffcc") or named colors with a fallback
            textColor = Color.FromString(data.Color, Colors.White);
        }
        
        // Apply opacity to the alpha channel
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
        //_backgroundBox.CustomMaximumSize = new Vector2(data.BoxWidth, data.BoxHeight);

        // 4. Text Alignment Mapping
        _textLabel.HorizontalAlignment = data.TextAlign?.ToLower() switch
        {
            "center" => HorizontalAlignment.Center,
            "right" or "end" => HorizontalAlignment.Right,
            "fill" or "justify" => HorizontalAlignment.Fill,
            _ => HorizontalAlignment.Left // "start" or default maps to Left
        };
        _textLabel.AddThemeConstantOverride("line_separation", data.VerticalSpacing);
    }
    /// <summary>
    /// Plays a resolved phrase, managing typewriter speed and UI elements.
    /// </summary>
    public async Task PlayPhraseAsync(ResolvedPhraseData data)
    {
        Visible = true;
        _skipRequested = false;
        // 1. Apply Dynamic Width and Height from ViDML <text-box> definition
        if (data.BoxWidth > 0 && data.BoxHeight > 0)
        {
            CustomMinimumSize = new Vector2(data.BoxWidth, data.BoxHeight);
            Size = CustomMinimumSize;
        }

        // 2. Configure Background Texture
        if (!string.IsNullOrEmpty(data.DialogueBoxPath))
        {
            var texture = TextureLoader.LoadTextureFromFile(data.DialogueBoxPath);
            if (texture != null && _backgroundBox != null)
            {
                _backgroundBox.Texture = texture;
            }
        }
        ApplyTextStyles(data);
        _textLabel.Size = new Vector2(data.TextWidth, data.TextHeight);

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

            // Apply custom X/Y coordinates defined in style/phrase
            _portraitSprite.Position = new Vector2(data.SpritePosX, data.SpritePosY);
        }
        else
        {
            _portraitSprite.Visible = false;
        }
    }

    // 4. Configure Text Position & Size
    if (_textLabel != null)
    {
        _textLabel.Position = new Vector2(data.TextPosX, data.TextPosY);
        
        // Optional: If you want text width/height to be bound inside the box bounds, 
        // you can adjust its size or anchors here as well.
    }
        // 1. Configure Background Box Texture & Dimensions if provided
        if (!string.IsNullOrEmpty(data.DialogueBoxPath))
        {
            var texture = TextureLoader.LoadTextureFromFile(data.DialogueBoxPath);
            if (texture != null && _backgroundBox != null)
            {
                _backgroundBox.Texture = texture;
            }
        }

        // 3. Set Text & BBCode
        if (_textLabel != null)
        {
            _textLabel.Text = data.Text ?? string.Empty;
            _textLabel.VisibleCharacters = 0;

            int totalCharacters = _textLabel.GetTotalCharacterCount();
            if (totalCharacters == 0) totalCharacters = 1;

            _isPrinting = true;

            if (data.InstantCharacters)
            {
                _textLabel.VisibleCharacters = totalCharacters;
            }
            else
            {
                // Typewriter loop
                double charInterval = 1.0 / data.CharSpeed;
                double timer = 0.0;

                while (_textLabel.VisibleCharacters < totalCharacters)
                {
                    if (_skipRequested && data.AllowManualSkip)
                    {
                        _textLabel.VisibleCharacters = totalCharacters;
                        break;
                    }

                    timer += get_process_delta_time();
                    if (timer >= charInterval)
                    {
                        timer = 0;
                        _textLabel.VisibleCharacters += 1;
                    }

                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                }
            }

            _isPrinting = false;
        }

        // 4. Handle Auto-Advance or Wait for Player Input
        _dialogueTcs = new TaskCompletionSource();

        if (data.AutoMoveNext)
        {
            if (data.AutoMoveNextDelay > 0)
            {
                await ToSignal(GetTree().CreateTimer(data.AutoMoveNextDelay), SceneTreeTimer.SignalName.Timeout);
            }
            else
            {
                // Instant auto-advance if delay is 0
                await Task.CompletedTask;
            }
        }
        else if (data.AllowManualMoveNext)
        {
            // Wait until user presses ui_accept again
            await _dialogueTcs.Task;
        }
        else
        {
            // Fallback short pause if manual movement is disallowed
            await ToSignal(GetTree().CreateTimer(1.5), SceneTreeTimer.SignalName.Timeout);
        }

        _dialogueTcs = null;
    }
    /// <summary>
    /// Positions the dialogue box in screen space.
    /// </summary>
    public void SetPosition(Vector2 globalPosition)
    {
        GlobalPosition = globalPosition;
    }

    /// <summary>
    /// Anchors the dialogue box centered relative to a target Node2D (e.g. an NPC),
    /// offset vertically by a given height.
    /// </summary>
    public void AttachToNode(Node2D targetNode, Vector2 offset = default)
    {
        if (targetNode == null) return;
        
        // Convert target node's global transform to screen position
        Vector2 screenPos = targetNode.GetGlobalTransformWithCanvas().Origin;
        
        // Default offset: center box horizontally over node and place it slightly above
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
            // Use the dedicated AudioLoader class here
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

    private double get_process_delta_time() => (float)GetProcessDeltaTime();
}