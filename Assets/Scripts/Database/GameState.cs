// Esta classe guarda os dados do jogo
public class GameState
{
    public int id;

    // Vida do jogador
    public int vida;

    // Nome da arma equipada
    public string arma_nome;

    // Balas que estão no carregador
    public int ammo_carregador;

    // Balas em reserva
    public int ammo_reserva;

    // -------- NOVO --------
    // Ronda atual
    public int round;

    // Posição do player
    public float pos_x;
    public float pos_y;
    public float pos_z;
}
