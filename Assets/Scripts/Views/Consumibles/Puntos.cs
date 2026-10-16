using UnityEngine;

public class Puntos : MonoBehaviour
{
    public int puntosBase = 1;

    [Header("Configuración de Generación")]
    [Tooltip("Marca esto si el punto está suelto en el nivel. Desmárcalo si sale de una caja.")]
    public bool esPuntoSuelto = false;
    private GameController gameController;
    private int puntosActuales;
    void Start()
    {
        gameController = FindAnyObjectByType<GameController>();// Encuentra el GameController en la escena
        
        // NUEVO: Solo suma al máximo del nivel si está suelta.
        if (esPuntoSuelto && gameController != null)
        {
            gameController.RegistrarPuntosDisponibles(puntosBase);
        }

        //Escuchar Ordenes de la IA
        GameController.OnDificultadCambiada += ActualizarPuntos;
        ActualizarPuntos(gameController.ObtenerFactorDificultad());
    }

    private void OnDestroy()
    {
        if (gameController != null) GameController.OnDificultadCambiada -= ActualizarPuntos;
    }

    private void ActualizarPuntos(float factorDificultad)
    {
        //En alta dificultad factor > 1, los puntos bajan.
        puntosActuales= Mathf.Max(1, (int)(puntosBase/factorDificultad));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            gameController.SumarPuntos(puntosActuales);

            // Aquí puedes agregar efectos visuales o de sonido al recoger la caja de puntos
            // Por ejemplo, reproducir un sonido o una animación

            // Destruye la caja de puntos después de ser recogida
            gameController.RegistrarObjetoRecolectado(gameObject);
        }
    }
}
