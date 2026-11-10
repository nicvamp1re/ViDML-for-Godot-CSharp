namespace ViDML.Core.Models;

public class InlineEvent
{
    public int CharacterIndex { get; set; }
    public double PauseDuration { get; set; } = 0.0;
    public string SfxName { get; set; }
    public string SfxSrc { get; set; }
    public float SfxVolume { get; set; } = 0.0f;
}