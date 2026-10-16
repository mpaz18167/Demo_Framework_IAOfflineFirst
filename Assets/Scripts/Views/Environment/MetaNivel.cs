using UnityEngine;

public class MetaNivel : MonoBehaviour
{
    private bool metaCruzada = false; //Candado para evitar que se active dos veces 

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag ("Player") && !metaCruzada)
        {
            metaCruzada=true;

            //buscar el gameController y avisar que se paso el nivel
            GameController controller = FindAnyObjectByType<GameController>();

            if (controller != null) 
            {
                controller.LlegarALaMeta(1.0f);
                Debug.Log("[MetaNivel] ¡Has cruzado la línea de meta!");
                
            
            }
        }
    }
}
