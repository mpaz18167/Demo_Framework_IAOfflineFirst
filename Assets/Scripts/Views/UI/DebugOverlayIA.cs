using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

namespace Views.UI
{
    public class DebugOverlayIA : MonoBehaviour
    {
        [Header("Contenedor Visual")]
        [Tooltip("El objeto hijo Panel que contiene los textos. Este script NUNCA debe estar en el objeto que se desactiva.")]
        [SerializeField] private GameObject panelContenedor;
        [Header("Referencias de UI (TextMeshPro)")]
        [SerializeField] private TextMeshProUGUI txtEstadoDDA;
        [SerializeField] private TextMeshProUGUI txtIndiceCp;
        [SerializeField] private TextMeshProUGUI txtEstadoRed;
        [SerializeField] private TextMeshProUGUI txtMuertesCheckpoint;

        private PlayerControls controls;
        private GameController gameController; // NUEVO: cache (el proyecto NO usa singletons)
        private DirectorIA director;           // NUEVO: cache
        private float temporizadorRefresco = 0f;
        private const float INTERVALO_REFRESCO = 0.1f; // 10 veces por segundo (tiempo real suave)

        private void Awake()
        {
            InicializarControles();
        }

        private void InicializarControles()
        {
            if (controls == null)
            {
                controls = new PlayerControls();
                // Acción ToggleDebug (F2) — vive en el mapa "Player" del Input Action Asset
                controls.Player.ToggleDebug.performed += OnToggleDebugPerformed;
            }
        }

        private void OnEnable()
        {
            InicializarControles();
            controls?.Player.Enable();
        }

        private void OnDisable()
        {
            controls?.Player.Disable();
        }

        private void OnDestroy()
        {
            if (controls != null)
            {
                controls.Player.ToggleDebug.performed -= OnToggleDebugPerformed;
                controls.Dispose();
                controls = null;
            }
        }

        private void Start()
        {
            // Mismo patrón que DirectorIA/EnemigoIA: buscar y cachear
            gameController = FindAnyObjectByType<GameController>();
            director = FindAnyObjectByType<DirectorIA>();

            // Si no se asignó un panel hijo, usamos el primer hijo de este objeto
            if (panelContenedor == null && transform.childCount > 0)
            {
                panelContenedor = transform.GetChild(0).gameObject;
            }
            // Inicia apagado por defecto (pero el script sigue vivo escuchando F2)
            if (panelContenedor != null) panelContenedor.SetActive(false);
        }

        private void OnToggleDebugPerformed(InputAction.CallbackContext context)
        {
            if (panelContenedor != null)
            {
                bool nuevoEstado = !panelContenedor.activeSelf;
                panelContenedor.SetActive(nuevoEstado);
                if (nuevoEstado)
                {
                    RefrescarMetricasEnTiempoReal();
                }
            }
        }

        private void Update()
        {
            // Solo consumimos recursos de actualización si el panel está visible
            if (panelContenedor != null && panelContenedor.activeSelf)
            {
                temporizadorRefresco += Time.unscaledDeltaTime;
                if (temporizadorRefresco >= INTERVALO_REFRESCO)
                {
                    temporizadorRefresco = 0f;
                    RefrescarMetricasEnTiempoReal();
                }
            }
        }

        private void RefrescarMetricasEnTiempoReal()
        {
            // Respaldo por si Start corrió antes que el GameController/DirectorIA
            if (gameController == null) gameController = FindAnyObjectByType<GameController>();
            if (director == null) director = FindAnyObjectByType<DirectorIA>();

            // 1. Estado de Red (Offline / Online)
            if (txtEstadoRed != null && gameController != null)
            {
                bool esOffline = gameController.simularSinConexion;
                txtEstadoRed.text = esOffline ? "<color=#FFA500>RED: OFFLINE (Local JSON)</color>"
                                              : "<color=#00FF00>RED: ONLINE (Firebase)</color>";
            }

            // 2. Métricas de DDA e Índice de Flujo
            if (director != null)
            {
                if (txtEstadoDDA != null)
                {
                    string estado = director.EstadoActual.ToString();
                    Color colorEstado = Color.white;
                    switch (director.EstadoActual)
                    {
                        case DirectorIA.EstadoDDA.Frustracion:
                            colorEstado = new Color(0.3f, 0.8f, 1f); // Cian (modo ayuda)
                            break;
                        case DirectorIA.EstadoDDA.Aburrimiento:
                            colorEstado = new Color(1f, 0.5f, 0.2f); // Naranja (modo reto)
                            break;
                        case DirectorIA.EstadoDDA.ZonaFlow:
                            colorEstado = Color.green;
                            break;
                    }
                    float dif = gameController != null ? gameController.ObtenerFactorDificultad() : 1f;
                    txtEstadoDDA.text = $"DDA: <color=#{ColorUtility.ToHtmlStringRGB(colorEstado)}>{estado}</color> (Dif: {dif:F2})";
                }
                if (txtIndiceCp != null)
                {
                    float cp = director.UltimoIndiceAptitud;
                    txtIndiceCp.text = $"Rendimiento (Cp): {cp:F2} / 1.00";
                }
            }
            else
            {
                if (txtEstadoDDA != null) txtEstadoDDA.text = "DDA: Esperando Director...";
            }

            // 3. Muertes en Checkpoint actual (Escudo de piedad)
            if (txtMuertesCheckpoint != null && gameController != null)
            {
                int muertes = gameController.MuertesConsecutivasEnCheckpoint;
                txtMuertesCheckpoint.text = $"Muertes Checkpoint: {muertes}/3 {(muertes >= 3 ? "<color=#FFFF00>[ESCUDO ACTIVO]</color>" : "")}";
            }
        }
    }
}