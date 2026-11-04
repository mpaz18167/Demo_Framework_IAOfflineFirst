using System.Runtime.CompilerServices;
using UnityEngine;

public class DeathZone : MonoBehaviour
{
    private GameController gameController;
    void Start()
    {
        gameController = FindAnyObjectByType<GameController>();// Encuentra el GameController en la escena
    }

    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            gameController.IniciarMuerte();
            Renderer ren = GetComponentInChildren<Renderer>();
            if(ren != null) ren.material.color = Color.red;
        }
    }
}
