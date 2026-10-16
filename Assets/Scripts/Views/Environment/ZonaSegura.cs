using UnityEngine;

public class ZonaSegura : MonoBehaviour
{
    private GameController gameController;

    void Start()
    {
        gameController = FindAnyObjectByType<GameController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && gameController !=null)
        {
            gameController.EstablecerEstadoSeguro(true);

        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && gameController != null)
        {
            gameController.EstablecerEstadoSeguro(false);
        }
    }
}
