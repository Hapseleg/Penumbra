using System;
using ImGuiNET;
using OtterGui.Raii;
using OtterGui;
using OtterGui.Widgets;
using Penumbra.Mods.Manager;
using System.Numerics;
using System.Collections.Generic;
using Dalamud.Logging;
using System.Linq;
using Penumbra.UI.Classes;

namespace Penumbra.UI.ModsTab;

public class ModPanelTagsFeatureTab : ITab
{
    
    private readonly ModFileSystemSelector _selector;
    private readonly TutorialService _tutorial;
    private readonly ModManager _modManager;
    private readonly TagButtons _localTags = new();

    //Contains the tags
    private List<List<string>> _tagsList = new();
    //private Dictionary<string, bool> _tagDict = new();
    //private List<string> _selectedTags = new();

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
        //Maybe remove this from the description tab?
        var tagIdx = _localTags.Draw("Local Tags: ",
            "Custom tags you can set personally that will not be exported to the mod data but only set for you.\n"
          + "If the mod already contains a local tag in its own tags, the local tag will be ignored.", _selector.Selected!.LocalTags,
            out var editedTag, false);
        _tutorial.OpenTutorial(BasicTutorialSteps.Tags);
        if (tagIdx >= 0)
            _modManager.DataEditor.ChangeLocalTag(_selector.Selected!, tagIdx, editedTag);


        //Adds the buttons, right now the tags are just in a List that contains List<string>, I'd properly move the tags to a json file so its easier to add and remove tags, can also add a function so the users can add tags themselves
        DrawButtons(_tagsList[0], "Type");
        DrawButtons(_tagsList[1], "Genre");
        DrawButtons(_tagsList[2], "Gear Types");
        DrawButtons(_tagsList[3], "Gear Category");
        DrawButtons(_tagsList[4], "Gear Styles");
        DrawButtons(_tagsList[5], "Gear Material");

    }


    private void DrawButtons(List<string> tags, string title)
    {
        ImGui.Text(title);
        //byte buttonsOnSameLine = 0;
        foreach (var tag in tags)
        {
            //Checks if the mod does not have the tag
            if (!_selector.Selected!.LocalTags.Contains(tag))
            {
                //Clicking the button adds the tag to the localTags list
                if (ImGui.Button(tag))
                {
                    AddTag(tag, _selector.Selected!.LocalTags.Count);
                }
            }
            //if it does we want to color the button so you can see its added, im pretty sure im using this wrong...?
            else
            {
                var color = ImRaii.PushColor(ImGuiCol.Button, ColorId.SelectedCollection.Value());
                //Clicking the button removes the tag from the localTags list
                if (ImGui.Button(tag))
                {
                    RemoveTag(_selector.Selected!.LocalTags.IndexOf(tag));
                }
                color.Pop();
            }
            ImGui.SameLine();
            //buttonsOnSameLine++;
            //if (buttonsOnSameLine >= 8)
            //{
            //    ImGui.NewLine();
            //    buttonsOnSameLine = 0;
            //}
            //else
            //    ImGui.SameLine();
        }
        ImGui.Separator();
    }

    //Adds tag to the mods json
    private void AddTag(string tag, int tagIdx)
    {
        _modManager.DataEditor.ChangeLocalTag(_selector.Selected!, tagIdx, tag);
    }

    //Removes the tag by passing an empty string
    private void RemoveTag(int tagIdx)
    {
        _modManager.DataEditor.ChangeLocalTag(_selector.Selected!, tagIdx, "");
    }

    private static bool DebugTestButton(string name)
    {
        ImGui.Button("TEST: " + name);
        return ImGui.IsItemClicked();
    }

    //This is just a hacky test
    private void AddTagsToList()
    {
        List<string> tagTypes = new List<string>
        {
            "NSFW",
            "SFW",
            "Dyable"
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
            "Bondage",
            "Costume",
            "Dress",
            "Garterbelt",
            "Heels",
            "Lingerie",
            "Sandals",
            "Skirt",
            "Stockings",
            "Swimwear",
            "Underwear",
        };
        List<string> gearTags = new List<string>
        {
            "BDSM",
            "Casual",
            "Chains",
            "Cute",
            "Daily",
            "Eastern",
            "Elegant",
            "Fishnet",
            "Party",
            "Punk",
            "Scary",
            "See-through",
            "Sexy",
            "Short/revealing",
            "Special",
            "Suggestive",
            "Tight",

        };
        List<string> gearMaterial = new List<string>
        {
            "Cotton",
            "Leather",
            "Latex",
            "Metal",
            "Spandex"
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
