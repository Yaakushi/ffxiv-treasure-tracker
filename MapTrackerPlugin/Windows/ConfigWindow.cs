using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
namespace MapTrackerPlugin.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;

    public ConfigWindow(Plugin plugin) : base(
        "A Wonderful Configuration Window",
        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.Size = new Vector2(232, 75);
        this.SizeCondition = ImGuiCond.Always;

        this.Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        // can't ref a property, so use a local copy
        var configValue = this.Configuration.shouldRemoveOnDig;
        if (ImGui.Checkbox("Remove map links on dig.", ref configValue))
        {
            this.Configuration.shouldRemoveOnDig = configValue;
            // can save immediately on change, if you don't want to provide a "Save and Close" button
            this.Configuration.Save();
        }

        ImGui.SameLine();
        ImGui.TextDisabled("(?)");
        Utils.HelpMarker("Track when players use the \"Dig\" action in the game, and remove their previous map link as soon as it happens. It can be helpful to keep track of actual maps pending in a zone, but it can also lead to links incorrectly being removed if someone use the Dig action spuriously, as there's currently no way to check whether or not the dig action resulted in a chest.");
    }
}
