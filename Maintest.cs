using Godot;
using System;
using System.Threading.Tasks.Dataflow;
using ViDML.Core;

public partial class Maintest : Node
{
    public override async void _Ready()
    {
        // 1. Initialize ViDML (scans res://dialogue/en by default)
       await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        // 1. Initialize ViDML
        ViDMLManager.Instance.Initialize("en");
        
        

        // 2. Play conversation
        await ViDMLManager.Instance.PlayConv("intro_conversation", new Vector2(100, 100));
        
        GD.Print("Dialogue sequence completed!");
    }
}
