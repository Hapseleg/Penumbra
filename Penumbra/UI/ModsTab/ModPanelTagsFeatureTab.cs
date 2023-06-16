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
using Lumina.Excel.GeneratedSheets;
using Dalamud.Interface;

namespace Penumbra.UI.ModsTab;

public class ModPanelTagsFeatureTab : ITab
{

    private readonly ModFileSystemSelector _selector;
    private readonly TutorialService _tutorial;
    private readonly ModManager _modManager;
    private readonly TagButtons _localTags = new();
    //Contains the tags
    private List<(string Name, List<string> Tags)> _tagsList = new();

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
        //int cur = 0;
        //ImGui.ListBox("test", ref cur, _tagsList[0].Tags.ToArray(), _tagsList[0].Tags.Count);

        //if (ImGui.TreeNode("Trees"))
        //{
        //    //IMGUI_DEMO_MARKER("Widgets/Trees/Basic trees");
        //    if (ImGui.TreeNode("Basic trees"))
        //    {
        //        for (int i = 0; i < 5; i++)
        //        {
        //            // Use SetNextItemOpen() so set the default state of a node to be open. We could
        //            // also use TreeNodeEx() with the ImGuiTreeNodeFlags_DefaultOpen flag to achieve the same thing!
        //            if (i == 0)
        //                ImGui.SetNextItemOpen(true, ImGuiCond.Once);

        //            if (ImGui.TreeNode(i,"Child %d"))
        //            {
        //                ImGui.Text("blah blah");
        //                ImGui.SameLine();
        //                if (ImGui.SmallButton("button")) { }
        //                ImGui.TreePop();
        //            }
        //        }
        //        ImGui.TreePop();
        //    }
        //}





        //-------------------------------------------------- Real stuff
        //Maybe remove this from the description tab?
        var tagIdx = _localTags.Draw("Local Tags: ",
            "Custom tags you can set personally that will not be exported to the mod data but only set for you.\n"
          + "If the mod already contains a local tag in its own tags, the local tag will be ignored.", _selector.Selected!.LocalTags,
            out var editedTag, false);
        _tutorial.OpenTutorial(BasicTutorialSteps.Tags);
        if (tagIdx >= 0)
            _modManager.DataEditor.ChangeLocalTag(_selector.Selected!, tagIdx, editedTag);


        //Adds the buttons, right now the tags are just in a List that contains List<string>, I'd properly move the tags to a json file so its easier to add and remove tags, can also add a function so the users can add tags themselves
        DrawButtons(_tagsList);

    }


    private void DrawButtons(List<(string Name, List<string> Tags)> tagList)
    {
        foreach (var (name, tags) in tagList)
        {
            ImGui.TextUnformatted(name);
            Console.WriteLine($"Name: {name}");

            foreach (var tag in tags)
            {
                if (!_selector.Selected!.LocalTags.Contains(tag))
                {
                    //Clicking the button adds the tag to the localTags list
                    if (ImGui.Button(tag))
                    {
                        AddTag(tag, _selector.Selected!.LocalTags.Count);
                    }
                }
                else
                {
                    using var color = ImRaii.PushColor(ImGuiCol.Button, ColorId.SelectedCollection.Value());
                    //var color = ImRaii.PushColor(ImGuiCol.Button, ColorId.SelectedCollection.Value());
                    //Clicking the button removes the tag from the localTags list
                    if (ImGui.Button(tag))
                    {
                        RemoveTag(_selector.Selected!.LocalTags.IndexOf(tag));
                    }
                    //color.Pop();
                }
                ImGui.SameLine();
            }
            ImGui.Separator();
            Console.WriteLine();
        }
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

    //This is just a hacky test
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
        List<string> gearStyles = new List<string>
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

        _tagsList.Add(("Type", tagTypes));
        _tagsList.Add(("Genre",tagGenre));
        _tagsList.Add(("Gear Type",gearTagTypes));
        _tagsList.Add(("Category",gearCategory));
        _tagsList.Add(("Style",gearStyles));
        _tagsList.Add(("Material",gearMaterial));
    }
}
