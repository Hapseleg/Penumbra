using System;
using OtterGui.Widgets;
using Penumbra.Interop.ResourceTree;
using Penumbra.UI.AdvancedWindow;

namespace Penumbra.UI.Tabs;

public class OnScreenTab : ITab
{
    private readonly Configuration       _config;
    private readonly ResourceTreeFactory _treeFactory;
    private          ResourceTreeViewer? _viewer;

    public OnScreenTab(Configuration config, ResourceTreeFactory treeFactory)
    {
        _config      = config;
        _treeFactory = treeFactory;
    }

    public ReadOnlySpan<byte> Label
        => "On-Screen"u8;

    public void DrawContent()
    {
        _viewer ??= new ResourceTreeViewer(_config, _treeFactory, "On-Screen tab", 0, delegate { }, delegate { });
        _viewer.Draw();
    }
}
