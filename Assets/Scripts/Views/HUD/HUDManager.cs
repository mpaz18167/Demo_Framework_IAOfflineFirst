using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class HUDManager : MonoBehaviour
{

    [Header("Referencias del Canvas")]
    [SerializeField] private TextMeshProUGUI textoVidas;
    [SerializeField] private TextMeshProUGUI textoPuntos;
    [SerializeField] private TextMeshProUGUI textoTiempo;
    [SerializeField] private Image indicadorRed; // Verde = Online, Naranja = Offline-First

    [Header("Colores de red (CN-10)")]
    [SerializeField] private Color colorOnline = Color.green;
    [SerializeField] private Color colorOffline = new Color(1f, 0.55f, 0f); // Naranja

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
}
