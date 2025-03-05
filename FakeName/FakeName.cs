using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui.NamePlate;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Game;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace FakeName;

public class FakeName
{
    private delegate void AtkTextNodeSetTextDelegate(IntPtr node, IntPtr text);

    /// <summary>
    /// https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Component/GUI/AtkTextNode.cs#L48
    /// </summary>
    [Signature("E8 ?? ?? ?? ?? 8D 4E 32", DetourName = nameof(AtkTextNodeSetTextDetour))]
    private Hook<AtkTextNodeSetTextDelegate>? AtkTextNodeSetTextHook { get; init; }

    public static Dictionary<string, string> Replacement { get; private set; } = [];

    internal FakeName()
    {
        Service.Hook.InitializeFromAttributes(this);

        AtkTextNodeSetTextHook?.Enable();

        Service.NamePlate.OnDataUpdate += OnNamePlateDataUpdate;

        Service.Framework.Update += OnUpdate;
    }

    private static void OnNamePlateDataUpdate(INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        if (!Service.Config.Enabled) return;

        foreach (var handler in handlers)
        {
            switch (handler.NamePlateKind)
            {
                case NamePlateKind.PlayerCharacter:
                    var str = handler.Name?.TextValue;

                    if (!string.IsNullOrEmpty(str))
                    {
                        handler.Name = ReplaceNameplate(str);
                    }

                    var nameSe = handler.FreeCompanyTag.TextValue;
                    foreach (var(key, value) in Service.Config.FreeCompanyNameDict)
                    {
                        var fcKey = " «" + key + "»";
                        if (fcKey != nameSe) continue;
                        handler.FreeCompanyTag = " «" + value + "»";
                        break;
                    }
                    break;

                case NamePlateKind.EventNpcCompanion:
                    if (handler.GameObject?.ObjectKind is not Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Companion) break;
                    str = handler.Title.ToString();
                    if (string.IsNullOrEmpty(str)) break;
                    if (str.Length < 3) break;
                    var start = str[0];
                    var end = str[^1];
                    handler.Title = start + ReplaceNameplate(str[1..^1]) + end;
                    break;
            }
        }
    }

    private static string ReplaceNameplate(string str)
    {
        if (Service.ClientState.LocalPlayer != null && Service.ClientState.LocalPlayer.Name.TextValue.Contains(str))
        {
            return Service.Config.FakeNameText;
        }
        return Service.Config.ReplaceAllPlayer ? GetChangedName(str) : str;
    }

    public void Dispose()
    {
        AtkTextNodeSetTextHook?.Dispose();
        Service.NamePlate.OnDataUpdate -= OnNamePlateDataUpdate;

        Service.Framework.Update -= OnUpdate;
    }

    private static unsafe void OnUpdate(IFramework framework)
    {
        if (!Service.Config.Enabled) return;

        var replacements = new Dictionary<string, string>();

        try
        {
            var player = Service.ClientState.LocalPlayer;

            if (Service.Config.ReplaceLocalPlayer && player != null)
            {
                if (!Service.Config.NameDict.TryAdd(player.Name.TextValue, Service.Config.FakeNameText)) 
                    Service.Config.NameDict[player.Name.TextValue] = Service.Config.FakeNameText;
            }

            foreach (var(key, value) in Service.Config.NameDict)
            {
                if (!replacements.ContainsKey(key))
                    replacements.TryAdd(key, value);
            }

            if (Service.ClientState.ClientLanguage == (ClientLanguage) 4 
                || !Service.Config.ReplaceAllPlayer) 
                return;

            foreach (var obj in Service.ObjectTable)
            {
                if (obj is not IPlayerCharacter member) continue;
                var memberName = member.Name.TextValue;
                if (memberName == player?.Name.TextValue) continue;

                replacements.Add(memberName, GetChangedName(memberName));
            }

            if (Service.Condition[ConditionFlag.ParticipatingInCrossWorldPartyOrAlliance])
                foreach (var x in InfoProxyCrossRealm.Instance()->CrossRealmGroups[0].GroupMembers)
                {
                    var name = Encoding.UTF8.GetString(x.Name);
                    replacements.Add(name, GetChangedName(name));
                }
            else
            {
                foreach (var obj in Service.Config.FriendList)
                {
                    replacements.Add(obj, GetChangedName(obj));
                }
            }

            var friendList = (AddonFriendList*)Service.GameGui.GetAddonByName("FriendList", 1);
            if (friendList == null) return;

            var list = friendList->FriendList;
            if (list == null) return;
            for (var i = 0; i < list->ListLength; i++)
            {
                var item = list->ItemRendererList[i];
                var textNode = item.AtkComponentListItemRenderer->AtkComponentButton.ButtonTextNode;

                var text = textNode->NodeText.ToString();
                if (!text.Contains('.') && Service.Config.FriendList.Add(text))
                {
                    Service.Config.SaveConfig();
                }
            }
        }
        finally
        {
            Replacement = replacements;
#if DEBUG
            foreach (var replacement in Replacement)
                Service.Log.Debug($"Key:{replacement.Key}||||Value:{replacement.Value}");
#endif
        }
    }

    private void AtkTextNodeSetTextDetour(IntPtr node, IntPtr text)
    {
        if (!Service.Config.Enabled)
        {
            AtkTextNodeSetTextHook?.Original(node,text);
            return;
        }
        AtkTextNodeSetTextHook?.Original(node, ChangeName(text));
    }

    public static IntPtr ChangeName(IntPtr seStringPtr)
    {
        if (seStringPtr == IntPtr.Zero) return seStringPtr;

        try
        {
            var str = GetSeStringFromPtr(seStringPtr);
            if (ChangeSeString(str))
                GetPtrFromSeString(str, seStringPtr);
            return seStringPtr;
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, "Something wrong with change name!");
            return seStringPtr;
        }

        static bool ChangeSeString(SeString seString)
        {
            try
            {
                return seString.Payloads.Any(payload => payload.Type == PayloadType.RawText) 
                       && Replacement.Any(pair => ReplacePlayerName(seString, pair.Key, pair.Value));
            }
            catch (Exception ex)
            {
                Service.Log.Error(ex, "Something wrong with replacement!");
                return false;
            }
        }
    }

    public static void GetPtrFromSeString(SeString str, IntPtr ptr)
    {
        var bytes = str.Encode();
        Marshal.Copy(bytes, 0, ptr, bytes.Length);
        Marshal.WriteByte(ptr, bytes.Length, 0);
    }

    public static SeString GetSeStringFromPtr(IntPtr seStringPtr)
    {
        var offset = 0;
        unsafe
        {
            while (*(byte*)(seStringPtr + offset) != 0)
                offset++;
        }
        var bytes = new byte[offset];
        Marshal.Copy(seStringPtr, bytes, 0, offset);
        return SeString.Parse(bytes);
    }

    public static string GetChangedName(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;

        foreach (var(key, value) in Service.Config.NameDict)
        {
            if (key == str) return value;
        }
        var lt = str.Split(' ');
        return lt.Length != 2 ? str : string.Join(" . ", lt.Select(s => s.ToUpper().FirstOrDefault()));
    }

    private static bool ReplacePlayerName(SeString text, string name, string replacement)
    {
        if (string.IsNullOrEmpty(name)) return false;

        var result = false;
        foreach (var payLoad in text.Payloads)
        {
            if (payLoad is TextPayload load)
            {
                if (string.IsNullOrEmpty(load.Text)) continue;

                var t = load.Text.Replace(name, replacement);
                if (t == load.Text) continue;

                load.Text = t;
                result = true;
            }
        }
        return result;
    }
}
