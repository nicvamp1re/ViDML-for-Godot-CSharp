using System.Collections.Generic;
using Godot;
using ViDML.Core.Models;

namespace ViDML.Core;

public class ResolvedPhraseData
{
    public string Text { get; set; }
    public string Font { get; set; }
    public string FontB { get; set; }
    public string FontI { get; set; }
    public string FontBi { get; set; }
    public string FontMono { get; set; }

    public int FontSizeB { get; set; }
    public int FontSizeI { get; set; }
    public int FontSizeBi { get; set; }
    public int FontSizeMono { get; set; }
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

    public string SpriteName { get; set; }
    public string SpriteCollectionName { get; set; }
    public string EmotionName { get; set; }
    public string SoundboardName { get; set; }
    public string SilentCharacters { get; set; }

    public string SpritePath { get; set; }
    public int SpriteWidth { get; set; }
    public int SpriteHeight { get; set; }
    public int SpritePosX { get; set; }
    public int SpritePosY { get; set; }
    public int TextPosX { get; set; }
    public int TextPosY { get; set; }
    public int VerticalSpacing { get; set; }
    public int TextWidth { get; set; }
    public int TextHeight { get; set; }
    
    public SoundboardDto ResolvedSoundboard { get; set; }
    public Dictionary<string, SfxDto> HeadSfxRegistry { get; set; }
}

public static class StyleResolver
{
    public static ResolvedPhraseData Resolve(
        PhraseDto phrase, 
        ConvDto conv, 
        Dictionary<string, StyleDto> headStyles,
        Dictionary<string, TextBoxDto> textBoxes,
        Dictionary<string, SpriteDto> sprites,
        Dictionary<string, SpriteCollectionDto> spriteCollections,
        Dictionary<string, SoundboardDto> soundboards,
        Dictionary<string, SfxDto> sfxRegistry)
    {
        string rawText = phrase?.TextContent ?? string.Empty;
        string cleanedText = CleanXmlText(rawText);

        var resolved = new ResolvedPhraseData
        {
            Text = cleanedText,
            FontSize = 14,
            Color = "#ffffff",
            Opacity = 1.0,
            CharSpeed = 30,
            InstantCharacters = false,
            MaxLineAmount = 3,
            NewLineSymbol = "",
            TextDirection = "left-to-right",
            TextAlign = "start",
            AutoMoveNext = false,
            AutoMoveNextDelay = 0,
            AllowManualSkip = true,
            AllowManualMoveNext = true,
            VerticalSpacing = 0,
            TextHeight = 300,
            TextWidth = 1200,
            HeadSfxRegistry = sfxRegistry,
            SilentCharacters = "\n.,!?-+*/:;<>@ "
        };

        // 1. Resolve style hierarchy (Phrase > Conv > Innermost Group -> Outermost Group)
        string effectiveStyleName = phrase?.Style;
        if (string.IsNullOrEmpty(effectiveStyleName)) effectiveStyleName = conv?.Style;
        if (string.IsNullOrEmpty(effectiveStyleName) && conv?.ParentGroups != null)
        {
            for (int i = conv.ParentGroups.Count - 1; i >= 0; i--)
            {
                if (!string.IsNullOrEmpty(conv.ParentGroups[i].Style))
                {
                    effectiveStyleName = conv.ParentGroups[i].Style;
                    break;
                }
            }
        }

        // 2. Apply Head Style base
        if (!string.IsNullOrEmpty(effectiveStyleName) && headStyles != null && headStyles.TryGetValue(effectiveStyleName, out var headStyle))
        {
            ApplySettings(resolved, headStyle);
        }

        // 3. Apply Group overrides from outermost to innermost
        if (conv?.ParentGroups != null)
        {
            foreach (var group in conv.ParentGroups)
            {
                ApplySettings(resolved, group);
            }
        }

        // 4. Apply Conversation overrides
        if (conv != null)
        {
            ApplySettings(resolved, conv);
        }

        // 5. Apply direct Phrase overrides (highest precedence)
        if (phrase != null)
        {
            ApplySettings(resolved, phrase);
        }

        // 6. Resolve Asset Links (TextBox, Sprite, Soundboard)
        if (!string.IsNullOrEmpty(resolved.DialogueBoxName) && textBoxes != null && textBoxes.TryGetValue(resolved.DialogueBoxName, out var textBox))
        {
            resolved.DialogueBoxPath = textBox.Src;
            resolved.BoxWidth = textBox.Width;
            resolved.BoxHeight = textBox.Height;
        }

        SpriteDto resolvedSprite = null;
        if (!string.IsNullOrEmpty(resolved.SpriteName) && sprites != null)
        {
            sprites.TryGetValue(resolved.SpriteName, out resolvedSprite);
        }
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

        if (!string.IsNullOrEmpty(resolved.SoundboardName) && soundboards != null && soundboards.TryGetValue(resolved.SoundboardName, out var sb))
        {
            resolved.ResolvedSoundboard = sb;
        }
        if (resolved.FontSizeB <= 0) resolved.FontSizeB = resolved.FontSize;
        if (resolved.FontSizeI <= 0) resolved.FontSizeI = resolved.FontSize;
        if (resolved.FontSizeBi <= 0) resolved.FontSizeBi = resolved.FontSize;
        if (resolved.FontSizeMono <= 0) resolved.FontSizeMono = resolved.FontSize;

        return resolved;
    }

    private static string CleanXmlText(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
        if (lines.Length <= 1) return text.Trim();

        int minIndent = int.MaxValue;
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            int indent = 0;
            while (indent < line.Length && char.IsWhiteSpace(line[indent])) indent++;
            if (indent < minIndent) minIndent = indent;
        }

        var cleanedLines = new List<string>();
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) { cleanedLines.Add(string.Empty); continue; }
            if (minIndent > 0 && line.Length >= minIndent) cleanedLines.Add(line.Substring(minIndent));
            else cleanedLines.Add(line.TrimStart());
        }

        return string.Join("\n", cleanedLines).Trim();
    }

    private static void ApplySettings(ResolvedPhraseData target, TextSettingsDto source)
    {
        if (source == null) return;

        if (!string.IsNullOrEmpty(source.Font)) target.Font = source.Font;
        if (!string.IsNullOrEmpty(source.FontB)) target.FontB = source.FontB;
        if (!string.IsNullOrEmpty(source.FontI)) target.FontI = source.FontI;
        if (!string.IsNullOrEmpty(source.FontBi)) target.FontBi = source.FontBi;
        if (!string.IsNullOrEmpty(source.FontMono)) target.FontMono = source.FontMono;
        if (source.FontSize != 14) target.FontSize = source.FontSize;
        if (source.FontSizeB > 0) target.FontSizeB = source.FontSizeB;
        if (source.FontSizeI > 0) target.FontSizeI = source.FontSizeI;
        if (source.FontSizeBi > 0) target.FontSizeBi = source.FontSizeBi;
        if (source.FontSizeMono > 0) target.FontSizeMono = source.FontSizeMono;
        if (!string.IsNullOrEmpty(source.Color) && source.Color != "#ffffff") target.Color = source.Color;
        if (source.Opacity != 1.0) target.Opacity = source.Opacity;
        if (source.CharSpeed != 30) target.CharSpeed = source.CharSpeed;
        if (source.InstantCharacters) target.InstantCharacters = source.InstantCharacters;
        if (source.MaxLineAmount != 3) target.MaxLineAmount = source.MaxLineAmount;
        if (source.NewLineSymbol != "") target.NewLineSymbol = source.NewLineSymbol;
        if (!string.IsNullOrEmpty(source.TextDirection)) target.TextDirection = source.TextDirection;
        if (!string.IsNullOrEmpty(source.TextAlign)) 
        {target.TextAlign = source.TextAlign; GD.Print(source.TextAlign);}
        if (source.AutoMoveNext) target.AutoMoveNext = source.AutoMoveNext;
        if (source.AutoMoveNextDelay > 0) target.AutoMoveNextDelay = source.AutoMoveNextDelay;
        if (!source.AllowManualSkip) target.AllowManualSkip = false;
        if (!source.AllowManualMoveNext) target.AllowManualMoveNext = false;
        if (!string.IsNullOrEmpty(source.DialogueBox)) target.DialogueBoxName = source.DialogueBox;

        if (!string.IsNullOrEmpty(source.Sprite)) target.SpriteName = source.Sprite;
        if (!string.IsNullOrEmpty(source.SpriteCollection)) target.SpriteCollectionName = source.SpriteCollection;
        if (!string.IsNullOrEmpty(source.Emotion)) target.EmotionName = source.Emotion;
        if (!string.IsNullOrEmpty(source.Soundboard)) target.SoundboardName = source.Soundboard;

        if (source.SpritePosX != 0) target.SpritePosX = source.SpritePosX;
        if (source.SpritePosY != 0) target.SpritePosY = source.SpritePosY;
        if (source.TextPosX != 0) target.TextPosX = source.TextPosX;
        if (source.TextPosY != 0) target.TextPosY = source.TextPosY;
        if (source.VerticalSpacing != 0) target.VerticalSpacing = source.VerticalSpacing;
        if (source.TextWidth != 0) target.TextWidth = source.TextWidth;
        if (source.TextHeight != 0) target.TextHeight = source.TextHeight;
        if (source.SilentCharacters != "\n.,!?-+*/:;<>") target.SilentCharacters = source.SilentCharacters;
    }
}