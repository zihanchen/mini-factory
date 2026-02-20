using Godot;
using System.Collections.Generic;

public partial class Game : Node2D
{
	private enum ResourceType
	{
		IronOre,
		Coal,
	}

	private sealed class ResourceNode
	{
		public ResourceNode(ResourceType type, int remaining)
		{
			Type = type;
			Remaining = remaining;
		}

		public ResourceType Type { get; }
		public int Remaining { get; set; }
	}

	private sealed class Inventory
	{
		public int IronOre { get; set; }
		public int Coal { get; set; }
		public int IronPlate { get; set; }
	}

	private const int GridWidth = 18;
	private const int GridHeight = 11;
	private const int TileSize = 44;
	private const int MarginX = 24;
	private const int MarginY = 24;
	private const int FontSize = 18;

	private readonly Vector2I _furnaceTile = new(12, 5);
	private readonly Dictionary<Vector2I, ResourceNode> _resourceNodes = new();

	private Vector2I _playerTile = new(2, 5);
	private Inventory _inventory = new();
	private string _status = "Mine iron ore and coal. Smelt at the furnace with F.";

	public override void _Ready()
	{
		ResetWorld();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
		{
			return;
		}

		bool shouldRedraw = false;

		switch (keyEvent.Keycode)
		{
			case Key.W:
			case Key.Up:
				shouldRedraw = TryMove(new Vector2I(0, -1));
				break;
			case Key.S:
			case Key.Down:
				shouldRedraw = TryMove(new Vector2I(0, 1));
				break;
			case Key.A:
			case Key.Left:
				shouldRedraw = TryMove(new Vector2I(-1, 0));
				break;
			case Key.D:
			case Key.Right:
				shouldRedraw = TryMove(new Vector2I(1, 0));
				break;
			case Key.E:
				shouldRedraw = TryMine();
				break;
			case Key.F:
				shouldRedraw = TrySmelt();
				break;
			case Key.R:
				ResetWorld();
				shouldRedraw = true;
				break;
		}

		if (shouldRedraw)
		{
			QueueRedraw();
		}
	}

	public override void _Draw()
	{
		DrawGrid();
		DrawResourceNodes();
		DrawFurnace();
		DrawPlayer();
		DrawHud();
	}

	private void ResetWorld()
	{
		_playerTile = new Vector2I(2, 5);
		_inventory = new Inventory();
		_resourceNodes.Clear();

		_resourceNodes[new Vector2I(4, 3)] = new ResourceNode(ResourceType.IronOre, 20);
		_resourceNodes[new Vector2I(5, 3)] = new ResourceNode(ResourceType.IronOre, 20);
		_resourceNodes[new Vector2I(4, 4)] = new ResourceNode(ResourceType.IronOre, 20);

		_resourceNodes[new Vector2I(3, 8)] = new ResourceNode(ResourceType.Coal, 20);
		_resourceNodes[new Vector2I(4, 8)] = new ResourceNode(ResourceType.Coal, 20);
		_resourceNodes[new Vector2I(3, 9)] = new ResourceNode(ResourceType.Coal, 20);

		_status = "Run reset. Mine with E, smelt at furnace with F.";
		QueueRedraw();
	}

	private bool TryMove(Vector2I delta)
	{
		Vector2I target = _playerTile + delta;
		if (!InsideGrid(target))
		{
			_status = "Cannot move outside the map.";
			return true;
		}

		_playerTile = target;
		_status = $"Moved to ({_playerTile.X}, {_playerTile.Y}).";
		return true;
	}

	private bool TryMine()
	{
		if (!TryFindNearbyResource(out Vector2I resourcePos))
		{
			_status = "No iron ore or coal nearby.";
			return true;
		}

		ResourceNode node = _resourceNodes[resourcePos];
		node.Remaining -= 1;

		switch (node.Type)
		{
			case ResourceType.IronOre:
				_inventory.IronOre += 1;
				_status = "Mined 1 iron ore.";
				break;
			case ResourceType.Coal:
				_inventory.Coal += 1;
				_status = "Mined 1 coal.";
				break;
		}

		if (node.Remaining <= 0)
		{
			_resourceNodes.Remove(resourcePos);
			_status += " The node is depleted.";
		}

		return true;
	}

	private bool TrySmelt()
	{
		if (!IsAdjacentOrSame(_playerTile, _furnaceTile))
		{
			_status = "Move next to the furnace first.";
			return true;
		}

		if (_inventory.IronOre <= 0 || _inventory.Coal <= 0)
		{
			_status = "Need at least 1 iron ore and 1 coal.";
			return true;
		}

		_inventory.IronOre -= 1;
		_inventory.Coal -= 1;
		_inventory.IronPlate += 1;
		_status = "Smelted 1 iron plate.";
		return true;
	}

	private bool TryFindNearbyResource(out Vector2I resourcePos)
	{
		foreach (Vector2I offset in NeighborOffsets())
		{
			Vector2I tile = _playerTile + offset;
			if (_resourceNodes.ContainsKey(tile))
			{
				resourcePos = tile;
				return true;
			}
		}

		resourcePos = default;
		return false;
	}

	private static IEnumerable<Vector2I> NeighborOffsets()
	{
		yield return new Vector2I(0, 0);
		yield return new Vector2I(0, -1);
		yield return new Vector2I(1, 0);
		yield return new Vector2I(0, 1);
		yield return new Vector2I(-1, 0);
	}

	private static bool IsAdjacentOrSame(Vector2I a, Vector2I b)
	{
		int manhattan = Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y);
		return manhattan <= 1;
	}

	private static bool InsideGrid(Vector2I tile)
	{
		return tile.X >= 0 && tile.X < GridWidth && tile.Y >= 0 && tile.Y < GridHeight;
	}

	private static Color TileFillColor(int x, int y)
	{
		return ((x + y) % 2 == 0) ? new Color("#1f2937") : new Color("#243244");
	}

	private void DrawGrid()
	{
		for (int y = 0; y < GridHeight; y++)
		{
			for (int x = 0; x < GridWidth; x++)
			{
				Rect2 rect = TileRect(new Vector2I(x, y));
				DrawRect(rect, TileFillColor(x, y), true);
				DrawRect(rect, new Color("#111827"), false, 1.5f);
			}
		}
	}

	private void DrawResourceNodes()
	{
		foreach (KeyValuePair<Vector2I, ResourceNode> pair in _resourceNodes)
		{
			Rect2 rect = TileRect(pair.Key).Grow(-6f);
			Color color = pair.Value.Type == ResourceType.IronOre ? new Color("#94a3b8") : new Color("#0f172a");
			DrawRect(rect, color, true);
		}
	}

	private void DrawFurnace()
	{
		Rect2 rect = TileRect(_furnaceTile).Grow(-5f);
		DrawRect(rect, new Color("#f97316"), true);
		DrawRect(rect, new Color("#7c2d12"), false, 2f);
	}

	private void DrawPlayer()
	{
		Vector2 center = TileRect(_playerTile).GetCenter();
		DrawCircle(center, TileSize * 0.28f, new Color("#22d3ee"));
		DrawCircle(center, TileSize * 0.13f, new Color("#083344"));
	}

	private void DrawHud()
	{
		Font font = ThemeDB.FallbackFont;
		float hudX = MarginX + GridWidth * TileSize + 24f;
		float lineY = MarginY + 20f;

		DrawString(font, new Vector2(hudX, lineY), "Factory Prototype", HorizontalAlignment.Left, -1f, FontSize + 4);
		lineY += 34f;

		DrawString(font, new Vector2(hudX, lineY), $"Iron Ore: {_inventory.IronOre}", HorizontalAlignment.Left, -1f, FontSize);
		lineY += 24f;
		DrawString(font, new Vector2(hudX, lineY), $"Coal: {_inventory.Coal}", HorizontalAlignment.Left, -1f, FontSize);
		lineY += 24f;
		DrawString(font, new Vector2(hudX, lineY), $"Iron Plate: {_inventory.IronPlate}", HorizontalAlignment.Left, -1f, FontSize);
		lineY += 36f;

		DrawString(font, new Vector2(hudX, lineY), "Controls", HorizontalAlignment.Left, -1f, FontSize + 2);
		lineY += 26f;
		DrawString(font, new Vector2(hudX, lineY), "Move: WASD / Arrows", HorizontalAlignment.Left, -1f, FontSize - 1);
		lineY += 22f;
		DrawString(font, new Vector2(hudX, lineY), "Mine nearby: E", HorizontalAlignment.Left, -1f, FontSize - 1);
		lineY += 22f;
		DrawString(font, new Vector2(hudX, lineY), "Smelt at furnace: F", HorizontalAlignment.Left, -1f, FontSize - 1);
		lineY += 22f;
		DrawString(font, new Vector2(hudX, lineY), "Reset run: R", HorizontalAlignment.Left, -1f, FontSize - 1);
		lineY += 38f;

		DrawString(font, new Vector2(hudX, lineY), "Status", HorizontalAlignment.Left, -1f, FontSize + 2);
		lineY += 24f;
		DrawString(font, new Vector2(hudX, lineY), _status, HorizontalAlignment.Left, 300f, FontSize - 1);
	}

	private static Rect2 TileRect(Vector2I tile)
	{
		return new Rect2(
			MarginX + tile.X * TileSize,
			MarginY + tile.Y * TileSize,
			TileSize,
			TileSize);
	}
}
