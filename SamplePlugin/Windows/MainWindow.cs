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

namespace SamplePlugin.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;

    public MainWindow(Plugin plugin) : base(
        "Map Hitchhiker", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            /*MinimumSize = new Vector2(375, 330),*/
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        /*this.GoatImage = goatImage;*/
        this.Plugin = plugin;
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

        for(int i = 0; i < Plugin.PartyMembersList.Count; i++)
        {
            ImGui.TableNextRow();

            if (Plugin.IsOnTheSameAreaAsThePlayer[i])
            {
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGui.GetColorU32(new Vector4(0.7f, 0.0f, 0.0f, 0.5f)));
            }

            ImGui.TableSetColumnIndex(0);
            ImGui.Text(Plugin.PartyMembersList[i]);

            ImGui.TableSetColumnIndex(1);

            MapLinkPayload? currentMapLink = Plugin.PartyMemberLinks[i];
            if (currentMapLink != null)
            {
                if (ImGui.Button(currentMapLink.PlaceName))
                {
                    Plugin.OpenMapWithMapLink(currentMapLink);
                }
            }
            else
            {
                ImGui.Text("No map link!");
            }
        }

        ImGui.EndTable();
    }
}
