using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using OtterGui;
using Penumbra.Api.Enums;
using Penumbra.GameData;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Penumbra.String;
using Penumbra.String.Classes;
using Penumbra.UI;
using static Penumbra.Interop.Structs.StructExtensions;

namespace Penumbra.Interop.ResourceTree;

internal record GlobalResolveContext(IObjectIdentifier Identifier, TreeBuildCache TreeBuildCache,
    int Skeleton, bool WithUiData)
{
    public readonly Dictionary<(Utf8GamePath, nint), ResourceNode> Nodes = new(128);

    public ResolveContext CreateContext(EquipSlot slot, CharacterArmor equipment)
        => new(this, slot, equipment);
}

internal record ResolveContext(GlobalResolveContext Global, EquipSlot Slot, CharacterArmor Equipment)
{
    private static readonly ByteString ShpkPrefix = ByteString.FromSpanUnsafe("shader/sm5/shpk"u8, true, true, true);

    private unsafe ResourceNode? CreateNodeFromShpk(ShaderPackageResourceHandle* resourceHandle, ByteString gamePath)
    {
        if (resourceHandle == null)
            return null;
        if (gamePath.IsEmpty)
            return null;
        if (!Utf8GamePath.FromByteString(ByteString.Join((byte)'/', ShpkPrefix, gamePath), out var path, false))
            return null;

        return GetOrCreateNode(ResourceType.Shpk, (nint)resourceHandle->ShaderPackage, &resourceHandle->ResourceHandle, path);
    }

    private unsafe ResourceNode? CreateNodeFromTex(TextureResourceHandle* resourceHandle, ByteString gamePath, bool dx11)
    {
        if (resourceHandle == null)
            return null;

        if (dx11)
        {
            var lastDirectorySeparator = gamePath.LastIndexOf((byte)'/');
            if (lastDirectorySeparator == -1 || lastDirectorySeparator > gamePath.Length - 3)
                return null;

            if (gamePath[lastDirectorySeparator + 1] != (byte)'-' || gamePath[lastDirectorySeparator + 2] != (byte)'-')
            {
                Span<byte> prefixed = stackalloc byte[gamePath.Length + 2];
                gamePath.Span[..(lastDirectorySeparator + 1)].CopyTo(prefixed);
                prefixed[lastDirectorySeparator + 1] = (byte)'-';
                prefixed[lastDirectorySeparator + 2] = (byte)'-';
                gamePath.Span[(lastDirectorySeparator + 1)..].CopyTo(prefixed[(lastDirectorySeparator + 3)..]);

                if (!Utf8GamePath.FromSpan(prefixed, out var tmp))
                    return null;

                gamePath = tmp.Path.Clone();
            }
        }
        else
        {
            // Make sure the game path is owned, otherwise stale trees could cause crashes (access violations) or other memory safety issues.
            if (!gamePath.IsOwned)
                gamePath = gamePath.Clone();
        }

        if (!Utf8GamePath.FromByteString(gamePath, out var path))
            return null;

        return GetOrCreateNode(ResourceType.Tex, (nint)resourceHandle->Texture, &resourceHandle->ResourceHandle, path);
    }

    private unsafe ResourceNode GetOrCreateNode(ResourceType type, nint objectAddress, ResourceHandle* resourceHandle,
        Utf8GamePath gamePath)
    {
        if (resourceHandle == null)
            throw new ArgumentNullException(nameof(resourceHandle));

        if (Global.Nodes.TryGetValue((gamePath, (nint)resourceHandle), out var cached))
            return cached;

        return CreateNode(type, objectAddress, resourceHandle, gamePath);
    }

    private unsafe ResourceNode CreateNode(ResourceType type, nint objectAddress, ResourceHandle* resourceHandle,
        Utf8GamePath gamePath, bool autoAdd = true)
    {
        if (resourceHandle == null)
            throw new ArgumentNullException(nameof(resourceHandle));

        var fullPath = Utf8GamePath.FromByteString(GetResourceHandlePath(resourceHandle), out var p) ? new FullPath(p) : FullPath.Empty;

        var node = new ResourceNode(type, objectAddress, (nint)resourceHandle, GetResourceHandleLength(resourceHandle), this)
        {
            GamePath = gamePath,
            FullPath = fullPath,
        };
        if (autoAdd)
            Global.Nodes.Add((gamePath, (nint)resourceHandle), node);

        return node;
    }

    public unsafe ResourceNode? CreateNodeFromEid(ResourceHandle* eid)
    {
        if (eid == null)
            return null;

        var path = Utf8GamePath.Empty; // TODO

        return GetOrCreateNode(ResourceType.Eid, 0, eid, path);
    }

    public unsafe ResourceNode? CreateNodeFromImc(ResourceHandle* imc)
    {
        if (imc == null)
            return null;

        var path = Utf8GamePath.Empty; // TODO

        return GetOrCreateNode(ResourceType.Imc, 0, imc, path);
    }

    public unsafe ResourceNode? CreateNodeFromTex(TextureResourceHandle* tex)
    {
        if (tex == null)
            return null;

        var path = Utf8GamePath.Empty; // TODO

        return GetOrCreateNode(ResourceType.Tex, (nint)tex->Texture, &tex->ResourceHandle, path);
    }

    public unsafe ResourceNode? CreateNodeFromRenderModel(Model* mdl)
    {
        if (mdl == null || mdl->ModelResourceHandle == null)
            return null;

        var path = Utf8GamePath.Empty; // TODO

        if (Global.Nodes.TryGetValue((path, (nint)mdl->ModelResourceHandle), out var cached))
            return cached;

        var node = CreateNode(ResourceType.Mdl, (nint)mdl, &mdl->ModelResourceHandle->ResourceHandle, path, false);

        for (var i = 0; i < mdl->MaterialCount; i++)
        {
            var mtrl     = mdl->Materials[i];
            var mtrlNode = CreateNodeFromMaterial(mtrl);
            if (mtrlNode != null)
            {
                if (Global.WithUiData)
                    mtrlNode.FallbackName = $"Material #{i}";
                node.Children.Add(mtrlNode);
            }
        }

        Global.Nodes.Add((path, (nint)mdl->ModelResourceHandle), node);

        return node;
    }

    private unsafe ResourceNode? CreateNodeFromMaterial(Material* mtrl)
    {
        static ushort GetTextureIndex(Material* mtrl, ushort texFlags, HashSet<uint> alreadyVisitedSamplerIds)
        {
            if ((texFlags & 0x001F) != 0x001F && !alreadyVisitedSamplerIds.Contains(mtrl->Textures[texFlags & 0x001F].Id))
                return (ushort)(texFlags & 0x001F);
            if ((texFlags & 0x03E0) != 0x03E0 && !alreadyVisitedSamplerIds.Contains(mtrl->Textures[(texFlags >> 5) & 0x001F].Id))
                return (ushort)((texFlags >> 5) & 0x001F);
            if ((texFlags & 0x7C00) != 0x7C00 && !alreadyVisitedSamplerIds.Contains(mtrl->Textures[(texFlags >> 10) & 0x001F].Id))
                return (ushort)((texFlags >> 10) & 0x001F);

            return 0x001F;
        }

        static uint? GetTextureSamplerId(Material* mtrl, TextureResourceHandle* handle, HashSet<uint> alreadyVisitedSamplerIds)
            => mtrl->TexturesSpan.FindFirst(p => p.Texture == handle && !alreadyVisitedSamplerIds.Contains(p.Id), out var p)
                ? p.Id
                : null;

        static uint? GetSamplerCrcById(ShaderPackage* shpk, uint id)
            => shpk->SamplersSpan.FindFirst(s => s.Id == id, out var s)
                ? s.CRC
                : null;

        if (mtrl == null || mtrl->MaterialResourceHandle == null)
            return null;

        var path = Utf8GamePath.Empty; // TODO

        var resource = mtrl->MaterialResourceHandle;
        if (Global.Nodes.TryGetValue((path, (nint)resource), out var cached))
            return cached;

        var node = CreateNode(ResourceType.Mtrl, (nint)mtrl, &resource->ResourceHandle, path, false);
        if (node == null)
            return null;

        var shpkNode = CreateNodeFromShpk(resource->ShaderPackageResourceHandle, new ByteString(resource->ShpkName));
        if (shpkNode != null)
        {
            if (Global.WithUiData)
                shpkNode.Name = "Shader Package";
            node.Children.Add(shpkNode);
        }
        var shpkFile = Global.WithUiData && shpkNode != null ? Global.TreeBuildCache.ReadShaderPackage(shpkNode.FullPath) : null;
        var shpk     = Global.WithUiData && shpkNode != null ? (ShaderPackage*)shpkNode.ObjectAddress : null;

        var alreadyProcessedSamplerIds = new HashSet<uint>();
        for (var i = 0; i < resource->TextureCount; i++)
        {
            var texNode = CreateNodeFromTex(resource->Textures[i].TextureResourceHandle, new ByteString(resource->TexturePath(i)),
                resource->Textures[i].IsDX11);
            if (texNode == null)
                continue;

            if (Global.WithUiData)
            {
                string? name = null;
                if (shpk != null)
                {
                    var   index = GetTextureIndex(mtrl, resource->Textures[i].Flags, alreadyProcessedSamplerIds);
                    uint? samplerId;
                    if (index != 0x001F)
                        samplerId = mtrl->Textures[index].Id;
                    else
                        samplerId = GetTextureSamplerId(mtrl, resource->Textures[i].TextureResourceHandle, alreadyProcessedSamplerIds);
                    if (samplerId.HasValue)
                    {
                        alreadyProcessedSamplerIds.Add(samplerId.Value);
                        var samplerCrc = GetSamplerCrcById(shpk, samplerId.Value);
                        if (samplerCrc.HasValue)
                            name = shpkFile?.GetSamplerById(samplerCrc.Value)?.Name ?? $"Texture 0x{samplerCrc.Value:X8}";
                    }
                }

                texNode = texNode.Clone();
                texNode.Name = name ?? $"Texture #{i}";
            }

            node.Children.Add(texNode);
        }

        Global.Nodes.Add((path, (nint)resource), node);

        return node;
    }

    public unsafe ResourceNode? CreateNodeFromPartialSkeleton(PartialSkeleton* sklb)
    {
        if (sklb == null || sklb->SkeletonResourceHandle == null)
            return null;

        var path = Utf8GamePath.Empty; // TODO

        if (Global.Nodes.TryGetValue((path, (nint)sklb->SkeletonResourceHandle), out var cached))
            return cached;

        var node = CreateNode(ResourceType.Sklb, (nint)sklb, (ResourceHandle*)sklb->SkeletonResourceHandle, path, false);
        if (node != null)
        {
            var skpNode = CreateParameterNodeFromPartialSkeleton(sklb);
            if (skpNode != null)
                node.Children.Add(skpNode);
            Global.Nodes.Add((path, (nint)sklb->SkeletonResourceHandle), node);
        }

        return node;
    }

    private unsafe ResourceNode? CreateParameterNodeFromPartialSkeleton(PartialSkeleton* sklb)
    {
        if (sklb == null || sklb->SkeletonParameterResourceHandle == null)
            return null;

        var path = Utf8GamePath.Empty; // TODO

        if (Global.Nodes.TryGetValue((path, (nint)sklb->SkeletonParameterResourceHandle), out var cached))
            return cached;

        var node = CreateNode(ResourceType.Skp, (nint)sklb, (ResourceHandle*)sklb->SkeletonParameterResourceHandle, path, false);
        if (node != null)
        {
            if (Global.WithUiData)
                node.FallbackName = "Skeleton Parameters";
            Global.Nodes.Add((path, (nint)sklb->SkeletonParameterResourceHandle), node);
        }

        return node;
    }

    internal List<Utf8GamePath> FilterGamePaths(IReadOnlyCollection<Utf8GamePath> gamePaths)
    {
        var filtered = new List<Utf8GamePath>(gamePaths.Count);
        foreach (var path in gamePaths)
        {
            // In doubt, keep the paths.
            if (IsMatch(path.ToString().Split('/', StringSplitOptions.RemoveEmptyEntries))
             ?? true)
                filtered.Add(path);
        }

        return filtered;
    }

    private bool? IsMatch(ReadOnlySpan<string> path)
        => SafeGet(path, 0) switch
        {
            "chara" => SafeGet(path, 1) switch
            {
                "accessory" => IsMatchEquipment(path[2..], $"a{Equipment.Set.Id:D4}"),
                "equipment" => IsMatchEquipment(path[2..], $"e{Equipment.Set.Id:D4}"),
                "monster"   => SafeGet(path, 2) == $"m{Global.Skeleton:D4}",
                "weapon"    => IsMatchEquipment(path[2..], $"w{Equipment.Set.Id:D4}"),
                _           => null,
            },
            _ => null,
        };

    private bool? IsMatchEquipment(ReadOnlySpan<string> path, string equipmentDir)
        => SafeGet(path, 0) == equipmentDir
            ? SafeGet(path, 1) switch
            {
                "material" => SafeGet(path, 2) == $"v{Equipment.Variant.Id:D4}",
                _          => null,
            }
            : false;

    internal ResourceNode.UiData GuessModelUIData(Utf8GamePath gamePath)
    {
        var path = gamePath.ToString().Split('/', StringSplitOptions.RemoveEmptyEntries);
        // Weapons intentionally left out.
        var isEquipment = SafeGet(path, 0) == "chara" && SafeGet(path, 1) is "accessory" or "equipment";
        if (isEquipment)
            foreach (var item in Global.Identifier.Identify(Equipment.Set, Equipment.Variant, Slot.ToSlot()))
            {
                var name = Slot switch
                    {
                        EquipSlot.RFinger => "R: ",
                        EquipSlot.LFinger => "L: ",
                        _                 => string.Empty,
                    }
                  + item.Name.ToString();
                return new ResourceNode.UiData(name, ChangedItemDrawer.GetCategoryIcon(item.Name, item));
            }

        var dataFromPath = GuessUIDataFromPath(gamePath);
        if (dataFromPath.Name != null)
            return dataFromPath;

        return isEquipment
            ? new ResourceNode.UiData(Slot.ToName(), ChangedItemDrawer.GetCategoryIcon(Slot.ToSlot()))
            : new ResourceNode.UiData(null,          ChangedItemDrawer.ChangedItemIcon.Unknown);
    }

    internal ResourceNode.UiData GuessUIDataFromPath(Utf8GamePath gamePath)
    {
        foreach (var obj in Global.Identifier.Identify(gamePath.ToString()))
        {
            var name = obj.Key;
            if (name.StartsWith("Customization:"))
                name = name[14..].Trim();
            if (name != "Unknown")
                return new ResourceNode.UiData(name, ChangedItemDrawer.GetCategoryIcon(obj.Key, obj.Value));
        }

        return new ResourceNode.UiData(null, ChangedItemDrawer.ChangedItemIcon.Unknown);
    }

    private static string? SafeGet(ReadOnlySpan<string> array, Index index)
    {
        var i = index.GetOffset(array.Length);
        return i >= 0 && i < array.Length ? array[i] : null;
    }

    internal static unsafe ByteString GetResourceHandlePath(ResourceHandle* handle)
    {
        if (handle == null)
            return ByteString.Empty;

        var name = handle->FileName.AsByteString();
        if (name.IsEmpty)
            return ByteString.Empty;

        if (name[0] == (byte)'|')
        {
            var pos = name.IndexOf((byte)'|', 1);
            if (pos < 0)
                return ByteString.Empty;

            name = name.Substring(pos + 1);
        }

        return name;
    }

    private static unsafe ulong GetResourceHandleLength(ResourceHandle* handle)
    {
        if (handle == null)
            return 0;

        return handle->GetLength();
    }
}
