using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemVida : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameController gc = FindAnyObjectByType<GameController>();
            if (gc != null)
            {
                gc.SumarVida(1);
                gc.RegistrarObjetoRecolectado(gameObject); // Usa tu sistema de memoria de checkpoint
            }
            else Destroy(gameObject);
        }
    }
}
