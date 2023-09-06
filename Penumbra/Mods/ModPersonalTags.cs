using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Penumbra.Collections;
using Penumbra.Services;

namespace Penumbra.Mods;
public class ModPersonalTags : ISavable
{
    public List<(string Category, List<string> Tags)> PersonalTags { get; set; }

    public ModPersonalTags()
    {
        //TODO load personal tags here?
        PersonalTags = new List<(string Category, List<string> Tags)> ();
        //PersonalTags.Add(("CategoryTest", new List<string>() { "Tag1","Tagtest2"}));
    }

    public void Save(StreamWriter writer)
    {
        using var j = new JsonTextWriter(writer);
        j.Formatting = Formatting.Indented;
        var x = JsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented });
        j.WriteStartObject();
        foreach (var (category, tags) in PersonalTags)
        {
            j.WritePropertyName(category);
            j.WriteStartArray();
            foreach (var tag in tags)
            {
                j.WriteValue(tag);
            }
            j.WriteEnd();
        }
        j.WriteEndObject();
    }

    public string ToFilename(FilenameService fileNames)
        => fileNames.PersonalTagsFile;
}
