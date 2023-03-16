using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ImageTimer : MonoBehaviour
{
    public static ImageTimer Instance { get; private set; }

    [SerializeField] private float time;
    [SerializeField] public Image timerImage;

    private float timeLeft = 0f;

    private IEnumerator StartTimer()
    {
        while (timeLeft > 0)
        {
            if (Board.Instance.CanPop() || Board.Instance.checkPossMatch() == false) break; 
            timeLeft -= Time.deltaTime;
            var normalizedValue = Mathf.Clamp(timeLeft / time, 0.0f, 1.0f);
            timerImage.fillAmount = normalizedValue;
            yield return null;
        }
    }

    public void TimerCall()
    {
        timeLeft = time;
        StartCoroutine(StartTimer());
    }

    private void Awake() => Instance = this;
}
