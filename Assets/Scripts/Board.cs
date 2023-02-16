using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public sealed class Board : MonoBehaviour
{
    public static Board Instance { get; private set; }

    public Row[] rows;

    public Tile[,] Tiles { get; private set; }

    public int Widht => Tiles.GetLength(dimension:0);
    public int Height => Tiles.GetLength(dimension: 1);

    private void Awake() => Instance = this;

    private void Start()
    {
        Tiles = new Tile[rows.Max(selector:row => row.tiles.Length), rows.Length];

        for (var y = 0; y < Height; y++)
        {
            for (var x=0; x< Widht; x++)
            {
                var tile = rows[y].tiles[x];

                tile.x = x;
                tile.y = y;


                Tiles[x, y] = tile;

                tile.item = ItemDatabase.Items[Random.Range(0, ItemDatabase.Items.Length)];

            }
        }
    }

}
