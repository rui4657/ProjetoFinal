using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MenuSaveUI : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string mainScene = "MainScene";
    [Header("UI")]
    [SerializeField] private Button loadButton;
    [SerializeField] private GameObject deleteSaveButton;
    [SerializeField] private TMP_Text saveNameText;
    [Header("Name Panel")]
    [SerializeField] private GameObject namePanel;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private string defaultSaveName = "Save 1";
    private Database db;

    private void Start()
    {
        db = new Database();
        if (namePanel != null) namePanel.SetActive(false);
        RefreshSaveUI();
    }

    private void RefreshSaveUI()
    {
        if (db == null) db = new Database();
        bool hasSave = db.HasSave();
        if (loadButton != null) loadButton.interactable = hasSave;
        if (deleteSaveButton != null) deleteSaveButton.SetActive(hasSave);
        if (saveNameText == null) return;
        saveNameText.gameObject.SetActive(hasSave);
        if (hasSave) saveNameText.text = PlayerPrefs.GetString("save_name", defaultSaveName);
    }

    public void StartGameAskName()
    {
        if (namePanel == null) { StartGameWithName(defaultSaveName); return; }
        namePanel.SetActive(true);
        if (nameInput == null) return;
        nameInput.characterLimit = 32;
        nameInput.text = PlayerPrefs.GetString("save_name", defaultSaveName);
        nameInput.Select();
        nameInput.ActivateInputField();
    }

    public void ConfirmStartGame()
    {
        string name = nameInput != null ? nameInput.text.Trim() : defaultSaveName;
        StartGameWithName(string.IsNullOrEmpty(name) ? defaultSaveName : name);
    }

    public void CancelStartGame()
    { if (namePanel != null) namePanel.SetActive(false); }

    private void StartGameWithName(string saveName)
    {
        SaveSystem.EnsureInstance().ResetForNewGame();
        PlayerPrefs.SetString("save_name", saveName);
        PlayerPrefs.SetInt("load", 0);
        PlayerPrefs.Save();
        // The first snapshot is captured from the actual player spawn by GameLoader.
        SceneManager.LoadScene(mainScene);
    }

    public void LoadGame()
    {
        if (db == null) db = new Database();
        if (!db.HasSave()) return;
        PlayerPrefs.SetInt("load", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(mainScene);
    }

    public void DeleteSave()
    {
        SaveSystem.EnsureInstance().DeleteSave();
        PlayerPrefs.DeleteKey("save_name");
        PlayerPrefs.DeleteKey("load");
        PlayerPrefs.Save();
        RefreshSaveUI();
    }
}
