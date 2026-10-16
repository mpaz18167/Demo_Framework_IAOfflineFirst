using UnityEngine;

public class ItemTemporal : MonoBehaviour
{
    private GameController gc;
    void Start()
    {
        gc = FindAnyObjectByType<GameController>();
        GameController.OnReconfiguracionEntorno += DestruirPorRespawn;
    }

    void OnDestroy()
    {
        GameController.OnReconfiguracionEntorno -= DestruirPorRespawn;
    }

    private void DestruirPorRespawn()
    {
        // Si el jugador muere, este clon desaparece del mapa para siempre
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Aquí puedes llamar a gc.SumarPuntos() o player.RecolectarMascara()

            // MUY IMPORTANTE: Se destruye, NO llama a RegistrarObjetoRecolectado
            Destroy(gameObject);
        }
    }
}
