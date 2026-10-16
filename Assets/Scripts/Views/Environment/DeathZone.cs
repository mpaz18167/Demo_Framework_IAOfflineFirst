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
            GetComponent<Renderer>().material.color = Color.red;
        }
    }
}
