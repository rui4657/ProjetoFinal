using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string MainScene = "MainScene";

    [Header("UI")]
    [SerializeField] private Button loadButton;          // botão Load Game
    [SerializeField] private GameObject deleteSaveButton; // botão Delete Save (GameObject)

    private Database db;

    void Start()
    {
        db = new Database();
        RefreshSaveButtons();

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void RefreshSaveButtons()
    {
        bool hasSave = db.HasSave(1);

        if (loadButton) loadButton.interactable = hasSave;
        if (deleteSaveButton) deleteSaveButton.SetActive(hasSave);
    }

    public void StartGame()
    {
        PlayerPrefs.SetInt("load", 0);
        SceneManager.LoadScene(MainScene);
    }

    public void LoadGame()
    {
        // segurança extra
        if (db != null && !db.HasSave(1)) return;

        PlayerPrefs.SetInt("load", 1);
        SceneManager.LoadScene(MainScene);
    }

    public void DeleteSaveGame()
    {
        if (db == null) db = new Database();

        db.DeleteSave(1);
        PlayerPrefs.SetInt("load", 0);
        RefreshSaveButtons();
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("SAIR DO JOGO");
    }
}
