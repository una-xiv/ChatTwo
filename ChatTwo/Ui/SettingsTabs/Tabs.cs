using ChatTwo.Code;
using ChatTwo.Resources;
using ChatTwo.Util;
using Dalamud.Interface;
using ImGuiNET;

namespace ChatTwo.Ui.SettingsTabs;

internal sealed class Tabs : ISettingsTab {
    private Plugin Plugin { get; }
    private Configuration Mutable { get; }

    public string Name => Language.Options_Tabs_Tab + "###tabs-tabs";

    private int _toOpen = -2;

    internal Tabs(Plugin plugin, Configuration mutable) {
        Plugin = plugin;
        Mutable = mutable;
    }

    public void Draw(bool changed) {
        const string addTabPopup = "add-tab-popup";

        if (ImGuiUtil.IconButton(FontAwesomeIcon.Plus, tooltip: Language.Options_Tabs_Add)) {
            ImGui.OpenPopup(addTabPopup);
        }

        if (ImGui.BeginPopup(addTabPopup)) {
            if (ImGui.Selectable(Language.Options_Tabs_NewTab)) {
                Mutable.Tabs.Add(new Tab());
            }

            ImGui.Separator();

            if (ImGui.Selectable(string.Format(Language.Options_Tabs_Preset, Language.Tabs_Presets_General))) {
                Mutable.Tabs.Add(TabsUtil.VanillaGeneral);
            }

            if (ImGui.Selectable(string.Format(Language.Options_Tabs_Preset, Language.Tabs_Presets_Event))) {
                Mutable.Tabs.Add(TabsUtil.VanillaEvent);
            }

            ImGui.EndPopup();
        }

        var toRemove = -1;
        var doOpens = _toOpen > -2;
        for (var i = 0; i < Mutable.Tabs.Count; i++) {
            var tab = Mutable.Tabs[i];

            if (doOpens) {
                ImGui.SetNextItemOpen(i == _toOpen);
            }

            if (ImGui.TreeNodeEx($"{tab.Name}###tab-{i}")) {
                ImGui.PushID($"tab-{i}");

                if (ImGuiUtil.IconButton(FontAwesomeIcon.TrashAlt, tooltip: Language.Options_Tabs_Delete)) {
                    toRemove = i;
                    _toOpen = -1;
                }

                ImGui.SameLine();

                if (ImGuiUtil.IconButton(FontAwesomeIcon.ArrowUp, tooltip: Language.Options_Tabs_MoveUp) && i > 0) {
                    (Mutable.Tabs[i - 1], Mutable.Tabs[i]) = (Mutable.Tabs[i], Mutable.Tabs[i - 1]);
                    _toOpen = i - 1;
                }

                ImGui.SameLine();

                if (ImGuiUtil.IconButton(FontAwesomeIcon.ArrowDown, tooltip: Language.Options_Tabs_MoveDown) && i < Mutable.Tabs.Count - 1) {
                    (Mutable.Tabs[i + 1], Mutable.Tabs[i]) = (Mutable.Tabs[i], Mutable.Tabs[i + 1]);
                    _toOpen = i + 1;
                }

                ImGui.InputText(Language.Options_Tabs_Name, ref tab.Name, 512, ImGuiInputTextFlags.EnterReturnsTrue);
                ImGui.Checkbox(Language.Options_Tabs_ShowTimestamps, ref tab.DisplayTimestamp);
                ImGui.Checkbox(Language.Options_Tabs_PopOut, ref tab.PopOut);
                if (tab.PopOut) {
                    ImGui.Checkbox(Language.Options_Tabs_IndependentOpacity, ref tab.IndependentOpacity);
                    if (tab.IndependentOpacity) {
                        ImGuiUtil.DragFloatVertical(Language.Options_Tabs_Opacity, ref tab.Opacity, 0.25f, 0f, 100f, $"{tab.Opacity:N2}%%", ImGuiSliderFlags.AlwaysClamp);
                    }
                }

                if (ImGuiUtil.BeginComboVertical(Language.Options_Tabs_UnreadMode, tab.UnreadMode.Name())) {
                    foreach (var mode in Enum.GetValues<UnreadMode>()) {
                        if (ImGui.Selectable(mode.Name(), tab.UnreadMode == mode)) {
                            tab.UnreadMode = mode;
                        }

                        if (mode.Tooltip() is { } tooltip && ImGui.IsItemHovered()) {
                            ImGui.BeginTooltip();
                            ImGui.TextUnformatted(tooltip);
                            ImGui.EndTooltip();
                        }
                    }

                    ImGui.EndCombo();
                }

                var input = tab.Channel?.ToChatType().Name() ?? Language.Options_Tabs_NoInputChannel;
                if (ImGuiUtil.BeginComboVertical(Language.Options_Tabs_InputChannel, input)) {
                    if (ImGui.Selectable(Language.Options_Tabs_NoInputChannel, tab.Channel == null)) {
                        tab.Channel = null;
                    }

                    foreach (var channel in Enum.GetValues<InputChannel>()) {
                        if (ImGui.Selectable(channel.ToChatType().Name(), tab.Channel == channel)) {
                            tab.Channel = channel;
                        }
                    }

                    ImGui.EndCombo();
                }

                if (ImGui.TreeNodeEx(Language.Options_Tabs_Channels)) {
                    foreach (var (header, types) in ChatTypeExt.SortOrder) {
                        if (ImGui.TreeNodeEx(header + $"##{i}")) {
                            foreach (var type in types) {
                                if (type.IsGm()) {
                                    continue;
                                }

                                var enabled = tab.ChatCodes.ContainsKey(type);
                                if (ImGui.Checkbox($"##{type.Name()}-{i}", ref enabled)) {
                                    if (enabled) {
                                        tab.ChatCodes[type] = ChatSourceExt.All;
                                    } else {
                                        tab.ChatCodes.Remove(type);
                                    }
                                }

                                ImGui.SameLine();

                                if (type.HasSource()) {
                                    if (ImGui.TreeNodeEx($"{type.Name()}##{i}")) {
                                        tab.ChatCodes.TryGetValue(type, out var sourcesEnum);
                                        var sources = (uint) sourcesEnum;

                                        foreach (var source in Enum.GetValues<ChatSource>()) {
                                            if (ImGui.CheckboxFlags(source.Name(), ref sources, (uint) source)) {
                                                tab.ChatCodes[type] = (ChatSource) sources;
                                            }
                                        }

                                        ImGui.TreePop();
                                    }
                                } else {
                                    ImGui.TextUnformatted(type.Name());
                                }
                            }

                            ImGui.TreePop();
                        }
                    }

                    ImGui.TreePop();
                }

                if (Plugin.ExtraChat.ChannelNames.Count > 0 && ImGui.TreeNodeEx(Language.Options_Tabs_ExtraChatChannels)) {
                    ImGui.Checkbox(Language.Options_Tabs_ExtraChatAll, ref tab.ExtraChatAll);

                    ImGui.Separator();

                    if (tab.ExtraChatAll) {
                        ImGui.BeginDisabled();
                    }

                    foreach (var (id, name) in Plugin.ExtraChat.ChannelNames) {
                        var enabled = tab.ExtraChatChannels.Contains(id);
                        if (!ImGui.Checkbox($"{name}##ec-{id}", ref enabled)) {
                            continue;
                        }

                        if (enabled) {
                            tab.ExtraChatChannels.Add(id);
                        } else {
                            tab.ExtraChatChannels.Remove(id);
                        }
                    }

                    if (tab.ExtraChatAll) {
                        ImGui.EndDisabled();
                    }

                    ImGui.TreePop();
                }

                ImGui.TreePop();

                ImGui.PopID();
            }
        }

        if (toRemove > -1) {
            Mutable.Tabs.RemoveAt(toRemove);
        }

        if (doOpens) {
            _toOpen = -2;
        }
    }
}
