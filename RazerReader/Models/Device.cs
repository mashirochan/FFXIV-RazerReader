using System.Collections.Generic;

namespace RazerReader.Models
{
    public class DeviceList
    {
        public List<Device> Mice { get; set; } = [];
        public List<Device> Keyboards { get; set; } = [];
        public List<Device> Headsets { get; set; } = [];
        public Device? LowestDevice { get; set; }

        public bool IsEmpty()
        {
            return Mice.Count == 0 && Keyboards.Count == 0 && Headsets.Count == 0;
        }

        public int Count()
        {
            return Mice.Count + Keyboards.Count + Headsets.Count;
        }
    }

    public class Device
    {
        public required string name { get; set; }
        public required string category { get; set; }
        public required string chargingStatus { get; set; }
        public required int level { get; set; }
        public required bool hasBattery { get; set; }
        public bool enabled { get; set; }
    }
}
