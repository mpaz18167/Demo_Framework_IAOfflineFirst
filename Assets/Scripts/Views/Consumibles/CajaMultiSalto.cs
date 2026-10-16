using UnityEngine;

public class CajaMultiSalto : CajaInteractiva
{
    [Header("Ajustes MultiSalto (Bouncy Box)")]
    [SerializeField] private GameObject prefabPuntos;
    [SerializeField] private float reboteEspecial = 12f; //Mayor al 9f normal
    [SerializeField] private int totalGolpes = 5; //Cantidad de golpes que puede recibir antes de romperse
    private int golpesRestantes; 
    
    protected override void Start()
    {
        base.Start(); //LLamar al Start de CajaInteractiva para buscar el GameController
        golpesRestantes = totalGolpes;
        // NUEVO: La caja registra sus frutas ocultas en el total del nivel
        if (gameController != null)
        {
            gameController.RegistrarPuntosDisponibles(totalGolpes);
        }
    }

    protected override void RomperCaja(bool esAtaque, bool esPisoton)
    {
        if (esAtaque) //Giro o Cabezaso
        {
            //Castigo por no saltar sobre la caja: Se rompe y da un solo punto
            if (prefabPuntos != null) Instantiate(prefabPuntos, transform.position + Vector3.up, Quaternion.identity);
            DesactivarCaja();
        }
        else
        {
            //Si es Pisoton o cabezaso: Suelta una cuota por cada rebote
            if(golpesRestantes>0)
            {
                if (prefabPuntos != null) Instantiate(prefabPuntos, transform.position + Vector3.up, Quaternion.identity);

                if (esPisoton)
                {
                    PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
                    if (player != null) player.AplicarImpulso(reboteEspecial);
                }

                golpesRestantes--;

                if (golpesRestantes <= 0)
                {
                    DesactivarCaja();
                }
            }
        }
    }
}
