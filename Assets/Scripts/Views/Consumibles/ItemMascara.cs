using UnityEngine;

public class ItemMascara : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement player = other.GetComponent<PlayerMovement>();
            if (player != null) player.RecolectarMascara();

            GameController gc = FindAnyObjectByType<GameController>();
            if(gc != null) gc.RegistrarObjetoRecolectado(gameObject);
            else Destroy(gameObject);
        }
    }
}
