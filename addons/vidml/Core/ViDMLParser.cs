using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Godot;
using ViDML.Core.Models;

namespace ViDML.Core;

public static class ViDMLParser
{
    public static void LoadAndRegister(
        string filePath, 
        Dictionary<string, ConvDto> conversationRegistry,
        Dictionary<string, StyleDto> styleRegistry,
        Dictionary<string, TextBoxDto> textBoxRegistry,
        Dictionary<string, SpriteDto> spriteRegistry,
        Dictionary<string, SpriteCollectionDto> spriteCollectionRegistry,
        Dictionary<string, SoundboardDto> soundboardRegistry,
        Dictionary<string, SfxDto> sfxRegistry)
        
    {
        if (!Godot.FileAccess.FileExists(filePath))
        {
            GD.PrintErr($"[ViDML] File not found: {filePath}");
            return;
        }

        string xmlContent;
        using (var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Read))
        {
            xmlContent = file.GetAsText();
        }

        try
        {
            var serializer = new XmlSerializer(typeof(VidmlRoot));
            using var reader = new StringReader(xmlContent);
            var root = (VidmlRoot)serializer.Deserialize(reader);

            if (root == null) return;

            if (root.Head != null)
            {
                if (root.Head.Styles != null)
                {
                    foreach (var style in root.Head.Styles)
                    {
                        if (!string.IsNullOrEmpty(style.Name)) 
                            styleRegistry[style.Name] = style;
                    }
                }
                if (root.Head.TextBoxes != null)
                {
                    foreach (var box in root.Head.TextBoxes)
                    {
                        if (!string.IsNullOrEmpty(box.Name)) 
                            textBoxRegistry[box.Name] = box;
                    }
                }
                if (root.Head.Sprites != null)
                {
                    foreach (var sprite in root.Head.Sprites)
                    {
                        if (!string.IsNullOrEmpty(sprite.Name)) 
                            spriteRegistry[sprite.Name] = sprite;
                    }
                }
                if (root.Head.SpriteCollections != null)
                {
                    foreach (var collection in root.Head.SpriteCollections)
                    {
                        if (!string.IsNullOrEmpty(collection.Name)) 
                            spriteCollectionRegistry[collection.Name] = collection;
                    }
                }
                if (root.Head.Soundboards != null)
                {
                    foreach (var sb in root.Head.Soundboards)
                    {
                        if (!string.IsNullOrEmpty(sb.Name)) 
                            soundboardRegistry[sb.Name] = sb;
                    }
                }
                if (root.Head.SoundEffects != null)
                {
                    foreach (var sfx in root.Head.SoundEffects)
                    {
                        if (!string.IsNullOrEmpty(sfx.Name)) 
                            sfxRegistry[sfx.Name] = sfx;
                    }
                }
            }

            if (root.Body != null)
            {
                RegisterGroup(root.Body, conversationRegistry);
            }
            
            GD.Print($"[ViDML] Successfully parsed and indexed: {filePath}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[ViDML] Failed to parse XML at {filePath}. Error: {ex.Message}");
        }
    }

    private static void RegisterGroup(GroupDto group, Dictionary<string, ConvDto> registry)
    {
        if (group.Convs != null)
        {
            foreach (var conv in group.Convs)
            {
                if (!string.IsNullOrEmpty(conv.Uid))
                {
                    if (registry.ContainsKey(conv.Uid))
                    {
                        GD.PrintErr($"[ViDML] Duplicate conversation UID detected across files: '{conv.Uid}'");
                    }
                    else
                    {
                        registry[conv.Uid] = conv;
                    }
                }
            }
        }

        if (group.Groups != null)
        {
            foreach (var subGroup in group.Groups)
            {
                RegisterGroup(subGroup, registry);
            }
        }
    }
}