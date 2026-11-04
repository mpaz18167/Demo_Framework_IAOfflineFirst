using UnityEngine;

// Sacudida de cámara estilo cartoon. Se aplica en LateUpdate DESPUÉS de CinemachineBrain
// (por el DefaultExecutionOrder) para que la cámara no "se coma" el offset.
[DefaultExecutionOrder(10000)]
public class CamaraShake : MonoBehaviour
{

    public static CamaraShake Instancia { get; private set; }
    private float fuerzaActual;
    private float tiempoRestante;


    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(this);
            return;
        }
        Instancia = this;
        
    }

    public static void Sacudir(float fuerza = 035f, float duracion = 0.2f)
    {
        if (Instancia == null) return;

        // FIX: antes solo arrancaba si YA había una sacudida en curso (nunca la primera)
        Instancia.fuerzaActual = fuerza;
        Instancia.tiempoRestante = Mathf.Max(Instancia.tiempoRestante, duracion);
    }

    private void LateUpdate()
    {
        if (tiempoRestante <= 0f || Time.timeScale <= 0f) return;
        tiempoRestante -= Time.deltaTime;

        // Se SUMA encima de donde CinemachineBrain dejó la cámara este frame
        // (el Brain la reubica cada frame → el offset no se acumula jamás)
        Vector3 offset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * fuerzaActual;
        transform.localPosition += offset;
    }

}
