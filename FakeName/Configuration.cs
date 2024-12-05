using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace FakeName;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public bool Enabled { get; set; } = false;
    public bool AllPlayerReplace { get; set; } = false;
    public string FakeNameText { get; set; } = Service.ClientState.LocalPlayer?.Name.TextValue ?? string.Empty;
    public bool FreeCompanyNameReplace { get; set; } = true;

    public HashSet<string> CharacterNames = [];
    public HashSet<string> FriendList = [];

    public List<(string, string)> NameDict = [];
    public List<(string, string)> FCNameDict = [];
    internal void SaveConfig()
    {
        Service.PluginInterface.SavePluginConfig(this);
    }
}
