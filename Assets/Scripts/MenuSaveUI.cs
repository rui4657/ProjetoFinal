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
    [SerializeField] private GameObject deleteSaveButton;   // o botão ao lado do Load
    [SerializeField] private TMP_Text saveNameText;         // texto ao lado do Load (opcional)

    [Header("Name Panel")]
    [SerializeField] private GameObject namePanel;          // painel para escrever o nome
    [SerializeField] private TMP_InputField nameInput;      // input do nome
    [SerializeField] private string defaultSaveName = "Save 1";

    private Database db;

    void Start()
    {
        db = new Database();

        if (namePanel) namePanel.SetActive(false);
        RefreshSaveUI();
    }

    void RefreshSaveUI()
    {
        bool hasSave = db.HasSave(1);

        if (loadButton) loadButton.interactable = hasSave;
        if (deleteSaveButton) deleteSaveButton.SetActive(hasSave);

        if (saveNameText)
        {
            if (hasSave)
            {
                string n = PlayerPrefs.GetString("save_name", defaultSaveName);
                saveNameText.text = n;
                saveNameText.gameObject.SetActive(true);
            }
            else
            {
                saveNameText.gameObject.SetActive(false);
            }
        }
    }

    // Botão Start Game -> abre painel para nome
    public void StartGameAskName()
    {
        if (!namePanel) { StartGameWithName(defaultSaveName); return; }

        namePanel.SetActive(true);

        if (nameInput)
        {
            string n = PlayerPrefs.GetString("save_name", defaultSaveName);
            nameInput.text = n;
            nameInput.Select();
            nameInput.ActivateInputField();
        }
    }

    // Botão Confirm dentro do painel
    public void ConfirmStartGame()
    {
        string n = defaultSaveName;
        if (nameInput)
        {
            string t = nameInput.text.Trim();
            if (!string.IsNullOrEmpty(t)) n = t;
        }

        StartGameWithName(n);
    }

    // Botão Cancel dentro do painel
    public void CancelStartGame()
    {
        if (namePanel) namePanel.SetActive(false);
    }

    void StartGameWithName(string saveName)
    {
        PlayerPrefs.SetString("save_name", saveName);

        // cria logo um save básico para o botão Load/Delete aparecerem no menu
        GameState gs = new GameState
        {
            id = 1,
            vida = 100,
            arma_nome = "AK",
            ammo_carregador = 30,
            ammo_reserva = 120,
            round = 1,
            pos_x = 0f,
            pos_y = 0f,
            pos_z = 0f
        };
        db.SaveGame(gs);

        PlayerPrefs.SetInt("load", 0);
        SceneManager.LoadScene(mainScene);
    }

    public void LoadGame()
    {
        if (!db.HasSave(1)) return;
        PlayerPrefs.SetInt("load", 1);
        SceneManager.LoadScene(mainScene);
    }

    public void DeleteSave()
    {
        db.DeleteSave(1);
        PlayerPrefs.DeleteKey("save_name");
        PlayerPrefs.SetInt("load", 0);
        RefreshSaveUI();
    }
}
