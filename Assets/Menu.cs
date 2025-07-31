using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    Button nextButton;
    Button redoButton;

    void Awake()
    {
        nextButton = transform.Find("Layout").Find("Next").GetComponent<Button>();
        redoButton = transform.Find("Layout").Find("Redo").GetComponent<Button>();

        nextButton.onClick.AddListener(LoadNextLevel);
        redoButton.onClick.AddListener(RedoCurrentLevel);

        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            nextButton.targetGraphic.enabled = false;
        }
    }

    void LoadNextLevel()
    {
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex);
        }
    }

    void RedoCurrentLevel()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentIndex);
    }
}
