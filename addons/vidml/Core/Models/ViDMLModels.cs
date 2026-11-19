using System.Collections.Generic;
using System.Xml.Serialization;

namespace ViDML.Core.Models;

[XmlRoot("vidml")]
public class VidmlRootDto
{
    [XmlElement("head")]
    public HeadDto Head { get; set; }

    [XmlElement("body")]
    public BodyDto Body { get; set; }
}

public class HeadDto
{
    [XmlElement("text-box")]
    public List<TextBoxDto> TextBoxList { get; set; } = new();

    [XmlElement("sprite")]
    public List<SpriteDto> SpriteList { get; set; } = new();

    [XmlElement("sprite-collection")]
    public List<SpriteCollectionDto> SpriteCollectionList { get; set; } = new();

    [XmlElement("soundboard")]
    public List<SoundboardDto> SoundboardList { get; set; } = new();

    [XmlElement("sfx")]
    public List<SfxDto> SoundEffects { get; set; } = new();

    [XmlElement("style")]
    public List<StyleDto> StyleList { get; set; } = new();
}

public class BodyDto
{
    [XmlElement("conv")]
    public List<ConvDto> Conversations { get; set; } = new();

    [XmlElement("group")]
    public List<GroupDto> Groups { get; set; } = new();
}

public class GroupDto : TextSettingsDto
{
    [XmlAttribute("style")] public string Style { get; set; }

    [XmlElement("conv")]
    public List<ConvDto> Conversations { get; set; } = new();

    [XmlElement("group")]
    public List<GroupDto> Groups { get; set; } = new();
}

public class ConvDto : TextSettingsDto
{
    [XmlAttribute("uid")] public string Uid { get; set; }
    [XmlAttribute("style")] public string Style { get; set; }

    [XmlElement("phrase")]
    public List<PhraseDto> Phrases { get; set; } = new();

    [XmlIgnore]
    public List<GroupDto> ParentGroups { get; set; } = new();
}

public class PhraseDto : TextSettingsDto
{
    [XmlAttribute("style")] public string Style { get; set; }
    [XmlText] public string TextContent { get; set; }
}

public class TextSettingsDto
{
    [XmlAttribute("font")] public string Font { get; set; }
    [XmlAttribute("font-b")] public string FontB { get; set; }
    [XmlAttribute("font-i")] public string FontI { get; set; }
    [XmlAttribute("font-bi")] public string FontBi { get; set; }
    [XmlAttribute("font-mono")] public string FontMono { get; set; }

    [XmlAttribute("font-size")] public int FontSize { get; set; } = 14;
    [XmlAttribute("font-size-b")] public int FontSizeB { get; set; } = 0;
    [XmlAttribute("font-size-i")] public int FontSizeI { get; set; } = 0;
    [XmlAttribute("font-size-bi")] public int FontSizeBi { get; set; } = 0;
    [XmlAttribute("font-size-mono")] public int FontSizeMono { get; set; } = 0;
    [XmlAttribute("color")] public string Color { get; set; } = "#ffffff";
    [XmlAttribute("opacity")] public double Opacity { get; set; } = 1.0;
    [XmlAttribute("char-speed")] public double CharSpeed { get; set; } = 30;
    [XmlAttribute("instant-characters")] public bool InstantCharacters { get; set; } = false;
    [XmlAttribute("max-line-amount")] public int MaxLineAmount { get; set; } = 3;
    [XmlAttribute("new-line-symbol")] public string NewLineSymbol { get; set; } = "";
    [XmlAttribute("text-direction")] public string TextDirection { get; set; } = "left-to-right";
    [XmlAttribute("text-align")] public string TextAlign { get; set; } = "start";
    [XmlAttribute("auto-move-next")] public bool AutoMoveNext { get; set; } = false;
    [XmlAttribute("auto-move-next-delay")] public double AutoMoveNextDelay { get; set; } = 0;
    [XmlAttribute("allow-manual-skip")] public bool AllowManualSkip { get; set; } = true;
    [XmlAttribute("allow-manual-move-next")] public bool AllowManualMoveNext { get; set; } = true;

    [XmlAttribute("dialogue-box")] public string DialogueBox { get; set; }
    [XmlAttribute("sprite")] public string Sprite { get; set; }
    [XmlAttribute("sprite-collection")] public string SpriteCollection { get; set; }
    [XmlAttribute("emotion")] public string Emotion { get; set; }
    [XmlAttribute("soundboard")] public string Soundboard { get; set; }

    [XmlAttribute("sprite-pos-x")] public int SpritePosX { get; set; }
    [XmlAttribute("sprite-pos-y")] public int SpritePosY { get; set; }
    [XmlAttribute("text-pos-x")] public int TextPosX { get; set; }
    [XmlAttribute("text-pos-y")] public int TextPosY { get; set; }
    [XmlAttribute("vertical-spacing")] public int VerticalSpacing { get; set; } = 0;
    [XmlAttribute("text-width")] public int TextWidth { get; set; }
    [XmlAttribute("text-height")] public int TextHeight { get; set; }
    [XmlAttribute("silent-characters")] public string SilentCharacters { get; set; } = "\n.,!?-+*/:;<>()";
}

public class TextBoxDto
{
    [XmlAttribute("name")] public string Name { get; set; }
    [XmlAttribute("src")] public string Src { get; set; }
    [XmlAttribute("width")] public int Width { get; set; }
    [XmlAttribute("height")] public int Height { get; set; }
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

public class SoundboardDto
{
    [XmlAttribute("name")] public string Name { get; set; }
    [XmlAttribute("order")] public string Order { get; set; }
    [XmlAttribute("ignore-spaces")] public bool IgnoreSpaces { get; set; }

    [XmlElement("sound")]
    public List<SoundDto> Sounds { get; set; } = new();
}

public class SoundDto
{
    [XmlAttribute("src")] public string Src { get; set; }
    [XmlAttribute("volume")] public float Volume { get; set; }
}

public class SfxDto
{
    [XmlAttribute("name")] public string Name { get; set; }
    [XmlAttribute("src")] public string Src { get; set; }
    [XmlAttribute("volume")] public float Volume { get; set; } = 0.0f;
}

public class StyleDto : TextSettingsDto
{
    [XmlAttribute("name")] public string Name { get; set; }
}