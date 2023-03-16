using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.RuleTile.TilingRuleOutput;


public sealed class Tile : MonoBehaviour
{
    public int x;
    public int y;

    private Item _item;

    public Item item
    {
        get => _item;
        set
        {
            if (_item == value) return;

            _item = value;
            icon.sprite = _item.sprite;
        }
    }

    public bool flag = true;

    public Image icon;
    public Button button;

    public Tile Left => x > 0 ? Board.Instance.Tiles[x - 1, y] : null;
    public Tile Top => y > 0 ? Board.Instance.Tiles[x, y - 1] : null;
    public Tile Right => x < Board.Instance.Widht - 1 ? Board.Instance.Tiles[x + 1, y] : null;
    public Tile Bottom => y < Board.Instance.Height - 1 ? Board.Instance.Tiles[x, y + 1] : null;

    public Tile[] Neighbours => new[]
    {
        Left,
        Top,
        Right,
        Bottom,
    };

    private void Start()
    {
        button.onClick.AddListener(() => Board.Instance.Select(this));
    }

    public List<Tile> GetConnectedTilesX(List<Tile> exclude = null)
    {
        var resultX = new List<Tile> { this, };

        if (exclude == null)
            exclude = new List<Tile> { this, };
        else
            exclude.Add(this);

        foreach (var neighbour in Neighbours)
        {
            if (neighbour == null || exclude.Contains(neighbour) || neighbour.item != item || neighbour.x != x) continue;
            resultX.AddRange(neighbour.GetConnectedTilesX(exclude));
        }

        return resultX;
    }

    public List<Tile> GetConnectedTilesY(List<Tile> exclude = null)
    {
        var resultY = new List<Tile> { this, };

        if (exclude == null)
            exclude = new List<Tile> { this, };
        else
            exclude.Add(this);

        foreach (var neighbour in Neighbours)
        {
            if (neighbour == null || exclude.Contains(neighbour) || neighbour.item != item || neighbour.y != y) continue;
            resultY.AddRange(neighbour.GetConnectedTilesY(exclude));
        }

        return resultY;
    }
}
