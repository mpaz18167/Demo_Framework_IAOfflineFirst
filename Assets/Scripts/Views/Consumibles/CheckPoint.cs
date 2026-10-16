using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    public int numeroDeCheckPoint = 1; // Número del checkpoint para identificarlo
    [Range(0f, 1f)] public float porcentajeCompletado=0.2f;
    
    private GameController gameController;
    private bool yaActivado = false; // Variable para evitar que se active varias veces

    void Start()
    {
        gameController = FindAnyObjectByType<GameController>();// Encuentra el GameController en la escena
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !yaActivado)
        {
            yaActivado = true; // Marcar como activado para evitar múltiples activaciones
            gameController.TocarCheckpoint(numeroDeCheckPoint, porcentajeCompletado, transform.position);
            // Aquí puedes agregar efectos visuales o de sonido al activar el checkpoint
            // Por ejemplo, reproducir un sonido o una animación

            GetComponent<Renderer>().material.color = Color.green; // Cambiar el color del checkpoint como indicación visual
        }
    }
}


