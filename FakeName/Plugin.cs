using Dalamud.Plugin;
using FakeName.Windows;
using System;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;

namespace FakeName;

public class Plugin : IDalamudPlugin
{
    internal Hooker Hooker { get; }
    internal ConfigUi ConfigUi { get; init; }
    public readonly WindowSystem WindowSystem = new("FakeName");

    private const string Command = "/fn";


    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();
        try
        {
            Service.Config = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        }
        catch
        {
            Service.Config = new Configuration();
            Service.Config.SaveConfig();
        }
        
        Hooker = new Hooker();
        ConfigUi = new ConfigUi();

        WindowSystem.AddWindow(ConfigUi);

        Service.CommandManager.AddHandler(Command, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open config window."
        });

        pluginInterface.UiBuilder.Draw += DrawUi;

        Service.PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        Service.PluginInterface.UiBuilder.OpenMainUi += ToggleConfigUi;
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        Hooker.Dispose();
        ConfigUi.Dispose();
        Service.CommandManager.RemoveHandler(Command);
        GC.SuppressFinalize(this);
    }

    private void DrawUi() => WindowSystem.Draw();
    public void ToggleConfigUi() => ConfigUi.Toggle();
    private void OnCommand(string command, string args) => ToggleConfigUi();
}
