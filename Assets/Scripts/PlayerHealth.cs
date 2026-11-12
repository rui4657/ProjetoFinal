using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int maxHP = 100;                    // Vida máxima do jogador
    public int CurrentHP { get; private set; } // Vida atual do jogador

    [Header("Game Over")]
    [SerializeField] private string menuSceneName = "Menu";
    [SerializeField] private float gameOverDelay = 2f;

    void Start()
    {
        // 0 = Novo Jogo ; 1 = Load Game (vem do menu)
        int loadFlag = PlayerPrefs.GetInt("load", 0);
        Debug.Log("PlayerHealth: loadFlag = " + loadFlag);

        if (loadFlag == 1)
        {
            // já não precisamos da flag depois disto
            PlayerPrefs.SetInt("load", 0);

            // vai buscar o save à base de dados
            GameState gs = SaveSystem.instance.Load();

            CurrentHP = Mathf.Clamp(gs.vida, 0, maxHP);
            Debug.Log("PlayerHealth: vida carregada do save = " + CurrentHP);
        }
        else
        {
            // NOVO JOGO  começa com vida cheia
            CurrentHP = maxHP;
            Debug.Log("PlayerHealth: novo jogo, vida = " + CurrentHP);
        }

        UpdateHUD();
    }

    public void TakeDamage(int amount)
    {
        CurrentHP -= amount;
        if (CurrentHP < 0) CurrentHP = 0;

        Debug.Log("PLAYER LEVOU DANO: " + amount + " | HP = " + CurrentHP);

        UpdateHUD();

        if (CurrentHP <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("PLAYER MORREU!");
        StartCoroutine(GameOverRoutine());
    }
    IEnumerator GameOverRoutine()
    {
        yield return new WaitForSeconds(gameOverDelay);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (Application.CanStreamedLevelBeLoaded(menuSceneName))
            SceneManager.LoadScene(menuSceneName);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name); // fallback
    }

    void UpdateHUD()
    {
        // HUDController já está a ler CurrentHP no Update, por isso aqui
        // não é obrigatório fazer nada por enquanto.
    }
}
