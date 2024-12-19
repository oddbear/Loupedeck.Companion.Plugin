using Loupedeck.CompanionPlugin.Extensions;
using Loupedeck.CompanionPlugin.Responses;
using Loupedeck.CompanionPlugin.Services;
using SkiaSharp;

namespace Loupedeck.CompanionPlugin.Adjustments;

internal class CompanionAdjustment : PluginDynamicAdjustment
{
    private CompanionPlugin _plugin;
    private CompanionClient Client => _plugin?.Client;

    private const int Dynamic = 0;

    private readonly SKBitmap[,] _imageCache = new SKBitmap[100, 32];

    public CompanionAdjustment()
        : base(true)
    {
        this.DisplayName = "Companion adjustment";
        this.GroupName = "";
        this.Description = "Control anything in companion.";

        this.MakeProfileAction("tree");
    }

    protected override bool OnLoad()
    {
        _plugin = (CompanionPlugin)base.Plugin;
        Client.FillImageResponse += PluginOnFillImageResponse;

        return true;
    }

    protected override bool OnUnload()
    {
        Client.FillImageResponse -= PluginOnFillImageResponse;

        return true;
    }

    private void PluginOnFillImageResponse(object sender, ResponseFillImage fillImage)
    {
        try
        {
            var page = fillImage.Page ?? Dynamic;
            var index = fillImage.Bank ?? fillImage.KeyIndex;

            if (_imageCache[page, index] is null)
                _imageCache[page, index] = new SKBitmap(72, 72);

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
    
    protected override void RunCommand(string actionParameter)
    {
        // TODO: Seems like there is no way to get Press and Release events other than
        // ProcessButtonEvent2, but I won't get the actionParameter (just COM stuff)
        ProcessTouch(actionParameter, DeviceTouchEventType.Press);
        ProcessTouch(actionParameter, DeviceTouchEventType.TouchUp);
    }

    private void ProcessTouch(string actionParameter, DeviceTouchEventType eventType)
    {
        var split = actionParameter.Split('|');

        if (!int.TryParse(split[0], out var page))
            return;

        if (!int.TryParse(split[1], out var bank))
            return;

        object obj = page == Dynamic
            ? new { keyIndex = bank }
            : new { page, bank };

        switch (eventType)
        {
            case DeviceTouchEventType.Press:
                Client.SendCommand("keydown", obj);
                break;
            case DeviceTouchEventType.TouchUp:
                Client.SendCommand("keyup", obj);
                break;
        }
    }

    protected override void ApplyAdjustment(string actionParameter, int diff)
    {
        var split = actionParameter.Split('|');

        if (!int.TryParse(split[0], out var page))
            return;

        if (!int.TryParse(split[1], out var bank))
            return;

        object obj = page == Dynamic
            ? new { keyIndex = bank, ticks = diff }
            : new { page, bank, ticks = diff };

        Client.SendCommand("rotate", obj);
        base.ApplyAdjustment(actionParameter, diff);
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
                node.AddItem($"{page}|{bank}", $"Page {page}, button {button} ({bank / 8}/{bank % 8})", $"Page {page}");
            }
        }

        return tree;
    }

    protected override BitmapImage GetCommandImage(string actionParameter, PluginImageSize imageSize)
    {
        if (string.IsNullOrWhiteSpace(actionParameter))
            return null;

        if (!Client.Connected)
            return BitmapExtensions.DrawDisconnected();

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
            {
                Client.OnConnectCommand(new { command = "request_button", arguments = new { page, bank } });
            }

            //Image not loaded yet.
            using var bitmapBuilder = new BitmapBuilder(72, 72);
            bitmapBuilder.Clear(new BitmapColor(0xFF, 0x00, 0x00));
            bitmapBuilder.DrawText("Image Missing");
            return bitmapBuilder.ToImage();
        }

        return image.BitmapToBitmapImage();
    }
}