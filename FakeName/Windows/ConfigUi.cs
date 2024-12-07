using Dalamud.Interface;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace FakeName.Windows;

public class ConfigUi : IDisposable
{
    public bool Visible;
    public ConfigUi()
    {
        Service.PluginInterface.UiBuilder.OpenConfigUi += OnOpenUi;
        Service.PluginInterface.UiBuilder.OpenMainUi += OnOpenUi;
        Service.PluginInterface.UiBuilder.Draw += DrawConfig;
    }

    public void Dispose()
    {
        Service.PluginInterface.UiBuilder.OpenConfigUi -= OnOpenUi;
        Service.PluginInterface.UiBuilder.OpenMainUi -= OnOpenUi;
        Service.PluginInterface.UiBuilder.Draw -= DrawConfig;
    }

    private void DrawConfig()
    {
        if (!Visible) return;
        var enabled = Service.Config.Enabled;
        if (ImGui.Begin("FakeName Config", ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.SetNextWindowSize(new Vector2(480f, 640f), ImGuiCond.FirstUseEver);
            if (ImGui.Checkbox("Enabled", ref enabled))
            {
                Service.Config.Enabled = enabled;
                Service.Config.SaveConfig();
            }
        }
        else return;

        if (Service.Config.Enabled
            && ImGui.BeginTabBar("##tabBar", ImGuiTabBarFlags.Reorderable))
        {
            if (ImGui.BeginTabItem("Settings"))
            {
                var fakeNameText = Service.Config.FakeNameText;
                ImGui.Text("Character Name");

                if (Service.ClientState.LocalPlayer is not null)
                {
                    ImGui.SameLine();
                    if (ImGui.Button("Reset To Default##Reset Character Name"))
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

                var allPlayerReplace = Service.Config.AllPlayerReplace;
                if (ImGui.Checkbox("Change All Player's Name To Abbr.\n(Only works for SE server)", ref allPlayerReplace))
                {
                    Service.Config.AllPlayerReplace = allPlayerReplace;
                    Service.Config.SaveConfig();
                }

                var fcNameReplace = Service.Config.FreeCompanyNameReplace;
                if (ImGui.Checkbox("Change FC Names", ref fcNameReplace))
                {
                    Service.Config.FreeCompanyNameReplace = fcNameReplace;
                    Service.Config.SaveConfig();
                }
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Player Names"))
            {
                if (Service.Config.Enabled)
                {
                    DrawList(Service.Config.NameDict);
                }

                ImGui.EndTabItem();
            }

            if (Service.Config.FreeCompanyNameReplace && ImGui.BeginTabItem("FC Names"))
            {
                ImGui.TextWrapped("The FC replacement only effect on the nameplate.");
                DrawList(Service.Config.FreeCompanyNameDict);
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    private static void DrawList(List<(string, string)> data)
    {

        if (!data.Any(p => string.IsNullOrEmpty(p.Item1)))
        {
            data.Add((string.Empty, string.Empty));
        }

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
            ImGui.TableHeader("Delete");

            var index = 0;

            var removeIndex = -1;
            var changedIndex = -1;

            var changedValue = (string.Empty, string.Empty);
            foreach (var(key, value) in data)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                var str = key;
                if (ImGui.InputTextWithHint($"##NameDict Key{index}", "Original Name", ref str, 1024))
                {
                    changedIndex = index;
                    changedValue = (str, value);
                }
                ImGui.TableNextColumn();

                str = value;

                if (ImGui.InputTextWithHint($"##NameDict Value{index}", "Replace Name", ref str, 1024))
                {
                    changedIndex = index;
                    changedValue = (key, str);
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

            ImGui.EndTable();
            if (removeIndex > -1)
            {
                data.RemoveAt(removeIndex);
                Service.Config.SaveConfig();
            }
            if (changedIndex > -1)
            {
                data[changedIndex] = changedValue;
                Service.Config.SaveConfig();
            }
        }
    }
    private void OnOpenUi() => Visible ^= true;
}
