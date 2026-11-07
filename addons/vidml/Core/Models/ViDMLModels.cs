using System.Collections.Generic;
using System.Xml.Serialization;

namespace ViDML.Core.Models;

[XmlRoot("vidml")]
public class VidmlRoot
{
    [XmlElement("head")]
    public HeadDto Head { get; set; }

    [XmlElement("body")]
    public BodyDto Body { get; set; }
}

public class HeadDto
{
    [XmlElement("import")]
    public List<ImportDto> Imports { get; set; } = new();

    [XmlElement("style")]
    public List<StyleDto> Styles { get; set; } = new();

    [XmlElement("text-box")]
    public List<TextBoxDto> TextBoxes { get; set; } = new();

    [XmlElement("sprite")]
    public List<SpriteDto> Sprites { get; set; } = new();
    [XmlElement("soundboard")]
    public List<SoundboardDto> Soundboards { get; set; } = new();

    [XmlElement("sprite-collection")]
    public List<SpriteCollectionDto> SpriteCollections { get; set; } = new();
}

public class ImportDto
{
    [XmlAttribute("src")] public string Src { get; set; }
}

public class StyleDto : TextSettingsDto
{
    [XmlAttribute("name")] public string Name { get; set; }
}

public class TextBoxDto
{
    [XmlAttribute("name")] public string Name { get; set; }
    [XmlAttribute("src")] public string Src { get; set; }
    [XmlAttribute("height")] public int Height { get; set; }
    [XmlAttribute("width")] public int Width { get; set; }
}

public class SpriteDto
{
    [XmlAttribute("name")] public string Name { get; set; }
    [XmlAttribute("src")] public string Src { get; set; }
    [XmlAttribute("width")] public int Width { get; set; }
    [XmlAttribute("height")] public int Height { get; set; }
}
public class SpriteCollectionDto
{
    [XmlAttribute("name")] public string Name { get; set; }
    [XmlAttribute("default")] public string DefaultSprite { get; set; }

    [XmlElement("sprite")]
    public List<SpriteDto> Sprites { get; set; } = new();
}

// Base settings class mapped to all text-settings & phrase-settings attributes
public class TextSettingsDto
{
    [XmlAttribute("font")] public string Font { get; set; }
    [XmlAttribute("font-size")] public int FontSize { get; set; } = 14;
    [XmlAttribute("color")] public string Color { get; set; } = "#ffffff";
    [XmlAttribute("opacity")] public double Opacity { get; set; } = 1.0;
    [XmlAttribute("char-speed")] public double CharSpeed { get; set; } = 30;
    [XmlAttribute("instant-characters")] public bool InstantCharacters { get; set; } = false;
    [XmlAttribute("max-line-amount")] public int MaxLineAmount { get; set; } = 3;
    [XmlAttribute("new-line-symbol")] public string NewLineSymbol { get; set; } = "";
    [XmlAttribute("text-direction")] public string TextDirection { get; set; } = "left-to-right";
    [XmlAttribute("text-align")] public string TextAlign { get; set; } = "";
    [XmlAttribute("auto-move-next")] public bool AutoMoveNext { get; set; } = false;
    [XmlAttribute("auto-move-next-delay")] public double AutoMoveNextDelay { get; set; } = 0;
    [XmlAttribute("allow-manual-skip")] public bool AllowManualSkip { get; set; } = true;
    [XmlAttribute("allow-manual-move-next")] public bool AllowManualMoveNext { get; set; } = true;
    [XmlAttribute("dialogue-box")] public string DialogueBox { get; set; }
    [XmlAttribute("sprite")] public string Sprite { get; set; }
    [XmlAttribute("sprite-collection")] public string SpriteCollection { get; set; }
    [XmlAttribute("emotion")] public string Emotion { get; set; }
    [XmlAttribute("sprite-pos-x")] public int SpritePosX { get; set; }
    [XmlAttribute("sprite-pos-y")] public int SpritePosY { get; set; }
    [XmlAttribute("text-pos-x")] public int TextPosX { get; set; }
    [XmlAttribute("text-pos-y")] public int TextPosY { get; set; }
    [XmlAttribute("text-width")] public int TextWidth{ get; set; }
    [XmlAttribute("text-height")] public int TextHeight{ get; set; }
    [XmlAttribute("style")] public string Style { get; set; }
    [XmlAttribute("vertical-spacing")] public int VerticalSpacing { get; set; }
    [XmlAttribute("soundboard")] public string Soundboard { get; set; }
}

public class PhraseDto : TextSettingsDto
{
    [XmlAttribute("uid")] public string Uid { get; set; }
    [XmlText] public string TextContent { get; set; }
}

public class ConvDto : TextSettingsDto
{
    [XmlAttribute("uid")] public string Uid { get; set; }

    [XmlElement("phrase")]
    public List<PhraseDto> Phrases { get; set; } = new();
}

public class GroupDto : TextSettingsDto
{
    [XmlElement("group")]
    public List<GroupDto> Groups { get; set; } = new();

    [XmlElement("phrase")]
    public List<PhraseDto> Phrases { get; set; } = new();

    [XmlElement("conv")]
    public List<ConvDto> Convs { get; set; } = new();
}

public class SoundDto
{
    [XmlAttribute("src")] public string Src { get; set; }
    [XmlAttribute("volume")] public float Volume { get; set; } = 0.0f; // dB offset
}

public class SoundboardDto
{
    [XmlAttribute("name")] public string Name { get; set; }
    [XmlAttribute("order")] public string Order { get; set; } = "sequence"; // "sequence" or "random"
    [XmlAttribute("ignore-spaces")] public bool IgnoreSpaces { get; set; } = true;

    [XmlElement("sound")]
    public List<SoundDto> Sounds { get; set; } = new();
}

public class BodyDto : GroupDto
{
    // Inherits all group-type behaviors
}
