using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static CloudService;


public class HUDManager : MonoBehaviour
{

    [Header("Referencias del Canvas")]
    [SerializeField] private TextMeshProUGUI textoVidas;
    [SerializeField] private TextMeshProUGUI textoPuntos;
    [SerializeField] private TextMeshProUGUI textoTiempo;
    [SerializeField] private TextMeshProUGUI textoRecord;
    [SerializeField] private Image indicadorRed; // Verde = Online, Naranja = Offline-First

    [Header("Colores de red (CN-10)")]
    [SerializeField] private Color colorOnline = Color.green;
    [SerializeField] private Color colorOffline = new Color(1f, 0.55f, 0f); // Naranja

    [Header("Ranking Online (Opcional, con fallback local)")]
    [SerializeField] private GameObject panelRanking;      // Panel del ranking (inactivo por defecto)
    [SerializeField] private TextMeshProUGUI textoRanking; // Un solo TMP multilínea
    [SerializeField] private GameObject indicadorCargando; // Opcional: texto "Cargando..."

    private bool rankingCargando = false; // Evita peticiones duplicadas
    private CloudService cloudService;
    private GameController gc;

    private void OnEnable()
    {
        TelemetryService.OnVidasCambiadas += ActualizarVidas;
        TelemetryService.OnPuntosCambiados += ActualizarPuntos;
        GameController.OnEstadoConexionCambiado += ActualizarRed;
    }

    private void OnDisable()
    {
        TelemetryService.OnVidasCambiadas -= ActualizarVidas;
        TelemetryService.OnPuntosCambiados -= ActualizarPuntos;
        GameController.OnEstadoConexionCambiado -= ActualizarRed;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Estado inicial de la red
        ActualizarRed(Application.internetReachability != NetworkReachability.NotReachable);
        StartCoroutine(ActualizarTiempoCadaSegundo());

        cloudService = FindAnyObjectByType<CloudService>();

        gc = FindAnyObjectByType<GameController>();
        GameController.OnRecordActualizado += ActualizarRecord;
        ActualizarRecord();

        StartCoroutine(InicializarValoresGuardados()); // ← NUEVO
    }

    void OnDestroy()
    {
        GameController.OnRecordActualizado -= ActualizarRecord;

    }

    private void ActualizarVidas(int vidas)
    {
        if (textoVidas) textoVidas.text = $"Vidas: {vidas}";
    }

    private void ActualizarPuntos(int puntos)
    {
        if (textoPuntos) textoPuntos.text = $"Puntos: {puntos}";
    }

    private void ActualizarRed(bool hayConexion)
    {
        if (indicadorRed) indicadorRed.color = hayConexion ? colorOnline : colorOffline;
    }

    // El tiempo se actualiza cada segundo (corrutina, no satura el Update)
    private IEnumerator ActualizarTiempoCadaSegundo()
    {
        var espera = new WaitForSeconds(1f);
        while (true)
        {
            if (textoTiempo)
            {
                int t = (int)Time.timeSinceLevelLoad;
                textoTiempo.text = $"Tiempo: {t / 60:00}:{t % 60:00}";
            }
            yield return espera;
        }
    }

    private void ActualizarRecord()
    {
        if (textoRecord)
        {
            textoRecord.text = $"Record: {(gc != null ? gc.ObtenerPuntajeGlobal() : 0)}";
        }
    }

    // Asociar al Botón "Ranking" y al Botón "Cerrar"
    public void AlternarRanking()
    {
        if (panelRanking == null) return;
        bool abierto = !panelRanking.activeSelf;
        panelRanking.SetActive(abierto);
        if (abierto) _ = CargarRankingSeguroAsync();
    }

    private async Task CargarRankingSeguroAsync()
    {
        if (rankingCargando || gc == null) return; // Candado anti-peticiones paralelas
        rankingCargando = true;
        if (indicadorCargando) indicadorCargando.SetActive(true);

        try
        {

            // CASO 1: sin red → respuesta inmediata, cero peticiones
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                MostrarRankingLocal("SIN CONEXIÓN — MODO LOCAL");
                return;
            }
            if (cloudService == null)
            {
                MostrarRankingLocal("SERVICIO NO DISPONIBLE — MODO LOCAL");
                return;
            }

            List<EntradaRanking> top = await cloudService.ObtenerTop10Ranking();

            // CASO 2: hubo red pero el servidor no respondió (permisos, timeout, error → ver warning "Ranking" en consola)
            if (top == null)
            {
                MostrarRankingLocal("SERVIDOR NO RESPONDIÓ — MODO LOCAL");
                return;
            }

            // CASO 3: conexión OK pero aún nadie ha subido puntaje
            if (top.Count == 0)
            {
                if (textoRanking)
                    textoRanking.text = $"TOP JUGADORES (Online)\n\nAún no hay puntajes registrados.\n¡Termina una partida para ser el primero!\n\nTu récord local: {gc.ObtenerPuntajeGlobal()}";
                return;
            }

            StringBuilder sb = new StringBuilder("TOP JUGADORES (Online)\n\n");

            for (int i = 0; i < top.Count; i++)
                sb.AppendLine($"{i + 1}. {top[i].alias} — {top[i].score}");

            if (textoRanking) textoRanking.text = sb.ToString();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[HUD] Ranking: {e.Message}");
            MostrarRankingLocal("ERROR INESPERADO — MODO LOCAL");
        }
        finally
        {
            if (indicadorCargando) indicadorCargando.SetActive(false);
            rankingCargando = false;
        }
    }

    private void MostrarRankingLocal(string motivo)
    {
        if (textoRanking)
            textoRanking.text = $"{motivo}\n\nTu récord histórico:\n\n1. Tú — {gc.ObtenerPuntajeGlobal()}";
    }


    // NUEVO: el GameController carga vidas/puntos en SU Start; esperamos 1 frame para no leer el valor por defecto
    private IEnumerator InicializarValoresGuardados()
    {
        yield return null;
        if (gc != null)
        {
            ActualizarVidas(gc.vidasActuales); // vidas REALES
            ActualizarPuntos(0); // NUEVO: la sesión arranca en 0; el récord ya tiene su label (textoRecord)
        }

    }
}
