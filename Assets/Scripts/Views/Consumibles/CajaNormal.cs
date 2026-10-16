using UnityEngine;

public class CajaNormal : CajaInteractiva
{
    [Header("Recompensa que suelta (Fruta, Mascara, Vida)")]
    [SerializeField] private GameObject prefabRecompensa;
    [SerializeField] public int cantidadASoltar = 5;

    [Header("Degradación (Anti-Farmeo)")]
    [SerializeField] private GameObject prefabWumpaNormal; // Arrastra aquí la fruta Wumpa
    private bool yaDioPremioMayor = false;


    protected override void Start()
    {
        base.Start(); // Busca el gameController

        // Si esta caja contiene 5 Wumpas, le avisa al sistema que el máximo posible subió en 5
        if (gameController != null)
        {
            gameController.RegistrarPuntosDisponibles(cantidadASoltar);
        }
    }

    protected override void RomperCaja(bool esAtaque, bool esPisoton)
    {
        // 1. Decide qué soltar. Si ya dio la vida, suelta Wumpa.
        GameObject premioActual = (yaDioPremioMayor && prefabWumpaNormal != null) ? prefabWumpaNormal : prefabRecompensa;

        if (premioActual != null)
        {
            for (int i = 0; i < cantidadASoltar; i++)
            {
                Instantiate(premioActual, transform.position + Vector3.up, Quaternion.identity);
            }
        }


        // 2.Si el premio era valioso, marcamos que ya se entregó
        //Instancia la recompensa fisicamente en el mundo, puede ser una fruta, una máscara o una vida extra
        if (prefabRecompensa != prefabWumpaNormal)
        {
            for (int i = 0; i < cantidadASoltar; i++)
            {
                yaDioPremioMayor = true;
            }
        }
        DesactivarCaja();
    }

    // Cuando el GameController revive la caja tras morir, Unity ejecuta OnEnable automáticamente
    void OnEnable()
    {
        fueActivada = false; // Reseteamos su candado para que se pueda volver a romper
    }
}
