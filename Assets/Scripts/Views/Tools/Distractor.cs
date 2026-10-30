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

    [Header("Anti-enganche (evita que salga anclado a tus pies)")]
    [SerializeField] private float graciaInicial = 0.25f; // segundos antes de poder anclarse



    private Rigidbody rb;
    private bool yaActivado = false;
    private float tiempoCreacion;



    void Start()
    {
        rb = GetComponent<Rigidbody>();
        tiempoCreacion = Time.time;


        // 1. Ignorar el colisor del jugador durante el vuelo (CharacterController no lo bloquea)
        GameObject objJugador = GameObject.FindGameObjectWithTag("Player");
        if (objJugador != null)
        {
            CharacterController cc = objJugador.GetComponent<CharacterController>();
            Collider miCollider = GetComponent<Collider>();
            if (cc != null && miCollider != null) Physics.IgnoreCollision(miCollider, cc, true);
        }

        // 2. Lanzamiento en arco hacia donde mira el jugador (ya instanciamos con SU rotación)
        Vector3 fuerzaLanzamiento = (transform.forward * fuerzaAdelante) + (transform.up * fuerzaArriba);
        rb.AddForce(fuerzaLanzamiento, ForceMode.Impulse);

        // 3. Giro aleatorio para realismo físico
        rb.AddTorque(new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f)));

        // 4. Limpieza de memoria (Offline-First) garantizada
        Destroy(gameObject, tiempoDeVida);
    }


    // El distractor se activa al chocar contra el entorno (suelo o paredes)
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player")) return;         // NUNCA se ancla contra el jugador
        if (Time.time - tiempoCreacion < graciaInicial) return;        // fuera de la "mano": sin choque instantáneo

        if (!yaActivado)
        {
            GenerarRuido();
            rb.isKinematic = true;   // se ancla al suelo/pared
            yaActivado = true;
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
