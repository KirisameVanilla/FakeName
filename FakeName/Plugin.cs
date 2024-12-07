using Dalamud.Plugin;
using FakeName.Windows;
using System;
using Dalamud.Game.Command;

namespace FakeName;

public class Plugin : IDalamudPlugin
{
    internal Hooker Hooker { get; }
    internal ConfigUi ConfigUi;
    private const string Command = "/fn";

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();
        Service.Config = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        Hooker = new Hooker();
        ConfigUi = new ConfigUi();
        Service.CommandManager.AddHandler(Command, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open config window."
        });
    }

    public void Dispose()
    {
        Hooker.Dispose();
        Service.CommandManager.RemoveHandler(Command);
        GC.SuppressFinalize(this);
    }

    private void OnCommand(string command, string args) => ConfigUi.Visible ^= true;
}
