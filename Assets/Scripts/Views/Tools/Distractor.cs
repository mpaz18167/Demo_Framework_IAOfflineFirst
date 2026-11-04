using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Distractor : MonoBehaviour
{
    [Header("Físicas de Lanzamiento (Estilo Cypher)")]
    public float fuerzaAdelante = 10f;
    public float fuerzaArriba = 5f;

    [Header("Configuración del Ruido")]
    public float radioDeSonido = 15f; // ya no limita el oído (solo referencia)
    public float tiempoDeVida = 5f;

    [Header("Anti-enganche (evita que salga anclado a tus pies)")]
    [SerializeField] private float graciaInicial = 0.25f;

    [Header("Cadena: buscar y activar TNT/Nitro")]
    [Tooltip("Radio para ENCONTRAR la caja más cercana")]
    public float radioCadena = 5f;
    [Tooltip("Velocidad a la que la mina viaja hasta la caja")]
    public float velocidadBusqueda = 8f;
    [Tooltip("Distancia a la que la mina la activa")]
    public float margenActivacion = 1.2f;

    private Rigidbody rb;
    private bool yaActivado = false;
    private float tiempoCreacion;
    private Vector3 posicionLanzamiento;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        tiempoCreacion = Time.time;
        posicionLanzamiento = transform.position;

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


    void Update()
    {
        if (yaActivado || rb == null) return;
        if (Time.time - tiempoCreacion < graciaInicial) return;

        if (rb.IsSleeping()) // ya cayó y se quedó quieta en el suelo
        {
            yaActivado = true;
            rb.isKinematic = true;
            StartCoroutine(ActivarYAlertar());
        }
    }


    // El distractor se activa al chocar contra el entorno (suelo o paredes)
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player")) return;         // NUNCA se ancla contra el jugador
        if (Time.time - tiempoCreacion < graciaInicial) return;        // fuera de la "mano": sin choque instantáneo

        if (!yaActivado)
        {
            yaActivado = true;
            rb.isKinematic = true;   // se ancla: ahora lo mueve el script
            StartCoroutine(ActivarYAlertar());
        }
    }

    // NUEVO: la mina BUSCA la TNT/Nitro más cercana, VIAJA hasta ella y la activa
    // con el mismo efecto que si un jugador la pisara/golpeara. Después hace el ruido.
    private IEnumerator ActivarYAlertar()
    {
        CajaTNT tntObjetivo = null;
        CajaNitro nitroObjetivo = null;
        float mejorDist = float.MaxValue;

        BuscarCajaEnRango(transform.position, ref tntObjetivo, ref nitroObjetivo, ref mejorDist);
        BuscarCajaEnRango(posicionLanzamiento, ref tntObjetivo, ref nitroObjetivo, ref mejorDist);

        if (tntObjetivo == null && nitroObjetivo == null)
            Debug.Log("[Distractor] Sin TNT/Nitro en rango. Solo ruido.");

        Transform destino = tntObjetivo != null ? tntObjetivo.transform
                         : nitroObjetivo != null ? nitroObjetivo.transform : null;

        // 1) VIAJAR hasta la caja
        if (destino != null)
        {
            Debug.Log("[Distractor] ¡Encontré una caja! Viajando para activarla...");
            while (destino != null && Vector3.Distance(transform.position, destino.position) > margenActivacion)
            {
                transform.position = Vector3.MoveTowards(transform.position, destino.position, velocidadBusqueda * Time.deltaTime);
                yield return null; // ← este yield es el que lo hace corrutina
            }
        }

        // 2) ACTIVAR con los argumentos CORRECTOS
        if (tntObjetivo != null)
            tntObjetivo.RecibirDaño(false, true, transform.position);   // PISOTON → cuenta regresiva (tic-tac)
        else if (nitroObjetivo != null)
            nitroObjetivo.RecibirDaño(true, false, transform.position); // INMEDIATO (como si lo tocaras)

        // 3) Ruido global + flash
        GenerarRuido();
    }

    private void BuscarCajaEnRango(Vector3 centro, ref CajaTNT tntOut, ref CajaNitro nitroOut, ref float mejorDist)
    {
        foreach (Collider c in Physics.OverlapSphere(centro, radioCadena))
        {
            CajaTNT tnt = c.GetComponentInParent<CajaTNT>();
            CajaNitro nitro = c.GetComponentInParent<CajaNitro>();
            if (tnt == null && nitro == null) continue;

            float d = Vector3.Distance(transform.position, c.transform.position);
            if (d < mejorDist)
            {
                mejorDist = d;
                tntOut = tnt;
                nitroOut = nitro;
            }
        }
    }

    private void GenerarRuido()
    {
        // OÍDO GLOBAL: sin límite de radio, alerta a TODOS los enemigos (antes: 15 m y solo uno)
        EnemigoIA[] enemigos = FindObjectsByType<EnemigoIA>(FindObjectsInactive.Exclude);

        foreach (EnemigoIA enemigo in enemigos)
        {
            // FLASH (arma): solo si está cerca Y no hay nada en medio (línea de vista clara)
            float distancia = Vector3.Distance(transform.position, enemigo.transform.position);
            if (distancia <= enemigo.radioFlash && HayLineaDeVista(enemigo))
            {
                enemigo.AturdirFlash(enemigo.duracionFlash); // UNA vez por mina
            }

            // CEBO: siempre va hacia la explosión, desde donde esté
            enemigo.AlertarPorRuido(transform.position);
        }

        Debug.Log("[Distractor] ¡El enemigo escuchó la explosión!");
    }

    // NUEVO: ¿hay pared/obstáculo entre la mina y el enemigo? (condición del flash)
    private bool HayLineaDeVista(EnemigoIA enemigo)
    {
        Vector3 origen = transform.position + Vector3.up * 0.3f;
        Vector3 destino = enemigo.transform.position + Vector3.up * 1.5f; // altura de los ojos del jefe
        Vector3 dir = destino - origen;

        if (Physics.Raycast(origen, dir.normalized, out RaycastHit hit, dir.magnitude, ~0, QueryTriggerInteraction.Collide))
            return hit.collider.transform.root == enemigo.transform.root; // choca primero con ÉL = nada en medio

        return true; // no chocó con nada → hay línea de vista
    }
}
