﻿using System;
using Dalamud.Interface;
using ImGuiNET;
using OtterGui.Raii;
using OtterGui;
using OtterGui.Widgets;
using Penumbra.Mods.Manager;
using System.Numerics;
using System.Collections.Generic;
using Dalamud.Logging;
using System.Linq;
using Penumbra.Collections;
using Penumbra.UI.Classes;

namespace Penumbra.UI.ModsTab;

public class ModPanelTagsFeatureTab : ITab
{
    private readonly ModFileSystemSelector _selector;
    private readonly TutorialService _tutorial;
    private readonly ModManager _modManager;
    private readonly TagButtons _localTags = new();
    private List<List<string>> _tagsList = new();
    //private Dictionary<string, bool> _tagDict = new();
    private List<string> _selectedTags = new();

    public ModPanelTagsFeatureTab(ModFileSystemSelector selector, TutorialService tutorial, ModManager modManager)
    {
        _selector = selector;
        _tutorial = tutorial;
        _modManager = modManager;

        AddTagsToList();
    }

    public ReadOnlySpan<byte> Label
        => "Tag Test Feature"u8;

    public void DrawContent()
    {

        var tagIdx = _localTags.Draw("Local Tags: ",
            "Custom tags you can set personally that will not be exported to the mod data but only set for you.\n"
          + "If the mod already contains a local tag in its own tags, the local tag will be ignored.", _selector.Selected!.LocalTags,
            out var editedTag, false);
        _tutorial.OpenTutorial(BasicTutorialSteps.Tags);
        if (tagIdx >= 0)
            _modManager.DataEditor.ChangeLocalTag(_selector.Selected!, tagIdx, editedTag);


        DrawButtons(_tagsList[0], "Type");
        DrawButtons(_tagsList[1], "Genre");
        DrawButtons(_tagsList[2], "Gear Types");
        DrawButtons(_tagsList[3], "Gear Category");
        DrawButtons(_tagsList[4], "Gear Styles");
        DrawButtons(_tagsList[5], "Gear Material");

        //ImGui.Separator();
        //if (ImGui.Button("Save tags"))
        //{
        //    for (var i = 0; i < _selectedTags.Count; i++)
        //    {
        //        _modManager.DataEditor.ChangeLocalTag(_selector.Selected!, i, _selectedTags[i]);
        //    }
        //    _selectedTags.Clear();
        //}
        //ImGui.SameLine();
        //if (ImGui.Button("Print List"))
        //    foreach (var s in _selectedTags)
        //        PluginLog.Debug(s);

        //int current = 0;
        //ImGui.ListBox("Selected Tags", ref current, _selectedTags.ToArray(), _selectedTags.Count);


    }

    private void DrawButtons(List<string> tags, string title)
    {
        ImGui.Text(title);
        
        foreach (var tag in tags)
        {
            if (!_selector.Selected!.LocalTags.Contains(tag))
            {
                
                if (ImGui.Button(tag))
                {
                    AddTag(tag, _selector.Selected!.LocalTags.Count);
                }
            }
            else
            {
                var color = ImRaii.PushColor(ImGuiCol.Button, ColorId.SelectedCollection.Value());
                if (ImGui.Button(tag))
                {
                    RemoveTag(_selector.Selected!.LocalTags.IndexOf(tag));
                }
                color.Pop();
            }
            ImGui.SameLine();
        }
        ImGui.Separator();
    }

    private void ColorButton()
    {

    }

    private void AddTag(string tag, int tagIdx)
    {
        _modManager.DataEditor.ChangeLocalTag(_selector.Selected!, tagIdx, tag);
    }
    private void RemoveTag(int tagIdx)
    {
        _modManager.DataEditor.ChangeLocalTag(_selector.Selected!, tagIdx, "");
    }

    private static bool DebugTestButton(string name)
    {
        ImGui.Button("TEST: " + name);
        return ImGui.IsItemClicked();
    }

    private void AddTagsToList()
    {
        List<string> tagTypes = new List<string>
        {
            "NSFW",
            "SFW"
        };
        List<string> tagGenre = new List<string>
        {
            "Gear",
            "Animation"
        };
        List<string> gearTagTypes = new List<string>
        {
            "Head",
            "Chest",
            "Fullbody",
            "Hands",
            "Legs",
            "Feet",
            "Necklace",
            "Earring",
            "Bracelet",
            "Ring",
            "Weapon",

        };
        List<string> gearCategory = new List<string>
        {
            "Underwear",
            "Costume",
            "Swimwear",
            "Dress",
            "Lingerie",
            "Bondage"
        };
        List<string> gearTags = new List<string>
        {
            "Cute",
            "Elegant",
            "Scary",
            "Casual"
        };
        List<string> gearMaterial = new List<string>
        {
            "Cotton",
            "Leather",
            "Metal",
            "Latex",
        };

        _tagsList.Add(tagTypes);
        _tagsList.Add(tagGenre);
        _tagsList.Add(gearTagTypes);
        _tagsList.Add(gearCategory);
        _tagsList.Add(gearTags);
        _tagsList.Add(gearMaterial);
    }



    private void tester()
    {
        ImGuiUtil.DrawTextButton("test", new Vector2(150 * UiHelpers.Scale, 0), 1);
        PluginLog.Debug("tester");
    }


}
