using System.Collections.Generic;
using Loupedeck.CompanionPlugin.Extensions;
using Loupedeck.CompanionPlugin.Responses;
using Loupedeck.CompanionPlugin.Services;
using SkiaSharp;

namespace Loupedeck.CompanionPlugin.Folders
{
    class DynamicFolder : PluginDynamicFolder
    {
        private readonly SKBitmap[] _buttons;

        private CompanionPlugin _plugin;
        private CompanionClient Client => _plugin?.Client;

        public DynamicFolder()
        {
            _buttons = new SKBitmap[34];
            for (var i = 0; i < 34; i++)
                _buttons[i] = new SKBitmap(72, 72);

            this.DisplayName = "Companion";
            this.GroupName = "Dynamic Folder";
        }

        public override PluginDynamicFolderNavigation GetNavigationArea(DeviceType deviceType)
        {
            return PluginDynamicFolderNavigation.EncoderArea;
        }

        public override bool Load()
        {
            _plugin = (CompanionPlugin) base.Plugin;
            Client.FillImageResponse += PluginOnFillImageResponse;

            return true;
        }

        public override bool Unload()
        {
            Client.FillImageResponse -= PluginOnFillImageResponse;

            return true;
        }

        private void PluginOnFillImageResponse(object sender, ResponseFillImage fillImage)
        {
            try
            {
                if (fillImage.Page != null || fillImage.Bank != null)
                    return;

                var bitmap = _buttons[fillImage.KeyIndex];
                bitmap.DrawBuffer(fillImage.Data.Data);

                var actionParameter = fillImage.KeyIndex.ToString();
                base.CommandImageChanged(actionParameter);
            }
            catch
            {
                //
            }
        }

        public override bool ProcessTouchEvent(string actionParameter, DeviceTouchEvent touchEvent)
        {
            if (!int.TryParse(actionParameter, out var index))
                return false;

            //TODO: How to get haptic feedback?
            switch (touchEvent.EventType)
            {
                case DeviceTouchEventType.TouchDown:
                    Client.SendCommand("keydown", new { keyIndex = index });
                    break;
                case DeviceTouchEventType.TouchUp:
                    Client.SendCommand("keyup", new { keyIndex = index });
                    break;
            }

            //It is supposed to be true... but then I will loose haptic feedback...
            return false;
        }

        public override BitmapImage GetCommandImage(string actionParameter, PluginImageSize imageSize)
        {
            if (!int.TryParse(actionParameter, out var index))
                return base.GetCommandImage(actionParameter, imageSize);

            if (!Client.Connected)
                return BitmapExtensions.DrawDisconnected();

            var image = _buttons[index];
            return image.BitmapToBitmapImage();
        }

        public override IEnumerable<string> GetButtonPressActionNames(DeviceType deviceType)
        {
            return new[]
            {
                this.CreateCommandName("0"),
                this.CreateCommandName("1"),
                this.CreateCommandName("2"),
                this.CreateCommandName("3"),

                this.CreateCommandName("8"),
                this.CreateCommandName("9"),
                this.CreateCommandName("10"),
                this.CreateCommandName("11"),

                this.CreateCommandName("16"),
                this.CreateCommandName("17"),
                this.CreateCommandName("18"),
                this.CreateCommandName("19")
            };
        }
    }
}
