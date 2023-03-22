using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using static UnityEditor.Experimental.GraphView.GraphView;
using Unity.VisualScripting.Dependencies.Sqlite;
using TMPro;

public sealed class Board : MonoBehaviour
{
    public static Board Instance { get; private set; }

    [SerializeField] private AudioClip collectSound;
    [SerializeField] private AudioSource audioSource;

    public Row[] rows;

    public Tile[,] Tiles { get; private set; }

    public int Widht => Tiles.GetLength(0);
    public int Height => Tiles.GetLength(1);

    private void Awake() => Instance = this;

    private readonly List<Tile> selection = new List<Tile>();
    private const float TweenDuration = 0.4f;

    public GameObject Form;
    public Button button;

    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI NameText;

    public int MaxScore = 0;
    public string Players;
    public static string NamePlayer;
    

    private void Start()
    {
        if (PlayerPrefs.HasKey("MaxScore"))
            MaxScore = PlayerPrefs.GetInt("MaxScore", MaxScore);
       
        if (PlayerPrefs.HasKey("Players"))
            Players = PlayerPrefs.GetString("Players", Players);

        CreateBoard();

        if (CanPop()) Pop0();

        while (checkPossMatch() == false)
        {
            CreateBoard();

            if (CanPop()) Pop0();
        }

        ImageTimer.Instance.TimerCall();
    }

    public void CreateBoard()
    {
        Tiles = new Tile[rows.Max(selector: row => row.tiles.Length), rows.Length];

        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Widht; x++)
            {
                var tile = rows[y].tiles[x];
                
                tile.x = x;
                tile.y = y;

                tile.item = ItemDatabase.Items[UnityEngine.Random.Range(0, ItemDatabase.Items.Length)];

                Tiles[x, y] = tile;

            }
        }
    }

    public async Task deflate()
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Widht; x++)
            {
                var tile = Tiles[x, y];

                var sequence = DOTween.Sequence();
                sequence.Append(Tiles[x, y].icon.transform.DOScale(Vector3.zero, 0.1f));

                await sequence.Play().AsyncWaitForCompletion();
            }
        }
    }

    public async Task inflate()
    {
        for (var y = Height - 1; y >= 0; y--)
        {
            for (var x = Widht - 1; x >= 0; x--)
            {
                var tile = Tiles[x, y];

                var sequence = DOTween.Sequence();

                sequence.Append(Tiles[x, y].icon.transform.DOScale(Vector3.one, 0.1f));

                await sequence.Play().AsyncWaitForCompletion();
            }
        }
    }

    public async void Select(Tile tile)
    {

        if (!selection.Contains(tile))
        {
            if(selection.Count > 0)
            {
                if (Array.IndexOf(selection[0].Neighbours,tile) != -1)
                    selection.Add(tile);
                else selection.Clear();
            }
            else selection.Add(tile);
             
        }
        
        if (selection.Count < 2) return;
        if (selection.Count > 2) selection.Clear();

        Debug.Log($"Выбранные плитки ({selection[0].x},{selection[0].y}) and ({selection[1].x},{selection[1].y})");

        await Swap(selection[0], selection[1]);

        if (CanPop())
        {
            await Pop();

            ImageTimer.Instance.TimerCall();
        }

        else await Swap(selection[0], selection[1]);
        
        selection.Clear();

        if (checkPossMatch() == false)
        {
            Debug.Log("Нет ходов");

            await deflate();

            CreateBoard();

            if (CanPop()) Pop0();

            await inflate();

            ImageTimer.Instance.TimerCall();
        }
    }

    public void Update()
    {
        if (ImageTimer.Instance.timerImage.fillAmount == 0)
        {
            Debug.Log("Время вышло!");

            ImageTimer.Instance.timerImage.fillAmount = 1;

            Form.SetActive(true);
            NameText.GetComponent<TextMeshProUGUI>();

            scoreText.SetText($"Ваше количество очков: {ScoreCounter.Instance.Score}");
        }
    }

    public void SaveScore()
    {
        if (ScoreCounter.Instance.Score > MaxScore)
        {
            MaxScore = ScoreCounter.Instance.Score;
            PlayerPrefs.SetInt("MaxScore", MaxScore);
        }
        
        NamePlayer = NameText.text.ToString() + " - ";

        Players = Players + NamePlayer + ScoreCounter.Instance.Score + " \n";

        PlayerPrefs.SetString("Players", Players);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }

    public async Task Swap(Tile tile1, Tile tile2)
    {
        var icon1 = tile1.icon;
        var icon2 = tile2.icon;

        var icon1Transform = icon1.transform;
        var icon2Transform = icon2.transform;

       var sequence = DOTween.Sequence();
       sequence.Join(icon1Transform.DOMove(icon2Transform.position,TweenDuration))
               .Join(icon2Transform.DOMove(icon1Transform.position, TweenDuration));
       await sequence.Play().AsyncWaitForCompletion();

        icon1Transform.SetParent(tile2.transform);
        icon2Transform.SetParent(tile1.transform);

        tile1.icon = icon2;
        tile2.icon = icon1;

        var tile1Item = tile1.item;
        tile1.item = tile2.item;
        tile2.item = tile1Item;

    }

    public bool CanPop()
    {

        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Widht; x++)
            {
                if (Tiles[x, y].GetConnectedTilesX().Skip(1).Count() >= 2 || Tiles[x, y].GetConnectedTilesY().Skip(1).Count() >= 2)
                { 
                    return true;
                }
            }
        }

        return false;
    }

    private void Pop0()
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Widht; x++)
            {
                var tile = Tiles[x, y];

                var connectedTilesX = tile.GetConnectedTilesX();
                var connectedTilesY = tile.GetConnectedTilesY();

                if (connectedTilesX.Skip(1).Count() < 2 && connectedTilesY.Skip(1).Count() < 2) continue;

                if (connectedTilesX.Count() > 2)
                {
                    foreach (var connectedTileX in connectedTilesX)
                        connectedTileX.item = ItemDatabase.Items[UnityEngine.Random.Range(0, ItemDatabase.Items.Length)];
                }
                else
                {
                    foreach (var connectedTileY in connectedTilesY)
                        connectedTileY.item = ItemDatabase.Items[UnityEngine.Random.Range(0, ItemDatabase.Items.Length)];
                }

                x = 0;
                y = 0;
            }   
        }
    }

    public async Task deflateAnimation(List<Tile> connectedTiles)
    {
        var deflateSequence = DOTween.Sequence();

        foreach (var connectedTile in connectedTiles)
        {
            deflateSequence.Join(connectedTile.icon.transform.DOScale(1.25f, TweenDuration));
        }

        await deflateSequence.Play().AsyncWaitForCompletion();

        audioSource.PlayOneShot(collectSound);

        var deflateSequence2 = DOTween.Sequence();

        foreach (var connectedTile in connectedTiles)
        {
            deflateSequence2.Join(connectedTile.icon.transform.DOScale(Vector3.zero, TweenDuration));
        }

        await deflateSequence2.Play().AsyncWaitForCompletion();
    }

    public bool checkPossMatch()
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Widht; x++)
            {
                var tile = Tiles[x, y];

                var connectedTilesX = tile.GetConnectedTilesX();
                var connectedTilesY = tile.GetConnectedTilesY();

                if (connectedTilesX.Count() == 1 && connectedTilesX.Count() == 1)
                {
                    var tile0 = connectedTilesX[0];

                    var tile0Right = tile0.Right;

                    if (tile0Right)
                    {
                        if (tile0Right.Bottom && tile0Right.Right && tile0Right.Bottom.item == tile0.item
                            && tile0Right.Right.item == tile0.item) return true;
                    }

                    var tile0Bottom = tile0.Bottom;

                    if (tile0Bottom)
                    {
                        if (tile0Bottom.Left && tile0Bottom.Right &&
                            tile0Bottom.Left.item == tile0.item && tile0Bottom.Right.item == tile0.item ||

                            tile0Bottom.Left && tile0Bottom.Bottom &&
                            tile0Bottom.Left.item == tile0.item && tile0Bottom.Bottom.item == tile0.item ||

                            tile0Bottom.Right && tile0Bottom.Bottom &&
                            tile0Bottom.Right.item == tile0.item && tile0Bottom.Bottom.item == tile0.item)

                            return true;
                    }
                    
                }

                if (connectedTilesX.Count() == 2)
                {
                    List<Tile> connectedTiles = connectedTilesX.OrderBy(x => x.y).ToList();

                    var yX0 = connectedTiles[0].y;
                    var yX1 = connectedTiles[1].y;

                    var tiles0 = Tiles[x, yX0];
                    var tiles1 = Tiles[x, yX1];

                    var neighbour0 = tiles0.Top;
                    var neighbour1 = tiles1.Bottom;

                    if (neighbour0)
                    {
                        if (neighbour0.Top && neighbour0.Top.item == tiles0.item ||
                             neighbour0.Left && neighbour0.Left.item == tiles0.item ||
                             neighbour0.Right && neighbour0.Right.item == tiles0.item)
                            return true;
                    }

                    if (neighbour1)
                    {
                        if(neighbour1.Bottom && neighbour1.Bottom.item == tiles1.item ||
                             neighbour1.Left && neighbour1.Left.item == tiles1.item ||
                             neighbour1.Right && neighbour1.Right.item == tiles1.item)
                            return true;
                    }

                }

                if (connectedTilesY.Count() == 2)
                {
                    List<Tile> connectedTiles = connectedTilesY.OrderBy(x => x.x).ToList();

                    var xY0 = connectedTiles[0].x;
                    var xY1 = connectedTiles[1].x;

                    var tiles0 = Tiles[xY0, y];
                    var tiles1 = Tiles[xY1, y];

                    var neighbour0 = tiles0.Left;
                    var neighbour1 = tiles1.Right;

                    if (neighbour0)
                    {
                        if (neighbour0.Top && neighbour0.Top.item == tiles0.item ||
                            neighbour0.Left && neighbour0.Left.item == tiles0.item ||
                            neighbour0.Bottom && neighbour0.Bottom.item == tiles0.item)
                            return true;
                    }

                    if (neighbour1)
                    {
                        if (neighbour1.Top && neighbour1.Top.item == tiles1.item ||
                             neighbour1.Right && neighbour1.Right.item == tiles1.item ||
                             neighbour1.Bottom && neighbour1.Bottom.item == tiles1.item)
                            return true;
                    }

                }

            }
        }

        return false;
    }

    private async Task Pop()
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Widht; x++)
            {
                var tile = Tiles[x, y];

                var connectedTilesX = tile.GetConnectedTilesX();
                var connectedTilesY = tile.GetConnectedTilesY();

                if (connectedTilesX.Skip(1).Count() < 2 && connectedTilesY.Skip(1).Count() < 2) continue;

                if (connectedTilesX.Count() > 2)
                {
                    await deflateAnimation(connectedTilesX);

                    ScoreCounter.Instance.Score += connectedTilesX.Count;

                    var shift = connectedTilesX.Count();

                    if (connectedTilesX[0].y != 0)
                    {
                        var top = connectedTilesX[0].Top.y;
                        var count = shift;
                        var a = await TilesShift(connectedTilesX, x, top, count, shift, true);

                        if (count != 0)
                        {
                            var b = shift - a - 1;
                            var yX = connectedTilesX[0].y;
                            while (b >= 0)
                            {
                                Tiles[x, yX + b].flag = false;
                                b--;
                            }

                        }

                        await NewTilesShift(FindEmptyTiles());
                    }

                    else
                        await NewTilesShift(connectedTilesX);
                }
                else
                {
                    await deflateAnimation(connectedTilesY);

                    ScoreCounter.Instance.Score += connectedTilesY.Count;

                    if (connectedTilesY[0].y != 0)
                    {
                        var shift = 1;

                        for (var i = 0; i < connectedTilesY.Count(); i++)
                        {
                            var xY = connectedTilesY[i].x;
                            var top = connectedTilesY[i].Top.y;
                            var count = top + 1;

                            await TilesShift(connectedTilesY, xY, top, count, shift, false);
                            
                        }

                        await NewTiles(FindEmptyTiles());

                    }
                    else
                        await NewTiles(connectedTilesY);
                }

                x = 0;
                y = 0;

            }
        }
    }

    public async Task NewTiles(List<Tile> TilesEmpty)
    {
        var count = 0;
        var x = TilesEmpty[0].x;
        var y = TilesEmpty[0].y;

        while (count <= TilesEmpty.Count() - 1)
        {
            Tiles[x + count, y].item = ItemDatabase.Items[UnityEngine.Random.Range(0, ItemDatabase.Items.Length)];

            var inflateSequence2 = DOTween.Sequence();
            inflateSequence2.Append(Tiles[x + count, y].icon.transform.DOScale(Vector3.one, TweenDuration));

            await inflateSequence2.Play().AsyncWaitForCompletion();

            Tiles[x + count, y].flag = true;

            count++;
        }
        
    }

    public async Task<int> TilesShift(List<Tile> connectedTiles, int x, int top, int count, int shift, bool flag)
    {
        int a = 0;
        
        while (count > 0 && top >= 0)
        {
            var iconTop = Tiles[x, top].icon;
            var iconShift = Tiles[x, top + shift].icon;

            var iconTopTransform = iconTop.transform;
            var iconShiftTransform = iconShift.transform;

            var inflateSequence = DOTween.Sequence();

            inflateSequence.Append(iconTopTransform.DOMove(iconShiftTransform.position, TweenDuration))
             .Append(iconShiftTransform.DOMove(iconTopTransform.position, 0));

            await inflateSequence.Play().AsyncWaitForCompletion();

            iconShiftTransform.SetParent(Tiles[x, top].transform);
            iconTopTransform.SetParent(Tiles[x, top + shift].transform);


            Tiles[x, top + shift].icon = iconTop;
            Tiles[x, top].icon = iconShift;

            var itemShift = Tiles[x, top + shift].item;
            Tiles[x, top + shift].item = Tiles[x, top].item;
            Tiles[x, top].item = itemShift;

            if (flag == true)
                Tiles[x, top].flag = false;
            else
            {
                if (top == 0)
                    Tiles[x, top].flag = false;
            }

            count--;
            top--;
            a++;

            inflateSequence.Kill();
        }

        return a;
    }

    public List<Tile> FindEmptyTiles()
    {
        var TilesEmpty = new List<Tile>();

        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Widht; x++)
            {
                var tile = Tiles[x, y];

                if (tile.flag == false)
                    TilesEmpty.Add(tile);
            }
        }

        return TilesEmpty;
    }

    public async Task NewTilesShift(List<Tile> TilesEmpty)
    {
        var count = TilesEmpty.Count() - 1;

        while (count >= 0)
        {
            var yX = TilesEmpty[0].y;
            var x = TilesEmpty[0].x;

            var icon0 = Tiles[x, yX].icon;
            var iconShift = Tiles[x, yX + count].icon;

            var icon0Transform = icon0.transform;
            var iconShiftTransform = iconShift.transform;

            Tiles[x, yX].item = ItemDatabase.Items[UnityEngine.Random.Range(0, ItemDatabase.Items.Length)];
          
            var inflateSequence0 = DOTween.Sequence();

            if (count == 0)
            {
                inflateSequence0.Append(Tiles[x, yX].icon.transform.DOMove(Tiles[x, yX].icon.transform.position, TweenDuration))
                                 .Append(Tiles[x, yX].icon.transform.DOScale(Vector3.one, TweenDuration));
                await inflateSequence0.Play().AsyncWaitForCompletion();
            }
            else
            {
                inflateSequence0.Append(icon0Transform.DOScale(Vector3.one, TweenDuration))
                               .AppendInterval(0.1f)
                               .Append(icon0Transform.DOMove(iconShiftTransform.position, TweenDuration))
                               .Append(iconShiftTransform.DOMove(icon0Transform.position, 0));
                       
                await inflateSequence0.Play().AsyncWaitForCompletion();

                inflateSequence0.Kill();

                iconShiftTransform.SetParent(Tiles[x, yX].transform);
                icon0Transform.SetParent(Tiles[x, yX + count].transform);

                Tiles[x, yX + count].icon = icon0;
                Tiles[x, yX].icon = iconShift;

                var itemShift = Tiles[x, yX + count].item;
                Tiles[x, yX + count].item = Tiles[x, yX].item;
                Tiles[x, yX].item = itemShift;
            }
           
            Tiles[x, yX + count].flag = true;

            count--;

        }
    }

   
}
