using UnityEngine;

public class CajaTrampolinMadera : CajaInteractiva
{
    [Header("Ajustes de Súper Salto")]
    [SerializeField] private float impulsoTrampolin = 20f;
    [SerializeField] private GameObject prefabFruta;
    [SerializeField] private int cantidadASoltar = 5; // Premio escondido si la rompes
   
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
        //Instancia la recompensa fisicamente en el mundo, puede ser una fruta, una máscara o una vida extra
        if (esAtaque || !esPisoton)
        {
            if(prefabFruta != null)
            {
                for (int i = 0; i < cantidadASoltar; i++)
                {
                    Instantiate(prefabFruta, transform.position + Vector3.up, Quaternion.identity);
                }
            }
            DesactivarCaja();
        }

        else if (esPisoton)
        {
            //Rebote infinito. Sobreescribe el rebote del jugador con el Super Salto
            PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
            if (player != null) player.AplicarImpulso(impulsoTrampolin);
        }
        
    }
}
