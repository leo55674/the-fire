using Godot;
using System;

public partial class ForgePanel : Control
{
	[Export] private Label _title;
	[Export] private Label _materials;
	[Export] private Label _description;
	[Export] private Label _result;
	[Export] private Button _craftButton;
	[Export] private Button _leaveButton;
	public GameSession Session { get; set; }

	public override void _Ready()
	{
		if (_title == null || _materials == null || _description == null || _result == null || _craftButton == null || _leaveButton == null)
			throw new InvalidOperationException("ForgePanel requires its labels and buttons.");
		_craftButton.Pressed += Craft;
		_leaveButton.Pressed += () => { Session.CloseForge(); Visible = false; };
	}

	public void Sync()
	{
		Visible = Session != null && Session.ForgeOpen;
		if (!Visible) { _result.Text = ""; return; }
		var next = Session.SwordCount == 1 ? "霜华" : "碧澜";
		var cost = Session.Balance.SwordCraftMaterialCost;
		_title.Text = Session.SwordCount < 3 ? $"锻造飞剑 · {next}" : "锻造飞剑 · 三剑已齐";
		_materials.Text = $"铸剑铁料  {Session.SwordIron} / {cost}";
		_description.Text = Session.SwordCount < 3 ? $"打造{next}后立即加入剑列。" : "所有飞剑都已打造完成。";
		_craftButton.Disabled = Session.SwordCount >= 3 || Session.SwordIron < cost;
	}

	private void Craft()
	{
		_result.Text = Session.CraftAtForge();
		Sync();
	}
}
