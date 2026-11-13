using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string MainScene = "MainScene";
    [Header("UI")]
    [SerializeField] private Button loadButton;
    [SerializeField] private GameObject deleteSaveButton;
    private Database db;

    private void Start()
    {
        db = new Database();
        RefreshSaveButtons();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void RefreshSaveButtons()
    {
        bool hasSave = db != null && db.HasSave();
        if (loadButton != null) loadButton.interactable = hasSave;
        if (deleteSaveButton != null) deleteSaveButton.SetActive(hasSave);
    }

    public void StartGame()
    {
        SaveSystem.EnsureInstance().ResetForNewGame();
        PlayerPrefs.SetInt("load", 0);
        PlayerPrefs.Save();
        SceneManager.LoadScene(MainScene);
    }

    public void LoadGame()
    {
        if (db == null) db = new Database();
        if (!db.HasSave()) return;
        PlayerPrefs.SetInt("load", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(MainScene);
    }

    public void DeleteSaveGame()
    {
        SaveSystem.EnsureInstance().DeleteSave();
        PlayerPrefs.DeleteKey("load");
        PlayerPrefs.DeleteKey("save_name");
        PlayerPrefs.Save();
        RefreshSaveButtons();
    }

    public void QuitGame()
    {
        if (SaveSystem.instance != null) SaveSystem.instance.FlushSave();
        PlayerPrefs.Save();
        Application.Quit();
    }
}
