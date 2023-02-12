using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using ImGuiScene;
using Lumina;
using Lumina.Excel.GeneratedSheets;

namespace MapTrackerPlugin.Windows;

public delegate void RefreshEventHandler(object sender, EventArgs args);

public class MainWindow : Window, IDisposable
{
    private Plugin plugin;

    public event RefreshEventHandler OnRefreshed;

    public MainWindow(Plugin plugin) : base(
        "Map Hitchhiker", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize)
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            /*MinimumSize = new Vector2(375, 330),*/
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        /*this.GoatImage = goatImage;*/
        this.plugin = plugin;
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        ImGui.BeginTable("playerMapsLinks", 2, ImGuiTableFlags.Resizable);
        ImGui.TableSetupColumn("Player");
        ImGui.TableSetupColumn("Map Location");
        ImGui.TableHeadersRow();

        for(int i = 0; i < plugin.PartyMembersList.Count; i++)
        {
            ImGui.TableNextRow();

            if (plugin.IsOnTheSameAreaAsThePlayer[i])
            {
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGui.GetColorU32(new Vector4(0.7f, 0.0f, 0.0f, 0.5f)));
            }

            ImGui.TableSetColumnIndex(0);
            ImGui.Text(plugin.PartyMembersList[i]);

            ImGui.TableSetColumnIndex(1);

            MapLinkPayload? currentMapLink = plugin.PartyMemberLinks[i];
            if (currentMapLink != null)
            {
                if (ImGui.Button(currentMapLink.PlaceName))
                {
                    plugin.OpenMapWithMapLink(currentMapLink);
                }
            }
            else
            {
                ImGui.Text("No map link!");
            }
        }

        ImGui.EndTable();

        if(ImGui.Button("Refresh")) OnRefreshed?.Invoke(this, EventArgs.Empty);
    }
}
