using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Unity.VisualScripting;

public class PausaUI : MonoBehaviour
{
    [SerializeField] private GameObject panelPausa;
    private bool enPausa = false;

    private PlayerControls controls;

    void Awake()
    {
        controls = new PlayerControls();
        //Escuchamos la accion Pause desde el InputActionAsset del proyecto
        controls.Player.Pause.performed += ctx => AlternarPausa();

    }

    private void OnEnable(){controls.Player.Enable();}

    private void OnDisable(){controls.Player.Disable();}

    public void AlternarPausa()
    {
        enPausa = !enPausa;
        if (panelPausa) panelPausa.SetActive(enPausa);
        Time.timeScale = enPausa ? 0f : 1f; // 0 = modo congelado (la pausa no depende del timeScale, el input sigue vivo)
    }
    public void Reanudar() 
    { 
        if (enPausa) AlternarPausa(); 
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
