using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuFinNivel : MonoBehaviour
{
    public GameObject panelNivelCompletado;
    public string[] ordenEscenas = { "MenuPrincipal", "SampleScene", "Nivel2" }; // índice = id del nivel (0 = menú)

    public void CargarSiguienteNivel()
    {
        GameController gc = FindAnyObjectByType<GameController>();
        int desbloqueado = gc != null && gc.DatosJugador != null ? gc.DatosJugador.highestLevelUnlocked : 2;
        if (desbloqueado >= ordenEscenas.Length) { SceneManager.LoadScene("MenuPrincipal"); return; } // último nivel → menú
        SceneManager.LoadScene(ordenEscenas[desbloqueado]); // nivel 2 → índice 2
    }

    public void VolverAlMenu() => SceneManager.LoadScene("MenuPrincipal");
}
