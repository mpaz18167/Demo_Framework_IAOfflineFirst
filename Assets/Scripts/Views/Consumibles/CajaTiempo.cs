using System;
using UnityEngine;

public class CajaTiempo : MonoBehaviour
{

    [Header("Segundos a congelar/restar")]
    [SerializeField] private float segundosRestar = 3f; // Se puede cambiar (1,2,3,4)

    private GameController gameController;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameController = FindAnyObjectByType<GameController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            //Verificar si el Controlador esta en modo contrarreloj antes de actuar
            if(gameController != null && gameController.esModoContrarreloj)
            {
                gameController.RestarTiempoContrarreloj(segundosRestar);
            }
        }
        else
        {
            Debug.Log("[CajaTiempo] Rota, pero no estamos en modo Contrarreloj.");
        }

        gameController.RegistrarObjetoRecolectado(gameObject);
    }
}
