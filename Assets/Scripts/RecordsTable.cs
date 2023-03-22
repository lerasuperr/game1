using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;


public sealed class RecordsTable : MonoBehaviour
{
    private int MaxScore = 0;
    private string Players;

    [SerializeField] private TextMeshProUGUI recordsText;
    [SerializeField] private TextMeshProUGUI maxScore;

    private void Start()
    {
        if (PlayerPrefs.HasKey("MaxScore"))
        {
            MaxScore = PlayerPrefs.GetInt("MaxScore", MaxScore);
            maxScore.SetText($"Максимальное количество очков: {MaxScore}");
        }

        if (PlayerPrefs.HasKey("Players"))
        {
            Players = PlayerPrefs.GetString("Players", Players);
            recordsText.SetText($"{Players}");
        }
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 2);
    }

}
