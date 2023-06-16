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
using System.Diagnostics;
using Swan.Logging;

namespace Penumbra.UI.ModsTab;

public class ModPanelTagsFeatureTab : ITab
{

    private readonly ModFileSystemSelector _selector;
    private readonly TutorialService _tutorial;
    private readonly ModManager _modManager;
    private readonly TagButtons _localTags = new();
    //Contains the tags
    private List<(string Name, List<string> Tags)> _tagsList = new();

    private List<string> _personalTagsTest = new();
    private string newTag = string.Empty;

    public ModPanelTagsFeatureTab(ModFileSystemSelector selector, TutorialService tutorial, ModManager modManager)
    {
        _selector = selector;
        _tutorial = tutorial;
        _modManager = modManager;

        //_personalTagsTest.Add("abe");
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

        ImGui.Separator();

        //int cur = 0;
        //ImGui.ListBox("test", ref cur, _tagsList[0].Tags.ToArray(), _tagsList[0].Tags.Count);

        if (ImGui.TreeNode("Personal Tags"))
        {
            AddContextForLeafs();
            //if (ImGui.BeginPopupContextItem())
            //{

            //    ImGui.Text("Add new Category.");
            //    ImGui.InputText("##edit", ref newTag, 128);

            //    if (ImGui.Button("Add"))
            //    {
            //        _personalTagsTest.Add(newTag);
            //        newTag = string.Empty;
            //        ImGui.EndPopup();
            //    }

            //    ImGui.EndPopup();
            //}

            for (var i = 0; i < _personalTagsTest.Count; i++)
            {
                

                if (ImGui.TreeNode(_personalTagsTest[i], _personalTagsTest[i]))
                {
                    //AddContextForLeafs();
                    //PluginLog.Debug(_personalTagsTest[i].ToString());

                    ImGui.TreePop();
                }
            }

            //foreach (var personalTag in _personalTagsTest)
            //{
            //    if (ImGui.TreeNode(personalTag))
            //    {
            //        PluginLog.Debug(_personalTagsTest.Count.ToString());

            //        ImGui.TreePop();
            //    }
            //}

            ImGui.TreePop();
        }

        ImGui.Separator();

        if (ImGui.TreeNode("Default Tags"))
        {
            foreach (var (name, tags) in _tagsList)
            {
                //if (ImGui.BeginPopupContextItem(name))
                //{
                //    ImGui.Text(name);
                //    ImGui.EndPopup();
                //}
                if (ImGui.TreeNode(name))
                {
                    //bool[] selection = new bool[tags.Count];
                    //foreach (var tag in tags)
                    //{
                    //    bool containsTag = _selector.Selected!.LocalTags.Contains(tag);

                    //    //selection[selection.Length] = containsTag;
                    //    if (ImGui.Selectable(tag, containsTag))
                    //    {
                    //        if (containsTag)
                    //            RemoveTag(_selector.Selected!.LocalTags.IndexOf(tag));
                    //        else
                    //            AddTag(tag);
                    //    }
                    //}
                    foreach (var tag in tags)
                    {

                        if (!_selector.Selected!.LocalTags.Contains(tag))
                        {
                            //Clicking the button adds the tag to the localTags list
                            if (ImGui.Button(tag))
                            {
                                AddTag(tag);
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
                        //ImGui.Unindent(ImGui.GetTreeNodeToLabelSpacing());
                        ImGui.SameLine();
                        
                        
                    }
                    ImGui.NewLine();
                    ImGui.TreePop();
                }
            }

        }





        //-------------------------------------------------- Real stuff
        //Maybe remove this from the description tab?
        //var tagIdx = _localTags.Draw("Local Tags: ",
        //    "Custom tags you can set personally that will not be exported to the mod data but only set for you.\n"
        //  + "If the mod already contains a local tag in its own tags, the local tag will be ignored.", _selector.Selected!.LocalTags,
        //    out var editedTag, false);
        //_tutorial.OpenTutorial(BasicTutorialSteps.Tags);
        //if (tagIdx >= 0)
        //    _modManager.DataEditor.ChangeLocalTag(_selector.Selected!, tagIdx, editedTag);


        ////Adds the buttons, right now the tags are just in a List that contains List<string>, I'd properly move the tags to a json file so its easier to add and remove tags, can also add a function so the users can add tags themselves
        //DrawButtons(_tagsList);

    }

    private void AddContextForLeafs()
    {
        if (ImGui.BeginPopupContextItem())
        {

            ImGui.Text("Add new Category.");
            ImGui.InputText("##edit", ref newTag, 128);

            if (ImGui.Button("Add"))
            {
                _personalTagsTest.Add(newTag);
                newTag = string.Empty;
                ImGui.EndPopup();
            }

            ImGui.EndPopup();
        }
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
                        AddTag(tag);
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
    private void AddTag(string tag)
    {
        _modManager.DataEditor.ChangeLocalTag(_selector.Selected!, _selector.Selected!.LocalTags.Count, tag);
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
        _tagsList.Add(("Genre", tagGenre));
        _tagsList.Add(("Gear Type", gearTagTypes));
        _tagsList.Add(("Category", gearCategory));
        _tagsList.Add(("Style", gearStyles));
        _tagsList.Add(("Material", gearMaterial));
    }
}
