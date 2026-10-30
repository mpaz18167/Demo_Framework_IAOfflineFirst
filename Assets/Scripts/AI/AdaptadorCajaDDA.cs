using UnityEngine;

public class AdaptadorCajaDDA : MonoBehaviour
{
    public enum NivelPeligro { Seguro, RiesgoLeve, PeligroMortal }

    [Header("Estado Actual de la Caja")]
    public NivelPeligro estadoActual = NivelPeligro.Seguro;

    [Header("Variantes (Arrastra los GameObjects Hijos)")]
    [SerializeField] private GameObject cajaSegura;  // Ej: CajaNormal o CajaPuntos
    [SerializeField] private GameObject cajaRiesgo;  // Ej: CajaTNT
    [SerializeField] private GameObject cajaMortal;  // Ej: CajaNitro

    private Camera camaraPrincipal;
    private GameController gameController;

    void Awake()
    {
        camaraPrincipal = Camera.main;
    }

    void Start()
    {
        gameController = FindAnyObjectByType<GameController>();
        ActualizarVisual();
    }

    void OnEnable()
    {
        // 1. Escucha en tiempo real (requiere estar fuera de cámara)
        GameController.OnDificultadCambiada += EvaluarEnVivo;
        // 2. Escucha cuando mueres (ignora la cámara porque la pantalla está negra)
        GameController.OnReconfiguracionEntorno += EvaluarEnRespawn;
    }

    void OnDisable()
    {
        GameController.OnDificultadCambiada -= EvaluarEnVivo;
        GameController.OnReconfiguracionEntorno -= EvaluarEnRespawn;
    }

    private void EvaluarEnVivo(float factorDificultad)
    {
        // Anti-Popping: Si la cámara lo está viendo, abortamos la mutación.
        if (EstaVisibleEnCamara()) return;
        MutarCaja(factorDificultad);
    }

    private void EvaluarEnRespawn()
    {
        if (gameController != null)
        {
            //Como el jugador esta respawneando, no importa si la cámara lo ve o no, se puede mutar la caja.
            MutarCaja(gameController.ObtenerFactorDificultad());
        }
    }

    private void MutarCaja(float factor)
    {
        if (factor < 0.95f) estadoActual = NivelPeligro.Seguro;            // Frustración (0.8) → CajaNormal
        else if (factor > 1.15f) estadoActual = NivelPeligro.PeligroMortal; // Aburrimiento (1.5) → Nitro
        else estadoActual = NivelPeligro.RiesgoLeve;                        // Flow (1.0) → TNT

        ActualizarVisual();
    }

    private void ActualizarVisual()
    {
        if (cajaSegura != null) cajaSegura.SetActive(estadoActual == NivelPeligro.Seguro);
        if (cajaRiesgo != null) cajaRiesgo.SetActive(estadoActual == NivelPeligro.RiesgoLeve);
        if (cajaMortal != null) cajaMortal.SetActive(estadoActual == NivelPeligro.PeligroMortal);
    }

    private bool EstaVisibleEnCamara()
    {
        if (camaraPrincipal == null) return false;
        Vector3 vp = camaraPrincipal.WorldToViewportPoint(transform.position);
        // Retorna true si está dentro de los márgenes de la pantalla y frente a la cámara
        return vp.z > 0 && vp.x >= -0.1f && vp.x <= 1.1f && vp.y >= -0.1f && vp.y <= 1.1f;
    }
}
