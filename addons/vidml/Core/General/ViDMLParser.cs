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
            GD.PrintErr($"[ViDML] File not found at path: {filePath}");
            return;
        }

        string xmlContent = Godot.FileAccess.GetFileAsString(filePath);
        if (string.IsNullOrWhiteSpace(xmlContent))
        {
            GD.PrintErr($"[ViDML] File is empty at path: {filePath}");
            return;
        }

        VidmlRootDto root;
        try
        {
            var serializer = new XmlSerializer(typeof(VidmlRootDto));
            using var reader = new StringReader(xmlContent);
            root = (VidmlRootDto)serializer.Deserialize(reader);
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[ViDML] Failed to parse XML at {filePath}: {ex.Message}");
            return;
        }

        if (root == null) return;

        // 1. Register Head Assets
        if (root.Head != null)
        {
            if (root.Head.TextBoxList != null)
                foreach (var box in root.Head.TextBoxList)
                    if (!string.IsNullOrEmpty(box.Name)) textBoxRegistry[box.Name] = box;

            if (root.Head.SpriteList != null)
                foreach (var spr in root.Head.SpriteList)
                    if (!string.IsNullOrEmpty(spr.Name)) spriteRegistry[spr.Name] = spr;

            if (root.Head.SpriteCollectionList != null)
                foreach (var col in root.Head.SpriteCollectionList)
                    if (!string.IsNullOrEmpty(col.Name)) spriteCollectionRegistry[col.Name] = col;

            if (root.Head.SoundboardList != null)
                foreach (var sb in root.Head.SoundboardList)
                    if (!string.IsNullOrEmpty(sb.Name)) soundboardRegistry[sb.Name] = sb;

            if (root.Head.SoundEffects != null)
                foreach (var sfx in root.Head.SoundEffects)
                    if (!string.IsNullOrEmpty(sfx.Name)) sfxRegistry[sfx.Name] = sfx;

            if (root.Head.StyleList != null)
                foreach (var sty in root.Head.StyleList)
                    if (!string.IsNullOrEmpty(sty.Name)) styleRegistry[sty.Name] = sty;
        }

        // 2. Register Body Conversations & Recursive Groups
        if (root.Body != null)
        {
            RegisterBodyElements(root.Body.Conversations, root.Body.Groups, new List<GroupDto>(), conversationRegistry);
        }
    }

    private static void RegisterBodyElements(
        List<ConvDto> convs,
        List<GroupDto> groups,
        List<GroupDto> ancestorGroups,
        Dictionary<string, ConvDto> conversationRegistry)
    {
        if (convs != null)
        {
            foreach (var conv in convs)
            {
                if (!string.IsNullOrEmpty(conv.Uid))
                {
                    conv.ParentGroups = new List<GroupDto>(ancestorGroups);
                    conversationRegistry[conv.Uid] = conv;
                }
            }
        }

        if (groups != null)
        {
            foreach (var group in groups)
            {
                var nextAncestors = new List<GroupDto>(ancestorGroups) { group };
                RegisterBodyElements(group.Conversations, group.Groups, nextAncestors, conversationRegistry);
            }
        }
    }
}