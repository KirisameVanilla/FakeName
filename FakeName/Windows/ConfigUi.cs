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

        if (ImGui.Checkbox("Enabled", ref enabled))
        {
            Service.Config.Enabled = enabled;
            Service.Config.SaveConfig();
        }

        using var disabled = ImRaii.Disabled(!Service.Config.Enabled);
        if (ImGui.BeginTabBar("##tabBar", ImGuiTabBarFlags.Reorderable))
        {
            if (ImGui.BeginTabItem("Player Names"))
            {
                var fakeNameText = Service.Config.FakeNameText;
                var replaceLocalPlayer = Service.Config.ReplaceLocalPlayer;
                if (ImGui.Checkbox("Replace Local Character Name##ReplaceLocalPlayer", ref replaceLocalPlayer))
                {
                    Service.Config.ReplaceLocalPlayer = replaceLocalPlayer;
                    Service.Config.SaveConfig();
                    if (!replaceLocalPlayer)
                    {
                        Service.Config.NameDict.Remove(Service.ClientState.LocalPlayer.Name.TextValue);
                        Service.Config.SaveConfig();
                    }
                }

                if (Service.ClientState.LocalPlayer is not null)
                {
                    ImGui.SameLine();
                    if (ImGui.Button("Reset##Reset Character Name"))
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
                    if (ImGui.Checkbox("Change All Player's Name To Abbr.\n(Only works for SE server)",
                                       ref allPlayerReplace))
                    {
                        Service.Config.ReplaceAllPlayer = allPlayerReplace;
                        Service.Config.SaveConfig();
                    }
                }

                if (Service.Config.Enabled)
                {
                    DrawList(ref Service.Config.NameDict);
                }

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("FC Names"))
            {
                var fcNameReplace = Service.Config.FreeCompanyNameReplace;
                if (ImGui.Checkbox("Change FC Names", ref fcNameReplace))
                {
                    Service.Config.FreeCompanyNameReplace = fcNameReplace;
                    Service.Config.SaveConfig();
                }
                ImGui.TextWrapped("The FC replacement only effect on the nameplate.");

                using var fcDisabled = ImRaii.Disabled(!Service.Config.FreeCompanyNameReplace);
                DrawList(ref Service.Config.FreeCompanyNameDict);
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    private static void DrawList(ref Dictionary<string, string> data)
    {
        var dataList = data.ToList();

        if (ImGui.BeginTable("Name Dict things", 3, ImGuiTableFlags.Borders
            | ImGuiTableFlags.Resizable
            | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

            ImGui.TableNextColumn();
            ImGui.TableHeader("Original Name");

            ImGui.TableNextColumn();
            ImGui.TableHeader("Replaced Name");

            ImGui.TableNextColumn();
            ImGui.TableHeader("Operation");

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

                if (ImGui.InputTextWithHint($"##NameDict Key{index}", "Original Name", ref strKey, 1024))
                {
                    changedIndex = index;
                    changedValue = (strKey, strV);
                }
                ImGui.TableNextColumn();

                if (ImGui.InputTextWithHint($"##NameDict Value{index}", "Replace Name", ref strV, 1024))
                {
                    changedIndex = index;
                    changedValue = (strKey, strV);
                }
                ImGui.TableNextColumn();

                ImGui.PushFont(UiBuilder.IconFont);
                var result = ImGui.Button(FontAwesomeIcon.TrashAlt.ToIconString() + $"##Remove NameDict Key{index}");
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
            if (ImGui.Button(FontAwesomeIcon.Plus.ToIconString() + $"##addNewName") && !data.ContainsKey(string.Empty))
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
