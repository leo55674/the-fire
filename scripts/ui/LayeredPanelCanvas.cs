using System;
using Godot;

public partial class LayeredPanelCanvas : Node2D
{
	public Action<CanvasItem> DrawPanels { get; set; }

	public override void _Draw() => DrawPanels?.Invoke(this);
}
