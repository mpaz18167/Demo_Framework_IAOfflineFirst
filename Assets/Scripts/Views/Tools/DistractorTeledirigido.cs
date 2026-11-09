using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
public class DistractorTeledirigido : MonoBehaviour
{
    public static event System.Action<bool> OnFlashResultado;

    [Header("Vuelo")]
    public float velocidad = 14f;
    public float giro = 6f;              // suavidad de la curva hacia el jefe
    public float radioImpacto = 3f;      // a esta distancia del jefe = impacta
    public float tiempoDeVida = 6f;      // si nunca llega, se apaga solo (= falló)

    [Header("Efecto")]
    [Tooltip("Probabilidad de aturdir al impactar (0.85 = 85%; el 15% restante FALLA)")]
    [Range(0f, 1f)] public float chanceDeAcierto = 0.85f;
    public float duracionAturdimiento = 2f;

    [Header("Impacto instantáneo a corta distancia")]
    [Tooltip("Si al lanzarlo el jefe está a esta distancia o menos con línea de vista, impacta SIN vuelo")]
    public float radioHitscan = 5f;

    private Transform objetivo;
    private bool resuelto = false;

    void Start()
    {
        GetComponent<Rigidbody>().isKinematic = true; // lo mueve el script, no la física

        EnemigoIA jefe = FindAnyObjectByType<EnemigoIA>();
        if (jefe != null) objetivo = jefe.transform;
        else Debug.LogWarning("[FlashTeledirigido] No hay EnemigoIA: vuela en línea recta.");

        if (jefe != null
            && Vector3.Distance(transform.position, jefe.transform.position) <= radioHitscan
            && HayLineaDeVista(jefe))
        {
            objetivo = jefe.transform;
            Impactar(); // resuelve acierto (85%) / fallo (15%) al instante
            return;     // no sigue el resto de Start
        }

        Destroy(gameObject, tiempoDeVida); // nunca llega = se apaga
    }

    void Update()
    {
        if (resuelto) return;

        if (objetivo != null)
        {
            Vector3 dir = (objetivo.position + Vector3.up * 1.2f - transform.position).normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), giro * Time.deltaTime);
        }
        transform.position += transform.forward * velocidad * Time.deltaTime;

        if (objetivo != null && Vector3.Distance(transform.position, objetivo.position) <= radioImpacto)
            Impactar();
    }

    private void Impactar()
    {
        if (resuelto) return;
        resuelto = true;

        EnemigoIA jefe = objetivo != null ? objetivo.GetComponent<EnemigoIA>() : null;
        if (jefe == null) { Destroy(gameObject); return; }

        // NUEVO: pequeña chance de fallar (el flash se apaga en el aire)
        if (Random.value <= chanceDeAcierto)
        {
            jefe.AturdirFlash(duracionAturdimiento);
            OnFlashResultado?.Invoke(true);
            Debug.Log($"[FlashTeledirigido] ¡IMPACTO! Jefe aturdido {duracionAturdimiento}s.");
        }
        else
        {
            OnFlashResultado?.Invoke(false);
            Debug.Log("[FlashTeledirigido] ¡FALLÓ! El flash se apagó antes de time.");
        }

        


        Destroy(gameObject);
    }

    // Chocó una pared/obstáculo antes de llegar → FALLA (sin suerte contra el mundo)
    private void OnTriggerEnter(Collider other)
    {
        if (resuelto || other.CompareTag("Player")) return;
        if (objetivo != null && other.transform.root == objetivo.root) return; // chocar con el jefe no es falla
        resuelto = true;
        OnFlashResultado?.Invoke(false);
        Debug.Log("[FlashTeledirigido] ¡FALLÓ! Chocó con un obstáculo.");
        Destroy(gameObject);
    }
    private void OnDisable()
    {
        // Si murió sin resolverse (se agotó el tiempo de vida) = FALLÓ
        if (!resuelto) OnFlashResultado?.Invoke(false);
    }


    private bool HayLineaDeVista(EnemigoIA jefe)
    {
        Vector3 origen = transform.position;
        Vector3 destino = jefe.transform.position + Vector3.up * 1.2f;
        Vector3 dir = destino - origen;

        RaycastHit[] hits = Physics.RaycastAll(origen, dir.normalized, dir.magnitude, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit h in hits)
        {
            if (h.collider.isTrigger) continue;                             // triggers no bloquean
            if (h.collider.transform.root == jefe.transform.root) return true; // lo primero sólido es ÉL
            if (h.collider.CompareTag("Player")) continue;                  // tu cuerpo no se interpone
            return false;                                                   // pared en medio = no hay vista
        }
        return true;
    }
}
