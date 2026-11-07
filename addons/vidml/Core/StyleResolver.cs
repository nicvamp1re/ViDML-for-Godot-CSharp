using System.Collections.Generic;
using ViDML.Core.Models;

namespace ViDML.Core;

public class ResolvedPhraseData
{
    public string Text { get; set; }
    public string Font { get; set; }
    public int FontSize { get; set; }
    public string Color { get; set; }
    public double Opacity { get; set; }
    public double CharSpeed { get; set; }
    public bool InstantCharacters { get; set; }
    public int MaxLineAmount { get; set; }
    public string NewLineSymbol { get; set; }
    public string TextDirection { get; set; }
    public string TextAlign { get; set; }
    public bool AutoMoveNext { get; set; }
    public double AutoMoveNextDelay { get; set; }
    public bool AllowManualSkip { get; set; }
    public bool AllowManualMoveNext { get; set; }
    
    public string DialogueBoxName { get; set; }
    public string DialogueBoxPath { get; set; }
    public int BoxWidth { get; set; }
    public int BoxHeight { get; set; }

    // Intermediate tracking fields for cascade resolution
    public string SpriteName { get; set; }
    public string SpriteCollectionName { get; set; }
    public string EmotionName { get; set; }

    // Final Resolved Sprite Output
    public string SpritePath { get; set; }
    public int SpriteWidth { get; set; }
    public int SpriteHeight { get; set; }
    public int SpritePosX { get; set; }
    public int SpritePosY { get; set; }
    public int TextPosX { get; set; }
    public int TextPosY { get; set; }
    public int VerticalSpacing { get; set; }
    public int TextWidth {get; set;}
    public int TextHeight {get; set;}
}

public static class StyleResolver
{
    public static ResolvedPhraseData Resolve(
    PhraseDto phrase, 
    ConvDto conv, 
    Dictionary<string, StyleDto> headStyles,
    Dictionary<string, TextBoxDto> textBoxes,
    Dictionary<string, SpriteDto> sprites,
    Dictionary<string, SpriteCollectionDto> spriteCollections)
{
    // Clean up XML formatting whitespace and newlines from text content
    string rawText = phrase?.TextContent ?? string.Empty;
    string cleanedText = CleanXmlText(rawText);

    var resolved = new ResolvedPhraseData
    {
        Text = cleanedText,
        Font = conv?.Font,
        // ... rest of property assignments ...
            FontSize = conv?.FontSize ?? 14,
            Color = conv?.Color ?? "#ffffff",
            Opacity = conv?.Opacity ?? 1.0,
            CharSpeed = conv?.CharSpeed ?? 30,
            InstantCharacters = conv?.InstantCharacters ?? false,
            MaxLineAmount = conv?.MaxLineAmount ?? 3,
            NewLineSymbol = conv?.NewLineSymbol ?? "",
            TextDirection = conv?.TextDirection ?? "left-to-right",
            TextAlign = conv?.TextAlign ?? "start",
            AutoMoveNext = conv?.AutoMoveNext ?? false,
            AutoMoveNextDelay = conv?.AutoMoveNextDelay ?? 0,
            AllowManualSkip = conv?.AllowManualSkip ?? true,
            AllowManualMoveNext = conv?.AllowManualMoveNext ?? true,
            DialogueBoxName = conv?.DialogueBox,
            VerticalSpacing = conv?.VerticalSpacing ?? 0,
            TextHeight = conv?.TextHeight ?? 300,
            TextWidth = conv?.TextWidth ?? 1000
        };

        // 1. Start with Conv-level settings if present
        if (conv != null)
        {
            ApplySettings(resolved, conv);
        }

        // 2. Cascade Head Style (if referenced by Phrase or Conv)
        string styleName = !string.IsNullOrEmpty(phrase.Style) ? phrase.Style : conv?.Style;
        if (!string.IsNullOrEmpty(styleName) && headStyles != null && headStyles.TryGetValue(styleName, out var headStyle))
        {
            ApplySettings(resolved, headStyle);
        }

        // 3. Apply direct Phrase overrides (highest precedence)
        ApplySettings(resolved, phrase);

        // 4. Resolve Box Dimensions & Path
        if (!string.IsNullOrEmpty(resolved.DialogueBoxName) && textBoxes != null && textBoxes.TryGetValue(resolved.DialogueBoxName, out var textBox))
        {
            resolved.DialogueBoxPath = textBox.Src;
            resolved.BoxWidth = textBox.Width;
            resolved.BoxHeight = textBox.Height;
        }

        // 5. Resolve Sprite via Direct Name OR Sprite-Collection + Emotion
        SpriteDto resolvedSprite = null;

        // Path A: Directly referenced single sprite tag
        if (!string.IsNullOrEmpty(resolved.SpriteName) && sprites != null)
        {
            sprites.TryGetValue(resolved.SpriteName, out resolvedSprite);
        }
        // Path B: Collection reference (inherited from Conv, Style, or Phrase)
        else if (!string.IsNullOrEmpty(resolved.SpriteCollectionName) && spriteCollections != null && spriteCollections.TryGetValue(resolved.SpriteCollectionName, out var collection))
        {
            string spriteToFind = !string.IsNullOrEmpty(resolved.EmotionName) ? resolved.EmotionName : collection.DefaultSprite;

            if (!string.IsNullOrEmpty(spriteToFind) && collection.Sprites != null)
            {
                resolvedSprite = collection.Sprites.Find(s => s.Name == spriteToFind);
            }
        }

        if (resolvedSprite != null)
        {
            resolved.SpritePath = resolvedSprite.Src;
            resolved.SpriteWidth = resolvedSprite.Width;
            resolved.SpriteHeight = resolvedSprite.Height;
        }

        return resolved;
    }
    /// <summary>
    /// Removes leading/trailing XML padding/newlines and normalizes indentation.
    /// </summary>
    private static string CleanXmlText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // Split into lines to remove uniform XML indentation if multiline
        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
        
        // If it's a single line or empty, just trim it
        if (lines.Length <= 1)
            return text.Trim();

        // Find minimum indentation of non-empty lines to strip excess XML spacing
        int minIndent = int.MaxValue;
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            int indent = 0;
            while (indent < line.Length && char.IsWhiteSpace(line[indent]))
            {
                indent++;
            }
            if (indent < minIndent) minIndent = indent;
        }

        var cleanedLines = new List<string>();
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                cleanedLines.Add(string.Empty);
                continue;
            }

            if (minIndent > 0 && line.Length >= minIndent)
            {
                cleanedLines.Add(line.Substring(minIndent));
            }
            else
            {
                cleanedLines.Add(line.TrimStart());
            }
        }

        // Rejoin and trim outer whitespace
        return string.Join("\n", cleanedLines).Trim();
    }

    private static void ApplySettings(ResolvedPhraseData target, TextSettingsDto source)
    {
        if (source == null) return;

        if (!string.IsNullOrEmpty(source.Font)) target.Font = source.Font;
        if (source.FontSize != 14) target.FontSize = source.FontSize;
        if (!string.IsNullOrEmpty(source.Color) && source.Color != "#ffffff") target.Color = source.Color;
        if (source.Opacity != 1.0) target.Opacity = source.Opacity;
        if (source.CharSpeed != 30) target.CharSpeed = source.CharSpeed;
        if (source.InstantCharacters) target.InstantCharacters = source.InstantCharacters;
        if (source.MaxLineAmount != 3) target.MaxLineAmount = source.MaxLineAmount;
        if (source.NewLineSymbol != "") target.NewLineSymbol = source.NewLineSymbol;
        if (!string.IsNullOrEmpty(source.TextDirection)) target.TextDirection = source.TextDirection;
        if (!string.IsNullOrEmpty(source.TextAlign)) target.TextAlign = source.TextAlign;
        if (source.AutoMoveNext) target.AutoMoveNext = source.AutoMoveNext;
        if (source.AutoMoveNextDelay > 0) target.AutoMoveNextDelay = source.AutoMoveNextDelay;
        if (!source.AllowManualSkip) target.AllowManualSkip = false;
        if (!source.AllowManualMoveNext) target.AllowManualMoveNext = false;
        if (!string.IsNullOrEmpty(source.DialogueBox)) target.DialogueBoxName = source.DialogueBox;

        // Carry forward sprite references inside ApplySettings
        if (!string.IsNullOrEmpty(source.Sprite)) target.SpriteName = source.Sprite;
        if (!string.IsNullOrEmpty(source.SpriteCollection)) target.SpriteCollectionName = source.SpriteCollection;
        if (!string.IsNullOrEmpty(source.Emotion)) target.EmotionName = source.Emotion;
        if (source.SpritePosX != 0) target.SpritePosX = source.SpritePosX;
        if (source.SpritePosY != 0) target.SpritePosY = source.SpritePosY;
        if (source.TextPosX != 0) target.TextPosX = source.TextPosX;
        if (source.TextPosY != 0) target.TextPosY = source.TextPosY;
        if (source.VerticalSpacing != 0) target.VerticalSpacing = source.VerticalSpacing;
        if (source.TextWidth != 0) target.TextWidth = source.TextWidth;
        if (source.TextHeight != 0) target.TextHeight = source.TextHeight;
    }
}