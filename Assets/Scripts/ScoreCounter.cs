using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class ScoreCounter : MonoBehaviour
{
    public static ScoreCounter Instance { get; private set; }

    private int score;

    public int Score
    {
        get => score;

        set
        {
            if (score == value) return;

            score = value;

            scoreText.SetText($"Количество очков:\n {score}");
        }
    }

    [SerializeField] private TextMeshProUGUI scoreText;

    private void Awake() => Instance = this;
   

}
