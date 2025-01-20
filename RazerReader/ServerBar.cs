using Dalamud.Game.Gui.Dtr;

namespace RazerReader
{
    public class ServerBar
    {
        private readonly Plugin plugin;
        private readonly IDtrBarEntry? dtrEntry;

        public ServerBar(Plugin plugin)
        {
            this.plugin = plugin;

            if (Plugin.DtrBar.Get("RazerReader") is not { } entry)
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

        private void OnClick()
        {
            plugin.ToggleConfigUI();
        }

        public void UpdateDtrBar()
        {
            if (plugin.Configuration.ShowDtrEntry)
            {
                dtrEntry!.Shown = true;
                if (!Plugin.DeviceList.IsEmpty() && Plugin.DeviceList.LowestDevice != null)
                {
                    dtrEntry.Text = $"{Plugin.DeviceList.LowestDevice!.level}% {Plugin.DeviceList.LowestDevice!.name}";
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
}
