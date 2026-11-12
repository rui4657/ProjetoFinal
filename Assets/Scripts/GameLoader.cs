using UnityEngine;

public class GameLoader : MonoBehaviour
{
    [Header("Referências")]
    public GunSystem gunSystem;  // script que controla a arma e as balas

    private void Start()
    {
        int loadFlag = PlayerPrefs.GetInt("load", 0);
        Debug.Log("GameLoader: flag de load = " + loadFlag);

        if (gunSystem == null)
        {
            Debug.LogWarning("GameLoader: gunSystem não ligado no Inspector!");
            return;
        }

        if (loadFlag == 1)
        {
            // não limpamos a flag aqui, porque o PlayerHealth também precisa dela
            GameState gs = SaveSystem.instance.Load();

            Debug.Log("GameLoader: carregado da BD -> " +
                      "Vida = " + gs.vida +
                      ", Arma = " + gs.arma_nome +
                      ", Mag = " + gs.ammo_carregador +
                      ", Reserva = " + gs.ammo_reserva);

            gunSystem.ApplyLoadedGun(gs.arma_nome,
                                     gs.ammo_carregador,
                                     gs.ammo_reserva);
        }
        else
        {
            Debug.Log("GameLoader: jogo novo (sem load)");

            gunSystem.InitNewGame();
        }
    }
}
