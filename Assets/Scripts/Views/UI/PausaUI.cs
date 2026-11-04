using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Unity.VisualScripting;

public class PausaUI : MonoBehaviour
{
    [SerializeField] private GameObject panelPausa;
    private bool enPausa = false;
    private bool pausadoPorRanking = false; // NUEVO: el ranking "presta" la pausa

    private PlayerControls controls;

    void Awake()
    {
        InicializarControles();

    }

    private void InicializarControles()
    {
        if (controls != null) return;
        controls = new PlayerControls();
        controls.Player.Pause.performed += ctx => AlternarPausa();
    }

    private void OnEnable()
    {
        InicializarControles();
        controls?.Player.Enable();
    }

    private void OnDisable()
    {
        controls?.Player.Disable();
    }

    public void AlternarPausa()
    {
        // si ya ganaste, ESC no pausa/reanuda (el mundo debe quedar congelado tras la meta)
        GameController gc = FindAnyObjectByType<GameController>();
        if (gc != null && gc.nivelTerminado) return;

        enPausa = !enPausa;
        if (panelPausa) panelPausa.SetActive(enPausa);
        Time.timeScale = enPausa ? 0f : 1f; // 0 = modo congelado (la pausa no depende del timeScale, el input sigue vivo)
    }
    public void Reanudar() 
    { 
        if (enPausa) AlternarPausa(); 
    }
    
    public void PausarParaRanking()
    {
        if (enPausa) return;            // ya estaba pausado con ESC → no tocamos nada
        pausadoPorRanking = true;
        Time.timeScale = 0f;
    }

    public void ReanudarDeRanking()
    {
        if (!pausadoPorRanking) return; // esta pausa no la abrió el ranking
        pausadoPorRanking = false;
        if (enPausa) return;            // si abrió ESC durante el ranking, manda la pausa normal
        Time.timeScale = 1f;
    }
    public void Reiniciar()
    {
        Time.timeScale = 1f; // Reactivar Siempre antes de recargar la escena, para evitar que el juego quede congelado al reiniciar
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // Recargar la escena actual
    }

    public void SalirAlMenu()
    {
        Time.timeScale = 1f; // Reactivar Siempre antes de salir al menú, para evitar que el juego quede congelado al salir
        SceneManager.LoadScene("MenuPrincipal"); // Cargar la escena del menú principal
    }

}
