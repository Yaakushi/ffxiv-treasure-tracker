using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ImGuiNET;
using Microsoft.VisualBasic;

namespace MapTrackerPlugin {

    static class Utils
    {
        public static string getKeyFrom(PlayerPayload payload)
        {
            return payload.PlayerName + "|" + payload.World.Name;
        }

        public static string getKeyFrom(PlayerCharacter playerCharacter)
        {
            return playerCharacter.Name + "|" + playerCharacter.HomeWorld.GameData!.Name;
        }
        public static string getKeyFrom(PartyMember partyMember)
        {
            return partyMember.Name + "|" + partyMember.World.GameData!.Name;
        }

        public static void HelpMarker(string text)
        {
            if (!ImGui.IsItemHovered()) return;
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 20f);
            ImGui.TextUnformatted(text);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }

    }

}
