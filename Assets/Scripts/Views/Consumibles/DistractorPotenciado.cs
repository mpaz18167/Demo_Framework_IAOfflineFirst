using UnityEngine;

public class DistractorPotenciado : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player == null) return;

        if (!player.TomarFlash())
        {
            Debug.Log("[ItemFlashPotenciado] Ya llevas un Flash: tíralo antes de agarrar otro.");
            return; // NO se consume: sigue ahí
        }

        GameController gc = FindAnyObjectByType<GameController>();
        if (gc != null) gc.RegistrarObjetoRecolectado(gameObject);
        else Destroy(gameObject);

        Debug.Log("[ItemFlashPotenciado] ¡Flash teledirigido obtenido! Tíralo con el botón del distractor.");
    }
}
