using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CajaTNT : CajaInteractiva
{
    [SerializeField] private float tiempoDetonacion = 3.0f;
    [SerializeField] private float radioExplosion = 3.5f;

    [Header("Efecto de Respiración Cartoon")]
    [SerializeField] private float intensidadRespiracion = 0.28f; // Cuánto se deforma
    [SerializeField] private GameObject prefabExplosion; 

    private bool enCuentaRegresiva = false;
    private bool yaDetonado = false;
    private Vector3 escalaOriginal;
    private Coroutine rutinaConteo;

    protected override void Start()
    {
        base.Start();
        escalaOriginal = transform.localScale;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        enCuentaRegresiva = false;
        yaDetonado = false;
        if (escalaOriginal != Vector3.zero) transform.localScale = escalaOriginal;
        if (rutinaConteo != null) { StopCoroutine(rutinaConteo); rutinaConteo = null; }

    }

    protected override void RomperCaja(bool esAtaque, bool esPisoton)
    {
        if (yaDetonado) return;

        if (esAtaque) DetonarInmediato();
        else if (esPisoton && !enCuentaRegresiva)
        {
            enCuentaRegresiva = true;
            rutinaConteo = StartCoroutine(RutinaRespiracionTicTac());
        }
    }

    private IEnumerator RutinaRespiracionTicTac()
    {
        float tiempoRestante = tiempoDetonacion;
        float fase = 0f; // NUEVO: fase acumulada. Time.time absoluto con frecuencia variable = parpadeo glichado
        while (tiempoRestante > 0f)
        {
            // Frecuencia suave: empieza en 2 pulsaciones por segundo y sube a 6 al final
            float t = 1f - (tiempoRestante / tiempoDetonacion); // 0 al inicio, 1 al final
            float frecuencia = Mathf.Lerp(2.5f, 6.5f, t);

            // Efecto Squash & Stretch (respiración Cartoon)
            fase += frecuencia * Mathf.PI * 2f * Time.deltaTime;
            float onda = Mathf.Sin(fase); // Oscilación entre -1 y 1
            float deformacion = onda * intensidadRespiracion * Mathf.Lerp(0.3f, 0.8f, t);

            // Se estira en Y y se comprime en X y Z (conservación de volumen estilo Crash)
            transform.localScale = new Vector3(
                escalaOriginal.x * (1f - deformacion * 0.5f),
                escalaOriginal.y * (1f + deformacion),
                escalaOriginal.z * (1f - deformacion * 0.5f)
            );
            tiempoRestante -= Time.deltaTime;
            yield return null;
        }
        transform.localScale = escalaOriginal;
        DetonarInmediato(); 
    }

    private void DetonarInmediato()
    {
        if (yaDetonado) return;
        yaDetonado = true;
        // Instanciar el efecto de explosión visual cartoon
        if (prefabExplosion != null)
        {
            Instantiate(prefabExplosion, transform.position, Quaternion.identity);
        }
        CamaraShake.Sacudir(0.5f, 0.25f); // Sacudida de cámara


        foreach (EnemigoIA e in FindObjectsByType<EnemigoIA>(FindObjectsInactive.Exclude))
            e.AlertarPorRuido(transform.position);

        Collider[] afectados = Physics.OverlapSphere(transform.position, radioExplosion);
        foreach (Collider col in afectados)
        {
            EnemigoIA jefe = col.GetComponentInParent<EnemigoIA>();
            if (jefe != null) jefe.Ralentizar();

            if (col.CompareTag("Player"))
            {
                PlayerMovement player = col.GetComponent<PlayerMovement>();
                if (player != null && !player.esInvulnerable)
                {
                    player.RecibirDaño(false, false, transform.position);
                }
            }
            else if (col.gameObject != this.gameObject)
            {
                IDamage obj = col.GetComponent<IDamage>();
                if (obj != null)
                {
                    obj.RecibirDaño(true, false, transform.position);
                }
            }
        }
        DesactivarCaja();
    }
}
