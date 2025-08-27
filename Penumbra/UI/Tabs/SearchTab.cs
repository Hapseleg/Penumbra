using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud.Bindings.ImGui;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;
using OtterGui.Services;
using OtterGui.Text;
using OtterGui.Widgets;
using PeNet.Header.Resource;
using Penumbra.Api.Enums;
using Penumbra.Communication;
using Penumbra.GameData.Data;
using Penumbra.Mods;
using Penumbra.Mods.Manager;
using Penumbra.Services;
using Penumbra.UI.Classes;
using Penumbra.UI.ModsTab;

namespace Penumbra.UI.Tabs;

public class SearchTab : ITab, IDisposable, IUiService
{
    private readonly ModManager          _modManager;
    private readonly ChangedItemDrawer   _drawer;
    private readonly CommunicatorService _communicator;
    private readonly PredefinedTagManager _predefinedTags;

    private readonly SearchFilter _filter;
    private          uint         _enabledColor;
    private          uint         _disabledColor;

    public SearchTab(ModManager modManager, ChangedItemDrawer drawer, CommunicatorService communicator, PredefinedTagManager predefinedTags)
    {
        _modManager     = modManager;
        _drawer         = drawer;
        _communicator   = communicator;
        _predefinedTags = predefinedTags;
        _filter         = new SearchFilter(drawer);
    }

    public void Dispose()
    { }

    public ReadOnlySpan<byte> Label
        => "Search"u8;

    public void DrawContent()
    {
        var width = ImGui.GetContentRegionAvail().X;
        _filter.Draw(width);
        _drawer.DrawTypeFilter();
        DrawTags();

        using var child = ImRaii.Child("##searchChild", -Vector2.One);
        if (!child)
            return;

        var       lineHeight = ImGui.GetTextLineHeightWithSpacing();
        var       skips      = ImGuiClip.GetNecessarySkips(lineHeight);
        using var list       = ImRaii.Table("##searchMods", 1, ImGuiTableFlags.RowBg, -Vector2.One);
        if (!list)
            return;

        ImUtf8.TableSetupColumn("Mods"u8, ImGuiTableColumnFlags.WidthStretch);
        var rest = ImGuiClip.FilteredClippedDraw(_modManager, skips, mod => _filter.ApplyFilter(mod), DrawMod);
        ImGuiClip.DrawEndDummy(rest, lineHeight);
    }

    // var ret = false;
    //     _enabledColor        = ColorId.PredefinedTagAdd.Value();
    //     _disabledColor       = ColorId.PredefinedTagRemove.Value();
    //     var (edited, others) = editLocal ? (localTags, modTags) : (modTags, localTags);
    //     foreach (var (tag, idx) in _predefinedTags.Keys.WithIndex())
    //     {
    //         var tagIdx  = edited.IndexOf(tag);
    //         var inOther = tagIdx < 0 && others.IndexOf(tag) >= 0;
    //         if (DrawColoredButton(tag, idx, tagIdx, inOther))
    //         {
    //             (changedTag, changedIndex) = tagIdx >= 0 ? (string.Empty, tagIdx) : (tag, edited.Count);
    //             ret                        = true;
    //         }

    //         ImGui.SameLine();
    //     }
    // private bool DrawColoredButton(string buttonLabel)
    // {
    //     using var id          = ImRaii.PushId(index);
    //     var       buttonWidth = ImGui.CalcTextSize(buttonLabel).X + 2 * ImGui.GetStyle().FramePadding.X;
    //     // Prevent adding a new tag past the right edge of the popup
    //     if (buttonWidth + ImGui.GetStyle().ItemSpacing.X >= ImGui.GetContentRegionAvail().X)
    //         ImGui.NewLine();

    //     bool ret;
    //     using (ImRaii.Disabled(inOther))
    //     {
    //         using var color = ImRaii.PushColor(ImGuiCol.Button, tagIdx >= 0 || inOther ? _disabledColor : _enabledColor);
    //         ret = ImGui.Button(buttonLabel);
    //     }

    //     if (inOther && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
    //         ImGui.SetTooltip("This tag is already present in the other set of tags.");


    //     return ret;
    // }

    private void DrawTags()
    {
        if (_predefinedTags.Count == 0)
            return;

        var style = ImGui.GetStyle();
        var availableWidth = ImGui.GetContentRegionAvail().X;
        _enabledColor = ColorId.PredefinedTagAdd.Value();
        _disabledColor = ColorId.PredefinedTagRemove.Value();
        using (var _ = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(style.ItemSpacing.X / 2, style.ItemSpacing.Y)))
        {
            foreach (var tag in _predefinedTags)
            {
                var buttonWidth = ImUtf8.CalcTextSize(tag).X + style.FramePadding.X * 2;
                if (ImGui.GetCursorPosX() > 0 && ImGui.GetCursorPosX() + buttonWidth > availableWidth)
                    ImGui.NewLine();

                var isContained = _filter.TagFilters.Contains(tag, StringComparer.OrdinalIgnoreCase);
                using var color = ImRaii.PushColor(ImGuiCol.Button, !isContained ? _disabledColor : _enabledColor);
                if (ImGui.Button(tag))
                {
                    if (isContained)
                        _filter.RemoveTagFromFilter(tag);
                    else
                        _filter.AddTagToFilter(tag);
                }

                ImGui.SameLine();
            }
        }

        ImGui.NewLine();
        ImGui.Separator();
    }

    private void DrawMod(Mod mod)
    {
        ImGui.TableNextColumn();
        if (ImUtf8.Selectable(mod.Name.Text, false, ImGuiSelectableFlags.None, new Vector2(0, ImGui.GetTextLineHeightWithSpacing()))
         && ImGui.GetIO().KeyCtrl)
            _communicator.SelectTab.Invoke(TabType.Mods, mod);

        if (ImGui.IsItemHovered())
        {
            using var tooltip = ImRaii.Tooltip();
            ImUtf8.Text("Hold Control and click to jump to mod."u8);
        }
    }

    private class SearchFilter
    {
        private readonly ChangedItemDrawer _drawer;
        private          string            _input      = string.Empty;
        private          LowerString       _textFilter = LowerString.Empty;
        private          string[]          _tagFilters = Array.Empty<string>();
        private          int               _filterMode = -1;
        private static readonly Regex TagRegex = new(@"tag:""([^""]*)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public IReadOnlyList<string> TagFilters => _tagFilters;

        public bool IsEmpty
            => _filterMode == -1;

        public SearchFilter(ChangedItemDrawer drawer)
            => _drawer = drawer;

        public void AddTagToFilter(string tag)
        {
            var tagFilter = $"tag:\"{tag}\"";
            if (_input.Length == 0)
                _input = tagFilter;
            else if (!_input.Contains(tagFilter, StringComparison.OrdinalIgnoreCase))
                _input += $" {tagFilter}";

            UpdateFilters();
        }

        public void RemoveTagFromFilter(string tag)
        {
            var newTags = _tagFilters.Where(t => !t.Equals(tag, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (newTags.Length == _tagFilters.Length)
                return;

            var text = TagRegex.Replace(_input, string.Empty).Trim();
            _tagFilters = newTags;
            var tagString = string.Join(" ", _tagFilters.Select(t => $"tag:\"{t}\""));
            _input = string.IsNullOrEmpty(text) ? tagString : $"{text} {tagString}";
            _textFilter = new LowerString(text);
            _filterMode = _textFilter.IsEmpty && _tagFilters.Length == 0 ? -1 : 0;
        }

        private string Tooltip
            => "Filter by item name, mod name, or tags using 'tag:\"tag name\"'...";

        private void UpdateFilters()
        {
            if (_input.Length == 0)
            {
                _textFilter = LowerString.Empty;
                _tagFilters = Array.Empty<string>();
                _filterMode = -1;
                return;
            }

            var tags = new List<string>();
            var text = TagRegex.Replace(_input, match =>
            {
                tags.Add(match.Groups[1].Value);
                return string.Empty;
            }).Trim();

            _tagFilters = tags.ToArray();
            _textFilter = new LowerString(text);
            _filterMode = _textFilter.IsEmpty && _tagFilters.Length == 0 ? -1 : 0;
        }

        public bool ApplyFilter(Mod mod)
        {
            if (_tagFilters.Length > 0 && !_tagFilters.All(t =>
                    mod.ModTags.Concat(mod.LocalTags).Any(mt => mt.Equals(t, StringComparison.OrdinalIgnoreCase))))
                return false;

            var modNameMatches = !_textFilter.IsEmpty && mod.Name.Contains(_textFilter);

            var matchingItems = mod.ChangedItems
                .Where(p => _drawer.FilterChangedItem(p.Key, p.Value, LowerString.Empty))
                .ToList();

            if (matchingItems.Count == 0)
                return false;

            if (_textFilter.IsEmpty)
                return true;

            if (modNameMatches)
                return true;

            return matchingItems.Any(p => p.Value.ToName(p.Key).Contains(_textFilter, StringComparison.OrdinalIgnoreCase));
        }

        public bool Draw(float width)
        {
            ImGui.SetNextItemWidth(width);
            var change = ImGui.InputTextWithHint("##filterInput"u8, "Filter..."u8, ref _input, 256);
            ImGuiUtil.HoverTooltip(Tooltip);
            if (!change)
                return false;

            UpdateFilters();
            return true;
        }
    }
}