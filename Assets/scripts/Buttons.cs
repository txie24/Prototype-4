using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class Buttons : MonoBehaviour
{
    public TMP_Text timeDisplay;
    public static float FinalSurvivalTime;
    void Start()
    {
        // Format the time into Minutes and Seconds
        float minutes = Mathf.FloorToInt(FinalSurvivalTime / 60);
        float seconds = Mathf.FloorToInt(FinalSurvivalTime % 60);

        if (timeDisplay != null)
        {
            timeDisplay.text = string.Format("You Survived: {0:00}:{1:00}", minutes, seconds);
        }
    }
    public void RestartGame()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SceneManager.LoadScene("Game");
    }
}
