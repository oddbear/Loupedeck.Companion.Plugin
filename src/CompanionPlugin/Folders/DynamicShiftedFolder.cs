using Loupedeck.CompanionPlugin.Extensions;
using Loupedeck.CompanionPlugin.Responses;
using Loupedeck.CompanionPlugin.Services;
using SkiaSharp;

namespace Loupedeck.CompanionPlugin.Folders;

class DynamicShiftedFolder : PluginDynamicFolder
{
    private readonly SKBitmap[] _buttons;

    private CompanionPlugin _plugin;
    private CompanionClient Client => _plugin?.Client;

    public DynamicShiftedFolder()
    {
        _buttons = new SKBitmap[34];
        for (var i = 0; i < 34; i++)
            _buttons[i] = new SKBitmap(72, 72);

        this.DisplayName = "Companion Shifted";
        this.GroupName = "Dynamic Folder";
    }

    public override PluginDynamicFolderNavigation GetNavigationArea(DeviceType deviceType)
    {
        return PluginDynamicFolderNavigation.None;
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
            if (actionParameter == "8")
                base.AdjustmentImageChanged("page");
            else
                base.CommandImageChanged(actionParameter);
        }
        catch
        {
            //
        }
    }

    public override void RunCommand(string actionParameter)
    {
        if (actionParameter?.StartsWith("dial3") is false)
            return;

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

        // Always dynamic:
        object obj = new { keyIndex = bank };

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

    public override void ApplyAdjustment(string actionParameter, int diff)
    {
        if (actionParameter == "page")
        {
            if (diff < 0)
                Client.SendCommand("keydown", new { keyIndex = 16 });

            if (diff > 0)
                Client.SendCommand("keydown", new { keyIndex = 0 });
        }
        else
        {
            var keyIndex = actionParameter switch
            {
                "dial3" => 24,
                // What indexes should these be?
                "dial4" => 5, // (0/5)
                "dial5" => 8 + 5, // (1/5)
                "dial6" => 16 + 5,  // (2/5)
                _ => 0
            };

            if (keyIndex == 0)
                return;

            Client.SendCommand("rotate", new { keyIndex, ticks = diff });
        }
    }

    public override BitmapImage GetAdjustmentImage(string actionParameter, PluginImageSize imageSize)
    {
        var buttonIndex = actionParameter switch
        {
            "page" => 8,
            "dial3" => 24,
            // What indexes should these be?
            "dial4" => 5, // (0/5)
            "dial5" => 8 + 5, // (1/5)
            "dial6" => 16 + 5, // (2/5)
            _ => 0
        };

        if (buttonIndex == 0)
            return base.GetAdjustmentImage(actionParameter, imageSize);
            
        if (!Client.Connected)
            return BitmapExtensions.DrawDisconnected();

        var image = _buttons[buttonIndex];

        var bitmapImage = image.BitmapToBitmapImage();
        if (actionParameter != "page")
            return bitmapImage;
        
        // Zoom a little inn on the page button:
        using var bitmapBuilder = new BitmapBuilder(50, 50);
        bitmapBuilder.DrawImage(bitmapImage, -10, -10);
        return bitmapBuilder.ToImage();
    }

    public override IEnumerable<string> GetEncoderPressActionNames(DeviceType deviceType)
    {
        // Pass through to Adjustments names
        return
        [
            PluginDynamicFolder.NavigateUpActionName, // Navigate back by press to Loupedeck buttons
            this.CreateAdjustmentName("page"), // register page adjustment (combines the 3 buttons to one)
            // Free adjustments:
            this.CreateAdjustmentName("dial3"), // left bottom
            this.CreateAdjustmentName("dial4"), // right top
            this.CreateAdjustmentName("dial5"), // right middle
            this.CreateAdjustmentName("dial6"), // right bottom
        ];
    }

    public override IEnumerable<string> GetEncoderRotateActionNames(DeviceType deviceType)
    {

        //TODO: DeviceType is not used, but I don't have more than the CT devices to test with.
        // Live S, Stream Controller X, and the new MX devices could be a problem.
        return
        [
            PluginDynamicFolder.NavigateUpActionName, // Navigate back by press to Loupedeck buttons
            this.CreateAdjustmentName("page"), // register page adjustment (combines the 3 buttons to one)
            // Free adjustments:
            this.CreateAdjustmentName("dial3"), // left bottom
            this.CreateAdjustmentName("dial4"), // right top
            this.CreateAdjustmentName("dial5"), // right middle
            this.CreateAdjustmentName("dial6"), // right bottom
        ];
    }

    public override bool ProcessTouchEvent(string actionParameter, DeviceTouchEvent touchEvent)
    {
        if (!int.TryParse(actionParameter, out var index))
            return false;

        //TODO: How to get haptic feedback?
        switch (touchEvent.EventType)
        {
            case DeviceTouchEventType.Press:
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
        return
        [
            this.CreateCommandName("1"),
            this.CreateCommandName("2"),
            this.CreateCommandName("3"),
            this.CreateCommandName("4"),

            this.CreateCommandName("9"),
            this.CreateCommandName("10"),
            this.CreateCommandName("11"),
            this.CreateCommandName("12"),

            this.CreateCommandName("17"),
            this.CreateCommandName("18"),
            this.CreateCommandName("19"),
            this.CreateCommandName("20")
        ];
    }
}