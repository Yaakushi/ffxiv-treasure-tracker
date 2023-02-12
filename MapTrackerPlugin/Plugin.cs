using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using MapTrackerPlugin.Windows;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using System;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Linq;

namespace MapTrackerPlugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Sample Plugin";
        private const string CommandName = "/mhh";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }

        [PluginService] private ChatGui Chat { get; set; }

        [PluginService] private PartyList PartyList { get; set; }

        [PluginService] private ClientState ClientState { get; set; }

        [PluginService] private GameGui GameGui { get; set; }

        /*[PluginService] private PlayerCharacter PlayerCharacter { get; set; }*/

        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("MapTrackerPlugin");

        private ConfigWindow ConfigWindow { get; init; }
        private MainWindow MainWindow { get; init; }
        private PlayerCharacter PlayerCharacter { get; set; }

        private Dictionary<String, MapLinkPayload> MapLinks { get; } = new Dictionary<String, MapLinkPayload>();

        public List<String> PartyMembersList { get; set; } = new List<String>();
        public List<MapLinkPayload?> PartyMemberLinks { get; set; } = new List<MapLinkPayload?>();
        public List<bool> IsOnTheSameAreaAsThePlayer { get; set; } = new List<bool>();

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            ConfigWindow = new ConfigWindow(this);
            MainWindow = new MainWindow(this);
            
            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);

            this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Display the treasure map tracker window."
            });

            this.MainWindow.OnRefreshed += OnWindowRefreshRequest;

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            this.Chat!.ChatMessage += onChatMessage;
            this.ClientState!.Login += onLogin;
            this.ClientState.TerritoryChanged += onTerritoryChanged;

            if(ClientState.LocalPlayer != null)
            {
                this.PlayerCharacter = ClientState.LocalPlayer;
            }
        }

        private void OnWindowRefreshRequest(object sender, EventArgs args)
        {
            RegenerateMapLinkTables();
        }

        private void onTerritoryChanged(object? sender, ushort e)
        {
            RegenerateMapLinkTables();
        }

        private void onLogin(object? sender, EventArgs e)
        {
            ClientState? loginClientState = sender as ClientState;
            if(loginClientState != null && loginClientState.LocalPlayer != null)
            {
                this.PlayerCharacter = loginClientState.LocalPlayer;
            }
        }

        private void onChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            // 2091 = Chat type for dig logs.
            if ((ushort)type == 2091 && Configuration.shouldRemoveOnDig)
            {
                // Combat log -- look for dig.
                Dalamud.Logging.PluginLog.Information("Sender: {sender}\n\n--------------\n\nMessage: {message}", sender.ToJson(), message.ToJson());
                TextPayload? usePayload = message.Payloads.Last(payload => payload.Type == PayloadType.RawText) as TextPayload;

                // TODO: Deal with i18n.
                if((!(usePayload!.Text?.Contains("use Dig")) ?? false) || (!(usePayload.Text?.Contains("uses Dig")) ?? false))
                {
                    // Non-dig log. We don't care.
                    return;
                }

                PlayerPayload? playerPayload = message.Payloads.First(payload => payload.Type == PayloadType.Player) as PlayerPayload;
                var key = (playerPayload != null) ? Utils.getKeyFrom(playerPayload) : Utils.getKeyFrom(this.PlayerCharacter);
                MapLinks.Remove(key);

                PruneNonPartyMembers();
                RegenerateMapLinkTables();

                return;
            }

            if (type != XivChatType.Party && type != XivChatType.CrossParty && type != XivChatType.CrossLinkShell2) return;

            MapLinkPayload? mapLinkPayload = message.Payloads.Find(payload => payload.Type == PayloadType.MapLink) as MapLinkPayload;
            if (mapLinkPayload == null) return;

            Dalamud.Logging.PluginLog.LogInformation("ok!");

            AddNewMapLink(sender, mapLinkPayload);
            PruneNonPartyMembers();
            RegenerateMapLinkTables();
        }

        private void AddNewMapLink(SeString sender, MapLinkPayload? mapLinkPayload)
        {
            PlayerPayload? senderPayload = sender.Payloads.Find(x => x.Type == PayloadType.Player) as PlayerPayload;

            if (senderPayload != null)
            {
                MapLinks[Utils.getKeyFrom(senderPayload)] = mapLinkPayload!;
            }

            else
            {
                // If there isn't a PlayerPayload (no link in the message), we just assume the player is the one sending the message.
                // This can technically break if another plugins send a message through party chat -- Like Sonar could technically do?
                MapLinks[Utils.getKeyFrom(this.PlayerCharacter)] = mapLinkPayload!;
            }
        }

        private void RegenerateMapLinkTables()
        {
            this.PartyMembersList.Clear();
            this.PartyMemberLinks.Clear();
            this.IsOnTheSameAreaAsThePlayer.Clear();

            foreach (PartyMember partyMember in PartyList)
            {
                if (this.PlayerCharacter.HomeWorld.Id == partyMember.World.Id)
                {
                    this.PartyMembersList.Add(partyMember.Name.ToString());
                }
                else
                {
                    this.PartyMembersList.Add(partyMember.Name + " (" + partyMember.World.GameData!.Name + ")");
                }

                String key = Utils.getKeyFrom(partyMember);
                if (MapLinks.ContainsKey(key))
                {
                    this.PartyMemberLinks.Add(MapLinks[key]);
                    this.IsOnTheSameAreaAsThePlayer.Add(MapLinks[key].Map.TerritoryType.Row == ClientState.TerritoryType);
                }
                else
                {
                    this.PartyMemberLinks.Add(null);
                    this.IsOnTheSameAreaAsThePlayer.Add(false);
                }
            }
        }

        private void PruneNonPartyMembers()
        {
            List<String> validKeys = new List<String>();
            List<String> keysToBeRemoved = new List<String>();

            // Prune party list.
            foreach (PartyMember partyMember in PartyList)
            {
                validKeys.Add(Utils.getKeyFrom(partyMember));
            }

            foreach (String key in MapLinks.Keys)
            {
                if (!validKeys.Contains(key))
                {
                    keysToBeRemoved.Add(key);
                }
            }

            foreach (String key in keysToBeRemoved)
            {
                MapLinks.Remove(key);
            }
        }

        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();
            
            ConfigWindow.Dispose();
            MainWindow.Dispose();
            
            this.CommandManager.RemoveHandler(CommandName);
            this.Chat.ChatMessage -= onChatMessage;
            this.ClientState.Login -= onLogin;
            this.ClientState.TerritoryChanged -= onTerritoryChanged;
        }

        public void OpenMapWithMapLink(MapLinkPayload mapLinkPayload)
        {
            GameGui.OpenMapWithMapLink(mapLinkPayload);
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            MainWindow.IsOpen = true;
            
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            ConfigWindow.IsOpen = true;
        }
    }
}
