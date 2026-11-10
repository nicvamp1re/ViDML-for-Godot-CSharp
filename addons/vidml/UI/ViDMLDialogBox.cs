using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
    [Export] private AudioStreamPlayer _audioPlayer;      // Typing soundboard player
    [Export] private AudioStreamPlayer _sfxAudioPlayer;   // One-shot SFX player

    private bool _isPrinting = false;
    private bool _skipRequested = false;
    private int _soundIndex = 0;
    private readonly RandomNumberGenerator _rng = new();
    private TaskCompletionSource _dialogueTcs;
    
    // Robust RegEx pattern matching [pause="0.5"], [pause="0.5" sfx="boom"], [sfx name="boom"], etc.
    private readonly RegEx _bbcodeRegex = new();
    private static readonly System.Text.RegularExpressions.Regex _inlineTagRegex = new(
        @"\[\s*(pause|sfx)([^\]]*?)\]", 
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    public override void _Ready()
    {
        _bbcodeRegex.Compile("\\[.*?\\]");
        if (_textLabel != null)
        {
            _textLabel.BbcodeEnabled = true;
        }

        if (_sfxAudioPlayer == null)
        {
            _sfxAudioPlayer = new AudioStreamPlayer();
            AddChild(_sfxAudioPlayer);
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

    private void ApplyTextStyles(ResolvedPhraseData data)
    {
        if (_textLabel == null) return;

        Color textColor = Colors.White;
        if (!string.IsNullOrEmpty(data.Color))
        {
            textColor = Color.FromString(data.Color, Colors.White);
        }
        textColor.A = (float)data.Opacity;
        _textLabel.AddThemeColorOverride("default_color", textColor);

        if (data.FontSize > 0)
        {
            _textLabel.AddThemeFontSizeOverride("normal_font_size", data.FontSize);
        }

        if (!string.IsNullOrEmpty(data.Font))
        {
            var fontResource = TextureLoader.LoadFontFromFile(data.Font);
            if (fontResource != null)
            {
                _textLabel.AddThemeFontOverride("normal_font", fontResource);
            }
        }

        _textLabel.HorizontalAlignment = data.TextAlign?.ToLower() switch
        {
            "center" => HorizontalAlignment.Center,
            "right" or "end" => HorizontalAlignment.Right,
            "fill" or "justify" => HorizontalAlignment.Fill,
            _ => HorizontalAlignment.Left
        };
        
        _textLabel.AddThemeConstantOverride("line_separation", data.VerticalSpacing);
    }

    public async Task PlayPhraseAsync(ResolvedPhraseData data)
    {
        Visible = true;
        _skipRequested = false;

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

        ApplyTextStyles(data);

        if (_textLabel != null)
        {
            _textLabel.Position = new Vector2(data.TextPosX, data.TextPosY);
            _textLabel.CustomMinimumSize = new Vector2(data.TextWidth, data.TextHeight);
            _textLabel.Size = _textLabel.CustomMinimumSize;
        }

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

        if (_textLabel != null)
        {
            // Parse and strip custom [pause] and [sfx] tags, building the event timeline
            string rawText = data.Text ?? string.Empty;
            string cleanBbcodeText = ParseInlineEvents(rawText, data.HeadSfxRegistry, out var eventTimeline);

            _textLabel.Text = cleanBbcodeText;
            _textLabel.VisibleCharacters = 0;

            string plainText = _bbcodeRegex.Sub(_textLabel.Text, "", true);
            int totalCharacters = plainText.Length;
            if (totalCharacters == 0) totalCharacters = 1;

            _isPrinting = true;

            if (data.InstantCharacters)
            {
                _textLabel.VisibleCharacters = -1;
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

                        int charIndex = _textLabel.VisibleCharacters - 1;

                        // 1. Trigger any inline SFX or Pause events attached to this character index
                        if (eventTimeline.TryGetValue(charIndex, out var eventsAtChar))
                        {
                            foreach (var inlineEvt in eventsAtChar)
                            {
                                PlayOneShotSfx(inlineEvt);

                                if (inlineEvt.PauseDuration > 0 && !_skipRequested)
                                {
                                    await ToSignal(GetTree().CreateTimer(inlineEvt.PauseDuration), SceneTreeTimer.SignalName.Timeout);
                                }
                            }
                        }

                        // 2. Play standard typing soundboard for visible character
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

    /// <summary>
    /// Strips [pause] and [sfx] tags from text and populates a character-indexed event timeline.
    /// </summary>
    private string ParseInlineEvents(
        string rawText, 
        Dictionary<string, SfxDto> sfxRegistry, 
        out Dictionary<int, List<InlineEvent>> timeline)
    {
        timeline = new Dictionary<int, List<InlineEvent>>();
        if (string.IsNullOrEmpty(rawText)) return string.Empty;

        var matches = _inlineTagRegex.Matches(rawText);
        if (matches.Count == 0) return rawText;

        var sb = new System.Text.StringBuilder();
        int lastIndex = 0;
        int currentPlainIndex = 0;

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            string textChunk = rawText.Substring(lastIndex, match.Index - lastIndex);
            sb.Append(textChunk);

            string plainChunk = _bbcodeRegex.Sub(textChunk, "", true);
            currentPlainIndex += plainChunk.Length;

            string body = match.Groups[2].Value.Trim();
            var evt = new InlineEvent { CharacterIndex = currentPlainIndex };

            // Handle shorthand assignment like [pause="0.5"] where body starts with '='
            if (body.StartsWith("="))
            {
                string val = body.Substring(1).Trim('"', '\'', ' ');
                if (double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double pTime))
                {
                    evt.PauseDuration = pTime;
                }
            }

            // Parse key-value attributes (e.g., sfx="boom", volume="-2.0", pause="0.5", time="0.5")
            var attrMatches = System.Text.RegularExpressions.Regex.Matches(
                body, 
                @"(?<key>\w+)\s*=\s*(?:""(?<val>[^""]*)""|'(?<val>[^']*)'|(?<val>[^\s\]]+))"
            );
            foreach (System.Text.RegularExpressions.Match attrMatch in attrMatches)
            {
                string key = attrMatch.Groups["key"].Value.ToLower();
                string val = attrMatch.Groups["val"].Value;

                if (key == "pause" || key == "time")
                {
                    double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double pauseTime);
                    evt.PauseDuration = pauseTime;
                }
                else if (key == "sfx" || key == "name")
                {
                    evt.SfxName = val;
                }
                else if (key == "src")
                {
                    evt.SfxSrc = val;
                }
                else if (key == "volume")
                {
                    float.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float vol);
                    evt.SfxVolume = vol;
                }
            }

            // Resolve SFX definition from head registry if referenced by name
            if (!string.IsNullOrEmpty(evt.SfxName) && sfxRegistry != null && sfxRegistry.TryGetValue(evt.SfxName, out var registeredSfx))
            {
                evt.SfxSrc = registeredSfx.Src;
                if (evt.SfxVolume == 0.0f) evt.SfxVolume = registeredSfx.Volume;
            }

            if (evt.PauseDuration > 0 || !string.IsNullOrEmpty(evt.SfxSrc))
            {
                if (!timeline.ContainsKey(currentPlainIndex))
                {
                    timeline[currentPlainIndex] = new List<InlineEvent>();
                }
                timeline[currentPlainIndex].Add(evt);
            }

            lastIndex = match.Index + match.Length;
        }

        sb.Append(rawText.Substring(lastIndex));
        return sb.ToString();
    }

    private void PlayOneShotSfx(InlineEvent evt)
    {
        if (string.IsNullOrEmpty(evt.SfxSrc)) return;

        var stream = AudioLoader.LoadAudioFromFile(evt.SfxSrc);
        if (stream != null && _sfxAudioPlayer != null)
        {
            _sfxAudioPlayer.Stream = stream;
            _sfxAudioPlayer.VolumeDb = evt.SfxVolume;
            _sfxAudioPlayer.Play();
        }
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