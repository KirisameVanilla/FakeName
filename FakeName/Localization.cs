using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeName;

public class Localization
{
    public const string Value = """
                                [
                                    {
                                        "id": "test",
                                        "zh-CN": "测试",
                                        "en-US": "Test"
                                    },
                                    {
                                        "id": "enabled",
                                        "zh-CN": "启用",
                                        "en-US": "Enabled"
                                    },
                                    {
                                        "id": "player names",
                                        "zh-CN": "玩家名字",
                                        "en-US": "Player Names"
                                    },
                                    {
                                        "id": "replace local pc name",
                                        "zh-CN": "替换当前玩家名字",
                                        "en-US": "Replace Local Character Name"
                                    },
                                    {
                                        "id": "reset to local pc name",
                                        "zh-CN": "重置为当前玩家名字",
                                        "en-US": "Reset to Local Character Name"
                                    },
                                    {
                                        "id": "change name to abbr",
                                        "zh-CN": "将所有玩家名字改为缩写\n(只在国际服有用)",
                                        "en-US": "Change All Player's Name To Abbr.\n(Only works for SE server)"
                                    },
                                    {
                                        "id": "Original Name",
                                        "zh-CN": "原始名字",
                                        "en-US": "Original Name"
                                    },
                                    {
                                        "id": "Replaced Name",
                                        "en-US": "Replaced Name",
                                        "zh-CN": "替换为"
                                    },
                                    {
                                        "id": "op",
                                        "zh-CN": "操作",
                                        "en-US": "Operation"
                                    },
                                    {
                                        "id": "fcNameHint",
                                        "en-US": "The FC replacement only effect on the nameplate.",
                                        "zh-CN": "部队名替换只对姓名牌(非冒险者名牌)生效。"
                                    }
                                ]
                                """;
}
