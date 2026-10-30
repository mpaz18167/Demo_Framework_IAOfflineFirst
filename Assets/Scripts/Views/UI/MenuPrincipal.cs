using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    public void Jugar()
    {
        SceneManager.LoadScene("SampleScene"); // Nombre de la escena del juego
    }

    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Detener el juego en el editor
#else
        Application.Quit(); // Salir de la aplicación en compilaciones
#endif
    }
}
