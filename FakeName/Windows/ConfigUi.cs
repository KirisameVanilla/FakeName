using Dalamud.Interface;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace FakeName.Windows;

public class ConfigUi : Window, IDisposable
{
    private readonly Dictionary<string, string> languageDictionary = new()
    {
        {  "简体中文", "zh-CN" },
        {  "English", "en-US" }
    };

    private readonly string[] languageList = ["简体中文", "English"];

    private int selectedIndex = 0;

    public ConfigUi()
        : base("FakeName##Config", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(480f, 320f),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        var enabled = Service.Config.Enabled;
        var localization = Plugin.Translations;
        if (ImGui.Checkbox($"{localization["enabled"]}##GlobalEnabled" , ref enabled))
        {
            Service.Config.Enabled = enabled;
            Service.Config.SaveConfig();
        }
        
        using var disabled = ImRaii.Disabled(!Service.Config.Enabled);
        if (ImGui.BeginTabBar("##tabBar", ImGuiTabBarFlags.None))
        {
            if (ImGui.BeginTabItem($"{localization["player names"]}##ConfigTabItem"))
            {
                var fakeNameText = Service.Config.FakeNameText;

                using (ImRaii.Disabled(Service.ClientState.LocalPlayer is null))
                {
                    var replaceLocalPlayer = Service.Config.ReplaceLocalPlayer;
                    if (ImGui.Checkbox($"{localization["replace local pc name"]}##ConfigReplaceLocalPlayer", ref replaceLocalPlayer))
                    {
                        Service.Config.ReplaceLocalPlayer = replaceLocalPlayer;
                        Service.Config.SaveConfig();
                        if (!replaceLocalPlayer)
                        {
                            Service.Config.NameDict.Remove(Service.ClientState.LocalPlayer.Name.TextValue);
                            Service.Config.SaveConfig();
                        }
                    }

                    ImGui.SameLine();
                    if (ImGui.Button($"{localization["reset to local pc name"]}##ConfigResetToLocalCharacterName"))
                    {
                        Service.Config.FakeNameText = Service.ClientState.LocalPlayer.Name.TextValue;
                        Service.Config.SaveConfig();
                    }
                }

                if (ImGui.InputText("##Character Name", ref fakeNameText, 256))
                {
                    Service.Config.FakeNameText = fakeNameText;
                    Service.Config.SaveConfig();
                }

                if (Service.ClientState.ClientLanguage != (ClientLanguage)4)
                {
                    var allPlayerReplace = Service.Config.ReplaceAllPlayer;
                    if (ImGui.Checkbox($"{localization["change name to abbr"]}##ConfigReplaceAllPlayer",
                                       ref allPlayerReplace))
                    {
                        Service.Config.ReplaceAllPlayer = allPlayerReplace;
                        Service.Config.SaveConfig();
                    }
                }

                DrawList(ref Service.Config.NameDict, "charaNames");
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem($"{localization["fcName"]}"))
            {
                var fcNameReplace = Service.Config.FreeCompanyNameReplace;
                if (ImGui.Checkbox($"{localization["Change FC Names"]}", ref fcNameReplace))
                {
                    Service.Config.FreeCompanyNameReplace = fcNameReplace;
                    Service.Config.SaveConfig();
                }
                ImGui.TextWrapped($"{localization["fcNameHint"]}");

                using (ImRaii.Disabled(!Service.Config.FreeCompanyNameReplace))
                    DrawList(ref Service.Config.FreeCompanyNameDict, "fcNames");
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Language"))
            {
                if (ImGui.Combo("##Language", ref selectedIndex, languageList, languageList.Length))
                {
                    Service.Config.Language = languageDictionary[languageList[selectedIndex]];
                    Service.Config.SaveConfig();
                    Plugin.InitTranslations();
                }
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    private static void DrawList(ref Dictionary<string, string> data, string id)
    {
        var dataList = data.ToList();

        if (ImGui.BeginTable($"Dict##{id}", 3, ImGuiTableFlags.Borders
            | ImGuiTableFlags.Resizable
            | ImGuiTableFlags.SizingStretchProp))
        {
            var originalName = Plugin.Translations["Original Name"].ToString();
            var replacedName = Plugin.Translations["Replaced Name"].ToString();
            var op = Plugin.Translations["op"].ToString();
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

            ImGui.TableNextColumn();
            ImGui.TableHeader($"{originalName}##{id}");

            ImGui.TableNextColumn();
            ImGui.TableHeader($"{replacedName}##{id}");

            ImGui.TableNextColumn();
            ImGui.TableHeader($"{op}##{id}");

            var index = 0;

            var removeIndex = -1;
            var changedIndex = -1;

            var changedValue = (string.Empty, string.Empty);
            foreach (var pair in dataList)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                var strKey = pair.Key;
                var strV = pair.Value;

                if (ImGui.InputTextWithHint($"##{id}Dict Key{index}", $"{originalName}", ref strKey, 1024))
                {
                    changedIndex = index;
                    changedValue = (strKey, strV);
                }
                ImGui.TableNextColumn();

                if (ImGui.InputTextWithHint($"##{id}Dict Value{index}", $"{replacedName}", ref strV, 1024))
                {
                    changedIndex = index;
                    changedValue = (strKey, strV);
                }
                ImGui.TableNextColumn();

                ImGui.PushFont(UiBuilder.IconFont);
                var result = ImGui.Button(FontAwesomeIcon.TrashAlt.ToIconString() + $"##Remove {id}Dict Key{index}");
                ImGui.PopFont();

                if (result)
                {
                    removeIndex = index;
                }

                index++;
            }
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(FontAwesomeIcon.Plus.ToIconString() + $"##AddNewNameTo{id}") && !data.ContainsKey(string.Empty))
            {
                data.Add(string.Empty, string.Empty);
            }
            ImGui.PopFont();

            ImGui.EndTable();
            if (removeIndex > -1)
            {
                dataList.RemoveAt(removeIndex);
                data = dataList.ToDictionary<string, string>();
                Service.Config.SaveConfig();
            }
            if (changedIndex > -1)
            {
                dataList.RemoveAt(changedIndex);
                try
                {
                    dataList.Insert(changedIndex,
                                    new KeyValuePair<string, string>(changedValue.Item1, changedValue.Item2));
                    data = dataList.ToDictionary<string, string>();
                    Service.Config.SaveConfig();
                } catch {}
            }
        }
    }
}
