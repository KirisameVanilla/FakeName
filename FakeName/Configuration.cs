using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace FakeName;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public bool Enabled = false;
    public bool ReplaceLocalPlayer = true;
    public bool ReplaceAllPlayer = false;
    public string FakeNameText = Service.ClientState.LocalPlayer?.Name.TextValue ?? string.Empty;
    public bool FreeCompanyNameReplace = true;

    public HashSet<string> FriendList = [];

    public Dictionary<string, string> NameDict = [];
    public Dictionary<string, string> FreeCompanyNameDict = [];

    public string Language = "en-US";
    internal void SaveConfig() => Service.PluginInterface.SavePluginConfig(this);
}
