using Unity.Cinemachine;
using UnityEngine;

public class CambioCamara : MonoBehaviour
{
    [Header("Arrastrar aquí la camara de esta zona")]
    public CinemachineCamera camaraAsginada;

    // Usamos 'static' para que todas las puertas del juego compartan esta memoria
    private static CinemachineCamera camaraActivaActual;

    private void OnTriggerEnter(Collider other)
    {
        //Si el jugador entra al cubo invisible esta camara toma el control
        if (other.CompareTag("Player") && camaraAsginada != null)
        {

            //Si el jugador choca con la puerta de la camara que ya esta activa, se ignora
            if (camaraActivaActual == camaraAsginada) return;

            //Apagamos la camara anterior (si existe una)
            if (camaraActivaActual != null)
            {
                camaraActivaActual.Priority = 10;
            }

            //Encendemos la nueva camara
            camaraAsginada.Priority = 20;

            //Guardar esta nueva camara en la memoria globla
            camaraActivaActual = camaraAsginada;
            camaraAsginada.Prioritize();
            Debug.Log($"[CinemachineCamera] Transicion suave a: {camaraAsginada.name}");
        }
    }
}
