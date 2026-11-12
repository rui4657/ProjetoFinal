using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("Referências")]
    public GunSystem gun;              // arma do jogador
    public PlayerHealth player;        // vida do jogador
    public RoundManager roundManager;  // gestor de rondas

    [Header("UI - Vida / Balas / Arma")]
    public TMP_Text hpText;            // texto da vida
    public TMP_Text ammoText;          // texto da munição
    public TMP_Text gunText;           // texto do nome da arma

    [Header("UI - Ronda / Timer")]
    public TMP_Text roundText;         // texto da ronda (objeto ROUND)
    public TMP_Text timerText;         // texto do timer (objeto TIME)

    void Update()
    {
        // --- VIDA ---
        if (player && hpText)
        {
            hpText.text = $"{player.CurrentHP}";

            if (player.CurrentHP <= 25)
                hpText.color = new Color32(255, 90, 90, 255);
            else if (player.CurrentHP <= 60)
                hpText.color = new Color32(255, 200, 90, 255);
            else
                hpText.color = new Color32(242, 242, 242, 255);
        }



        // --- MUNIÇÃO ---
        if (gun && ammoText)
            ammoText.text = $"{gun.currentAmmo}<size=60%><color=#B0B0B0>/{gun.reserveAmmo}</color></size>";

        // --- NOME ARMA ---
        if (gun && gunText)
            gunText.text = gun.gunName;

        // --- RONDA (ROUND lá em cima) ---
        if (roundManager && roundText)
            roundText.text = $"ROUND {roundManager.currentRound}";

        // --- TIMER (TIME lá em cima) ---
        if (roundManager && timerText)
        {
            if (roundManager.inIntermission)
            {
                int secs = Mathf.CeilToInt(roundManager.intermissionTimer);

                // texto diferente no INÍCIO vs entre rondas
                if (roundManager.isGameStarting)
                    timerText.text = $"GAME STARTS IN: {secs}";
                else
                    timerText.text = $"NEXT ROUND IN: {secs}";
            }
            else
            {
                timerText.text = ""; // sem texto durante a ronda normal
            }
        }
    }
}
