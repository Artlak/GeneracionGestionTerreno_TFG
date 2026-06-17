using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour
{
    public void GoToWorldGen()
    {
        SceneManager.LoadScene("Game");
    }

    public void GoToTutorial()
    {
        SceneManager.LoadScene("Tutorial");
    }

    public void ExitProgram()
    {
        Application.Quit();
    }
}
