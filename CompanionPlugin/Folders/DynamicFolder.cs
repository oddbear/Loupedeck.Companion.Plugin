﻿using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Loupedeck.CompanionPlugin.Extensions;
using Loupedeck.CompanionPlugin.Responses;
using Loupedeck.CompanionPlugin.Services;

namespace Loupedeck.CompanionPlugin.Folders
{
    class DynamicFolder : PluginDynamicFolder
    {
        private Bitmap[] _buttons;

        private CompanionPlugin _plugin;
        private CompanionClient Client => _plugin?.Client;

        public DynamicFolder()
        {
            _buttons = new Bitmap[34];
            for (var i = 0; i < 34; i++)
                _buttons[i] = new Bitmap(72, 72);

            this.DisplayName = "Companion";
            this.GroupName = "Dynamic Folder";
            this.Navigation = PluginDynamicFolderNavigation.EncoderArea;
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
            {
                using (var bitmapBuilder = new BitmapBuilder(80, 80))
                {
                    var path = "Loupedeck.CompanionPlugin.Resources.Companion.disconnected-80.png";
                    var background = EmbeddedResources.ReadImage(path);
                    bitmapBuilder.Clear(BitmapColor.Black);
                    bitmapBuilder.SetBackgroundImage(background);
                    return bitmapBuilder.ToImage();
                }
            }

            var image = _buttons[index];

            using (var memoryStream = new MemoryStream())
            {
                image.Save(memoryStream, ImageFormat.Bmp);

                var data = memoryStream.ToArray();
                
                return BitmapImage.FromArray(data);
            }
        }
        
        public override IEnumerable<string> GetButtonPressActionNames()
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
