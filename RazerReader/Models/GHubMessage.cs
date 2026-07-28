using System.Text.Json.Nodes;

namespace BatteryBuddy.Models;

public class GHubMessage
{
    public string? MsgId { get; set; }
    public string? Verb { get; set; }
    public string? Path { get; set; }
    public string? Origin { get; set; }
    public JsonObject? Result { get; set; }
    public JsonObject? Payload { get; set; }
}
