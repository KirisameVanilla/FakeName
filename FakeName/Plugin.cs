using Dalamud.Plugin;
using FakeName.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Newtonsoft.Json.Linq;

namespace FakeName;

public class Plugin : IDalamudPlugin
{
    internal FakeName FakeName { get; }
    internal ConfigUi ConfigUi { get; init; }
    public readonly WindowSystem WindowSystem = new("FakeName");
    public static Dictionary<string, Translation> Translations = new();
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
        
        FakeName = new FakeName();
        ConfigUi = new ConfigUi();

        WindowSystem.AddWindow(ConfigUi);

        Service.CommandManager.AddHandler(Command, new CommandInfo(OnCommand)
        {
            HelpMessage = """
                          Open config window.
                          打开配置窗口。
                          """
        });

        pluginInterface.UiBuilder.Draw += DrawUi;

        Service.PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        Service.PluginInterface.UiBuilder.OpenMainUi += ToggleConfigUi;

        InitTranslations();
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        FakeName.Dispose();
        ConfigUi.Dispose();
        Service.CommandManager.RemoveHandler(Command);
        GC.SuppressFinalize(this);
    }

    private void DrawUi() => WindowSystem.Draw();
    public void ToggleConfigUi() => ConfigUi.Toggle();
    private void OnCommand(string command, string args) => ToggleConfigUi();

    public class Translation(string english, string chinese)
    {
        private string English { get; set; } = english;
        private string Chinese { get; set; } = chinese;
        public override string ToString()
        {
            return Service.Config.Language switch
            {
                "en-US" => English,
                "zh-CN" => Chinese,
                _ => English
            };
        }
    }

    public static void InitTranslations()
    {
        Translations = new Dictionary<string, Translation>();
        var jsonArray = JArray.Parse(GetManifestJson());

        foreach (var jToken in jsonArray)
        {
            if (jToken["id"] == null || jToken["en-US"] == null || jToken["zh-CN"] == null) continue;
            Translations.Add($"{jToken["id"]}", new Translation($"{jToken["en-US"]}", $"{jToken["zh-CN"]}"));
        }
    }

    public static string GetManifestJson()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("FakeName.Localization.json");
        if (stream != null)
        {
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        return string.Empty;
    }
}
