﻿using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using Loupedeck.CompanionPlugin.Extensions;
using Loupedeck.CompanionPlugin.Responses;
using WebSocketSharp;

namespace Loupedeck.CompanionPlugin.Commands
{
    class CompanionButton : PluginDynamicCommand
    {
        private CompanionPlugin _plugin;
        private WebSocket _client;
        
        private const int Dynamic = 0;

        private readonly Bitmap[,] _imageCache = new Bitmap[100, 32];

        public CompanionButton()
        {
            this.DisplayName = "Companion button";
            this.GroupName = "";
            this.Description = "Control anything in companion.";

            this.MakeProfileAction("tree");
        }

        protected override bool OnLoad()
        {
            _plugin = (CompanionPlugin)base.Plugin;
            _plugin.FillImageResponse += PluginOnFillImageResponse;

            _client = _plugin.Client;
            
            return true;
        }
        
        protected override bool OnUnload()
        {
            _plugin.FillImageResponse -= PluginOnFillImageResponse;

            return true;
        }

        private void PluginOnFillImageResponse(object sender, ResponseFillImage fillImage)
        {
            try
            {
                var page = fillImage.Page ?? Dynamic;
                var index = fillImage.Bank ?? fillImage.KeyIndex;

                if (_imageCache[page, index] is null)
                    _imageCache[page, index] = new Bitmap(72, 72);

                var bitmap = _imageCache[page, index];

                bitmap.DrawBuffer(fillImage.Data.Data);

                var actionParameter = $"{page}|{index}";
                base.ActionImageChanged(actionParameter);
            }
            catch
            {
                //
            }
        }
        
        protected override bool ProcessTouchEvent(string actionParameter, DeviceTouchEvent touchEvent)
        {
            var split = actionParameter.Split('|');

            if (!int.TryParse(split[0], out var page))
                return false;

            if (!int.TryParse(split[1], out var bank))
                return false;
            
            var obj = page == Dynamic
                ? new { keyIndex = bank } as object
                : new { page, bank } as object;

            //TODO: How to get haptic feedback?
            switch (touchEvent.EventType)
            {
                case DeviceTouchEventType.TouchDown:
                    _client.SendCommand("keydown", obj);
                    break;
                case DeviceTouchEventType.TouchUp:
                    _client.SendCommand("keyup", obj);
                    break;
            }
            
            //It is supposed to be true... but then I will loose haptic feedback...
            return false;
        }

        protected override PluginProfileActionData GetProfileActionData()
        {
            var tree = new PluginProfileActionTree("Button");

            tree.AddLevel("Page");
            tree.AddLevel("Button");

            var dynamicNope = tree.Root.AddNode("dynamic");
            for (var bank = 0; bank < 32; bank++)
            {
                var button = bank + 1;
                dynamicNope.AddItem($"{Dynamic}|{bank}", $"Page dynamic, button {button}", "Page dynamic");
            }

            for (var page = 1; page <= 99; page++)
            {
                var node = tree.Root.AddNode($"Page {page}");

                for (var bank = 0; bank < 32; bank++)
                {
                    var button = bank + 1;
                    node.AddItem($"{page}|{bank}", $"Page {page}, button {button}", $"Page {page}");
                }
            }

            return tree;
        }

        protected override BitmapImage GetCommandImage(string actionParameter, PluginImageSize imageSize)
        {
            if (string.IsNullOrWhiteSpace(actionParameter))
                return null;

            var split = actionParameter.Split('|');

            if (!int.TryParse(split[0], out var page))
                return null;

            if (!int.TryParse(split[1], out var bank))
                return null;

            var image = _imageCache[page, bank];
            if (image is null)
            {
                //Request image resource.
                //There is limited how many images we can load, we only want to show those who at some point has been on the screen.
                //We don't want to request on dynamic pages.
                if (page != 0)
                    _ = Task.Run(() => _client.SendCommand("request_button", new {page, bank})); //TODO: Only if connected. And on every reconnect.

                //Image not loaded yet.
                using (var bitmapBuilder = new BitmapBuilder(72, 72))
                {
                    bitmapBuilder.Clear(new BitmapColor(0xFF, 0x00, 0x00));
                    bitmapBuilder.DrawText("Image Missing");
                    return bitmapBuilder.ToImage();
                }
            }

            using (var memoryStream = new MemoryStream())
            {
                image.Save(memoryStream, ImageFormat.Bmp);

                var data = memoryStream.ToArray();

                return BitmapImage.FromArray(data);
            }
        }
    }
}