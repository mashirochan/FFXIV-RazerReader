using Dalamud.Game.Addon.Events.EventDataTypes;
using Dalamud.Game.Gui.Dtr;

namespace BatteryBuddy;

public class ServerBar
{
    private readonly Plugin plugin;
    private readonly IDtrBarEntry? dtrEntry;

    public ServerBar(Plugin plugin)
    {
        this.plugin = plugin;

        if (Plugin.DtrBar.Get("BatteryBuddy") is not { } entry)
            return;

        dtrEntry = entry;
        
        dtrEntry.OnClick += OnClick;
    }

    public void Dispose()
    {
        if (dtrEntry == null)
            return;

        dtrEntry.OnClick -= OnClick;
        dtrEntry.Remove();
    }

    private void OnClick(DtrInteractionEvent e)
    {
        plugin.ToggleConfigUI();
    }

    public void UpdateDtrBar()
    {
        if (plugin.Configuration.ShowDtrEntry)
        {
            dtrEntry!.Shown = true;
            if (!plugin.Configuration.DeviceList.IsEmpty() && plugin.Configuration.DeviceList.LowestDevice != null)
            {
                var nameText = plugin.Configuration.ShowDeviceName ? $" {plugin.Configuration.DeviceList.LowestDevice!.name}" : "";
                dtrEntry.Text = $"{plugin.Configuration.DeviceList.LowestDevice!.level}%{nameText}";
            }
            else
            {
                dtrEntry.Text = "No devices found...";
            }
        }
        else
        {
            dtrEntry!.Shown = false;
        }
    }
}
