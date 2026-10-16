using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Distractor : MonoBehaviour
{
    [Header("Físicas de Lanzamiento (Estilo Cypher)")]
    public float fuerzaAdelante = 10f;
    public float fuerzaArriba = 5f;

    [Header("Configuración del Ruido")]
    public float radioDeSonido = 15f;
    public float tiempoDeVida = 5f; // Cuánto tiempo dura el objeto antes de desaparecer

    private Rigidbody rb;
    private bool yaActivado = false;
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // 1. Lanzamiento en arco parabólico usando la dirección hacia adelante
        Vector3 fuerzaLanzamiento = (transform.forward * fuerzaAdelante) + (transform.up * fuerzaArriba);
        rb.AddForce(fuerzaLanzamiento, ForceMode.Impulse);

        // 2. Giro aleatorio para darle realismo físico al lanzamiento
        rb.AddTorque(new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f)));

        // 3. Limpieza de memoria (Offline-First) garantizada
        Destroy(gameObject, tiempoDeVida);
    }


    // El distractor se activa al chocar contra el entorno (suelo o paredes)
    private void OnCollisionEnter(Collision collision)
    {
        if (!yaActivado)
        {
            GenerarRuido();

            // Opcional: Anclar la mina al suelo para que no ruede por rampas
            rb.isKinematic = true;

            yaActivado = true; // Evita que genere ruido repetido si rebota
        }
    }

    private void GenerarRuido()
    {
        //Crear Esfera de deteccion fisica
        Collider[] detectados = Physics.OverlapSphere(transform.position, radioDeSonido);

        foreach (Collider col in detectados)
        {
            EnemigoIA enemigo = col.GetComponent<EnemigoIA>();

            if (enemigo != null)
            {
                // Le pasamos las coordenadas exactas de este distractor al enemigo
                enemigo.AlertarPorRuido(transform.position);
                Debug.Log("[Distractor] ¡El enemigo escuchó el ruido!");
                break; // Solo alertamos a un enemigo (el más cercano)
            }
        }
    }
}
