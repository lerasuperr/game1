using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pause : MonoBehaviour
{
    public GameObject Form;

    public void pause()
    {
        Form.SetActive(true);
        Time.timeScale = 0;
    }

    
}
