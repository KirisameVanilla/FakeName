using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace FakeName;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public bool Enabled { get; set; } = false;
    public bool ReplaceLocalPlayer { get; set; } = true;
    public bool ReplaceAllPlayer { get; set; } = false;
    public string FakeNameText { get; set; } = Service.ClientState.LocalPlayer?.Name.TextValue ?? string.Empty;
    public bool FreeCompanyNameReplace { get; set; } = true;

    public HashSet<string> FriendList = [];

    public Dictionary<string, string> NameDict = [];
    public Dictionary<string, string> FreeCompanyNameDict = [];
    internal void SaveConfig()
    {
        Service.PluginInterface.SavePluginConfig(this);
    }
}
