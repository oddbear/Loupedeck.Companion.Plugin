using Loupedeck.CompanionPlugin.Services;

namespace Loupedeck.CompanionPlugin
{
    public class CompanionPlugin : Plugin
    {
        public override bool UsesApplicationApiOnly => true;
        public override bool HasNoApplication => true;

        public CompanionClient Client;

        public CompanionPlugin()
        {
            Client = new CompanionClient(this);
        }

        public override void Load()
        {
            this.LoadPluginIcons();

            Client.Start();
        }

        public override void Unload()
        {
            Client.Dispose();
        }
        
        public override void RunCommand(string commandName, string parameter)
        {
        }

        public override void ApplyAdjustment(string adjustmentName, string parameter, int diff)
        {
        }

        private void LoadPluginIcons()
        {
            this.Info.Icon16x16 = EmbeddedResources.ReadImage("Loupedeck.CompanionPlugin.Resources.Icons.Icon-16.png");
            this.Info.Icon32x32 = EmbeddedResources.ReadImage("Loupedeck.CompanionPlugin.Resources.Icons.Icon-32.png");
            this.Info.Icon48x48 = EmbeddedResources.ReadImage("Loupedeck.CompanionPlugin.Resources.Icons.Icon-48.png");
            this.Info.Icon256x256 = EmbeddedResources.ReadImage("Loupedeck.CompanionPlugin.Resources.Icons.Icon-256.png");
        }

        internal void ConnectedStatus()
        {
            base.OnPluginStatusChanged(Loupedeck.PluginStatus.Normal,
                "Connected",
                "https://github.com/oddbear/Loupedeck.Companion.Plugin",
                "Companion Repository");
        }

        internal void NotConnectedStatus()
        {
            base.OnPluginStatusChanged(Loupedeck.PluginStatus.Warning,
                "Could not connect to companion, is it running on this machine, and 'Use Elgato Plugin for StreamDeck access' is enabled?",
                "https://github.com/oddbear/Loupedeck.Companion.Plugin",
                "Companion Repository");
        }

        internal void ErrorStatus(string message)
        {
            base.OnPluginStatusChanged(Loupedeck.PluginStatus.Error,
                $"Error: {message}",
                "https://github.com/oddbear/Loupedeck.Companion.Plugin",
                "Plugin GitHub page");
        }
    }
}
