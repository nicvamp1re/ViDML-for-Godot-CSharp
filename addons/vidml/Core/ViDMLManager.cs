using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using ViDML.Core.Models;
using ViDML.UI;

namespace ViDML.Core;

public partial class ViDMLManager : Node
{
    public static ViDMLManager Instance { get; private set; }

    [Export] public string BaseDialoguePath { get; set; } = "res://dialogue";
    [Export] public string CurrentLanguage { get; set; } = "en";
    [Export] public PackedScene DefaultDialogBoxScene { get; set; }

    private readonly Dictionary<string, ConvDto> _conversations = new();
    private readonly Dictionary<string, StyleDto> _styles = new();
    private readonly Dictionary<string, TextBoxDto> _textBoxes = new();
    private readonly Dictionary<string, SpriteDto> _sprites = new();
    private readonly Dictionary<string, SpriteCollectionDto> _spriteCollections = new();

    private ViDMLDialogBox _activeDialogBox;

    public override void _EnterTree()
    {
        if (Instance != null && Instance != this)
        {
            QueueFree();
            return;
        }
        Instance = this;
    }

    public void Initialize(string language = null)
    {
        if (!string.IsNullOrEmpty(language))
            CurrentLanguage = language;

        _conversations.Clear();
        _styles.Clear();
        _textBoxes.Clear();
        _sprites.Clear();
        _spriteCollections.Clear();

        string langPath = $"{BaseDialoguePath}/{CurrentLanguage}";

        if (!DirAccess.DirExistsAbsolute(langPath))
        {
            GD.PrintErr($"[ViDML] Language directory does not exist: {langPath}");
            return;
        }

        using var dir = DirAccess.Open(langPath);
        if (dir != null)
        {
            dir.ListDirBegin();
            string fileName = dir.GetNext();
            while (!string.IsNullOrEmpty(fileName))
            {
                if (!dir.CurrentIsDir() && fileName.EndsWith(".vidml"))
                {
                    string fullPath = $"{langPath}/{fileName}";
                    ViDMLParser.LoadAndRegister(
                        fullPath, 
                        _conversations, 
                        _styles, 
                        _textBoxes, 
                        _sprites, 
                        _spriteCollections
                    );
                }
                fileName = dir.GetNext();
            }
        }

        GD.Print($"[ViDML] Initialized '{CurrentLanguage}': Loaded {_conversations.Count} conversations, {_spriteCollections.Count} collections.");
    }

    public async Task PlayConv(
        string convUid, 
        Vector2? position = null, 
        Node2D targetNode = null, 
        Vector2 offset = default)
    {
        if (!_conversations.TryGetValue(convUid, out var conv))
        {
            GD.PrintErr($"[ViDML] Conversation UID '{convUid}' not found.");
            return;
        }

        EnsureDialogBoxInstance();

        if (_activeDialogBox == null) return;

        if (targetNode != null)
        {
            _activeDialogBox.AttachToNode(targetNode, offset);
        }
        else if (position.HasValue)
        {
            _activeDialogBox.SetPosition(position.Value);
        }

        foreach (var phrase in conv.Phrases)
        {
            ResolvedPhraseData resolvedData = StyleResolver.Resolve(
                phrase, 
                conv, 
                _styles, 
                _textBoxes, 
                _sprites, 
                _spriteCollections
            );
            await _activeDialogBox.PlayPhraseAsync(resolvedData);
        }

        _activeDialogBox.Visible = false;
    }

    private void EnsureDialogBoxInstance()
    {
        if (_activeDialogBox != null && GodotObject.IsInstanceValid(_activeDialogBox))
            return;

        _activeDialogBox = GetTree().Root.FindChild("ViDMLDialogBox", true, false) as ViDMLDialogBox;

        if (_activeDialogBox == null && DefaultDialogBoxScene != null)
        {
            var instance = DefaultDialogBoxScene.Instantiate<ViDMLDialogBox>();
            GetTree().Root.AddChild(instance);
            _activeDialogBox = instance;
        }
    }
}