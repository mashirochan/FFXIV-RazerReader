using Newtonsoft.Json;
using System.Collections.Generic;

namespace RazerReader.Models
{
    public class DeviceLog
    {
        public required string serialNumber { get; set; }
        public required int editionId { get; set; }
        public required int layoutId { get; set; }
        public required string activeProfile { get; set; }
        public required int productId { get; set; }
        public required List<Profile> profiles { get; set; }
        public required string setupStatus { get; set; }
        public required bool isChromaDevice { get; set; }
        public required bool hasBattery { get; set; }
        public required bool useBle { get; set; }
        public required bool isScrollingFeature { get; set; }
        public required bool isExternalBatt { get; set; }
        public required string displayedSerialNumber { get; set; }
        public required Name editionName { get; set; }
        public required Name name { get; set; }
        public required Name productName { get; set; }
        public required string category { get; set; }
        public required string subCategory { get; set; }
        public required string UIWindowName { get; set; }
        public required string MWWindowName { get; set; }
        public required string deviceContainerId { get; set; }
        public required List<Effect> supportEffects { get; set; }
        public required int minDPI { get; set; }
        public required int maxDPI { get; set; }
        public required int dpiStep { get; set; }
        public required bool supportXYDPI { get; set; }
        public required PowerStatus powerStatus { get; set; }
        public required string sidePanelName { get; set; }
        public required int sidepadLayout { get; set; }
        public required FirmwareInfo firmwareInfo { get; set; }
        public required bool isLightingOff { get; set; }
    }

    public class Profile
    {
        public required string name { get; set; }
        public required string guid { get; set; }
        public required DpiStages dpiStages { get; set; }
    }

    public class DpiStages
    {
        public required List<Stage> stages { get; set; }
        public required int active { get; set; }
        public required bool enable { get; set; }
        public required int count { get; set; }
    }

    public class Stage
    {
        public required int x { get; set; }
        public required int y { get; set; }
        public required bool independent { get; set; }
        public required bool visible { get; set; }
        public required int index { get; set; }
    }

    public class Name
    {
        public required string en { get; set; }
        [JsonProperty(PropertyName = "zh-cn")]
        public required string zhCn { get; set; }
        public required string de { get; set; }
        public required string es { get; set; }
        public required string fr { get; set; }
        public required string ja { get; set; }
        public required string kr { get; set; }
        [JsonProperty(PropertyName = "pt-br")]
        public required string ptBr { get; set; }
        public required string ru { get; set; }
        [JsonProperty(PropertyName = "zh-tw")]
        public required string zhTw { get; set; }
    }

    public class Effect { }

    public class PowerStatus
    {
        public required string chargingStatus { get; set; }
        public required int level { get; set; }
    }

    public class FirmwareInfo
    {
        public required string currentFWVersion { get; set; }
    }
}
