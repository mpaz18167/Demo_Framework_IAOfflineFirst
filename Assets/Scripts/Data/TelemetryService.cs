using UnityEngine;
using UnityEngine.InputSystem;
// Clase para manejar la telemetría de la sesión del jugador
//Las formulas planteadas en el plan de tesis para la medicion se pueden colocar en este script.

public class TelemetryService : MonoBehaviour
{
    private SessionData currentSession;
    private bool isSessionActive = false;
    private PlayerControls controls;

    [Header("Pesos Ecuación ISO/IEC 25010")]
    public float w1 = 0.3f; // Peso para la variable de Nivel de dificultad - D
    public float w2 = 0.2f; // Peso para la variable de eficiencia de tiempo - T
    public float w3 = 0.2f;// Peso para la variable de Eficiencia de Movimientos - M
    public float w4 = 0.3f;// Peso para la variable de Puntaje Final - P

    //Eventos para el HUD
    public delegate void EnteroDelegate(int valor);
    public static event EnteroDelegate OnVidasCambiadas;
    public static event EnteroDelegate OnPuntosCambiados;

    void Awake()
    {
        controls = new PlayerControls();

        controls.Player.Move.performed += ctx => RegistrarMovimientoM(); //Registra Movimiento cuando camina
        controls.Player.Jump.performed += ctx => RegistrarMovimientoM(); //Registrar movimiento cuando salta.
    }

    void OnEnable()
    {
        controls.Player.Enable();
    }

    void OnDisable()
    {
        controls.Player.Disable();
    }

    public void IniciarSesion (float DificultadInicial, string idPlayer, bool timeTrialMode= false) 
    {
        currentSession = new SessionData ();
        currentSession.playerId = idPlayer;
        currentSession.dificultty_D = DificultadInicial;
        currentSession.isTimeTrial = timeTrialMode;
        currentSession.zoneType = "Plataform";

        // NUEVO: Fijar el valor Z ideal para el nivel (Ej: 15 acciones perfectas)
        currentSession.idealReferenceValue_Z = 15f;

        isSessionActive = true;
        Debug.Log("[TelemetryService] Medicion iniciada");
    }

    public void cambiarZona(string nuevaZona)
    {
        if (isSessionActive)
        {
            currentSession.zoneType = nuevaZona;
            Debug.Log($"[TelemetryService] Zona cambiada a: {nuevaZona}");
        }
    }

    public void RestarTiempoCaja(float segundos)
    {
        if (isSessionActive && currentSession!=null && currentSession.isTimeTrial)
        {
            currentSession.time_T -= segundos; // Restar tiempo a la sesión
            
            // Evitar que el tiempo sea negativo
            if (currentSession.time_T < 0)
            {
                currentSession.time_T = 0;
                Debug.Log($"[TelemetryService] ¡El tiempo se ha agotado!");
            }
            else
            {
                Debug.Log($"[TelemetryService] Tiempo restado: {segundos} segundos. Tiempo restante: {currentSession.time_T} segundos.");
            }
        }
    }

    public void ActualizarProgreso(int checkpoints, float porcentajeNivel)
    {
        // Actualizar el progreso del jugador en la sesión
        if (isSessionActive)
        {
            currentSession.checkpointsReached = checkpoints;// Actualizar el número de checkpoints alcanzados
            currentSession.levelProgress = porcentajeNivel;// Actualizar el porcentaje de progreso del nivel
            Debug.Log($"[TelemetryService] Progreso actualizado: {checkpoints} checkpoints alcanzados, {porcentajeNivel * 100}% del nivel completado.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!isSessionActive) return;// Si la sesión no está activa, no hacer nada

        currentSession.time_T += Time.deltaTime; // Incrementar el tiempo de la sesión

        //Detectar caidas de FPS (Si el frame tarda mas de 0.033 segundos = menos de 30 FPS)
        if (Time.deltaTime > 0.0333f)
        {
            RegistrarCaidaFPS(Time.deltaTime);
        }

        // Sumamos tiempo sin fallos para la Estabilidad (Asumiendo que si Update corre, no hay crasheo)
        currentSession.crashFreeTime += Time.deltaTime;
    }

    // Registrar un movimiento del jugador (M) cada vez que se presiona un botón
    public void RegistrarMovimientoM()
    {
        if (isSessionActive)
        {
            currentSession.movement_M ++; // Incrementar el contador de movimientos
        }
    }

    public void RegistrarPuntaje(int points)
    {
        //En contrareloj no se suman puntos, solo se registra el tiempo y los movimientos.
        if (isSessionActive && !currentSession.isTimeTrial) 
        {
            currentSession.points_P += points;
            OnPuntosCambiados?.Invoke(currentSession.points_P);
        }
    }

    //NUEVO: Registrar una muerte para el calculo  de la Dificultad Objetiva (OGD)
    public void RegistrarMuerte()
    {
        if (isSessionActive && currentSession != null)
        {
            currentSession.deaths++;
            Debug.Log($"[TelemetryService] Muerte Registrada. Total muertes en sesión: {currentSession.deaths}");
        }
    }


    public SessionData FinalizarSesionYCalcular()
    {

        //Proteccion contra race conditions (Si el jugador muere antes de que cargue la IA)
        if (currentSession == null)
        {
            Debug.LogWarning("[TelemetryService] Intento de finalizar una sesión nula. Abortando cálculo.");
            return null;
        }

        isSessionActive = false;

        // Definir si el DDA estuvo activo en esta sesión
        GameController gc = FindAnyObjectByType<GameController>();
        if (gc != null) currentSession.isDDAActive = gc.activarDDA;

        CalcularMetricasDeMatriz(currentSession);


        Debug.Log($"[TelemetryService] Dificultad del jugador: {currentSession.dificultty_D}");
        Debug.Log($"[TelemetryService] Puntos del jugador: {currentSession.points_P}");
        Debug.Log($"[TelemetryService] Tiempo del jugador: {currentSession.time_T}");
        Debug.Log($"[TelemetryService] Movimiento del Jugador: {currentSession.movement_M}");
        Debug.Log($"[TelemetryService] Índice de Operabilidad final: {currentSession.opperabilty:F2}");
        Debug.Log($"[TelemetryService] OGD: {currentSession.ogd:F2}");


        return currentSession;
    }

    /*====================================================================================================
     =============  Funcion para calcular las ecuaciones de la matriz de operacionalizacion  =============
    ==================================================================================================== */

    public void CalcularMetricasDeMatriz(SessionData sesion)
    {
        //Dificultad Objetiva del juego (OGD)
        sesion.ogd = sesion.points_P / (sesion.deaths + 1f);


        //Indice de Pérdida de Vida por Encuentro
        if (sesion.startingLives > 0)
        {
            sesion.lifeLossIndex = 1f - ((float)sesion.remainingLives / sesion.startingLives);

        }

        //Indice de Flujo
        if (sesion.time_T > 0)
        {
            sesion.flowIndex = (sesion.points_P / sesion.time_T) * sesion.remainingLives; 
        }

        //Dificultad Objetiva por Ticks (D)
        if (sesion.telemetryTicks_D > 0)
        {
            sesion.difficultyByTicks = (sesion.actionsPerformed_N - sesion.idealReferenceValue_Z) / (float)sesion.telemetryTicks_D;
        }

        //Calculo de Facilidad Percibida (EASE)
        sesion.ease = 1f - sesion.difficultyByTicks;

        //Rendimiento del Ajuste Dinamico de Dificultad (Performance DDA)
        if (sesion.expectedPerformance_Ep > 0)
        {
            sesion.performanceDDA = sesion.currentPerformance_Cp / sesion.expectedPerformance_Ep;
        }

        //Disponibilidad Operativa Continua (Uptime) 
        // --- ESTABILIDAD DEL JUEGO ---
        if (sesion.time_T > 0)
        {
            float activeTime = sesion.time_T - sesion.fpsDropTime;
            sesion.uptimePercentage = (activeTime / sesion.time_T) * 100f;

            sesion.stabilityPercentage = (sesion.crashFreeTime / sesion.time_T) * 100f;
        }

        //Tasa de Eficiencia de Sincronizacion Offline
        if (sesion.syncAttempts>0)
        {
            sesion.syncSuccessRate = ((float)sesion.successfulSyncs / sesion.syncAttempts) * 100f;
        }

        //Tasa de éxito en la generacion de rutas validas (Procedural)
        if (sesion.totalProceduralAttempts> 0)
        {
            sesion.routeSuccessRate = ((float)sesion.successfulProceduralSpawns / sesion.totalProceduralAttempts) * 100f;

        }

        // --- MÉTRICAS DE VALIDACIÓN IA ---
        // 1. PRECISIÓN = VP / (VP + FP)
        float sumaPrecision = sesion.verdaderosPositivos + sesion.falsosPositivos;
        if (sumaPrecision > 0)
        {
            sesion.precisionIA = (float)sesion.verdaderosPositivos / sumaPrecision;
        }

        // 2. SENSIBILIDAD (RECALL) = VP / (VP + FN)
        float sumaRecall = sesion.verdaderosPositivos + sesion.falsosNegativos;
        if (sumaRecall > 0)
        {
            sesion.recallIA = (float)sesion.verdaderosPositivos / sumaRecall;
        }

        // =========================================================
        // 4. F1-SCORE (Basado en Precision y Recall)
        // Fórmula: F1 = 2 * ((PRECISION * RECALL) / (PRECISION + RECALL))
        // =========================================================
        float sumaPrecisionRecall = sesion.precisionIA + sesion.recallIA;
        if (sumaPrecisionRecall > 0)
        {
            sesion.f1ScoreIA = 2f * ((sesion.precisionIA * sesion.recallIA) / sumaPrecisionRecall);
        }

        // 3. ERROR ABSOLUTO MEDIO (MAE)
        if (sesion.totalPrediccionesIA > 0)
        {
            sesion.maeIA = sesion.sumaErroresAbsolutos / sesion.totalPrediccionesIA;
        }

    

        //Indice de Operabilidad ISO/IEC 25010
        // Ecuación 1: Índice de Operabilidad ISO/IEC 25010
        float tSafe = sesion.time_T > 0 ? sesion.time_T : 1f; // Evitar división por cero
        float mSafe = sesion.movement_M > 0 ? sesion.movement_M : 1f; // Evitar división por cero

        // Extraemos el total calculado dinámicamente
        GameController gc = FindAnyObjectByType<GameController>();
        int maxPuntos = (gc != null && gc.puntosTotalesGenerados > 0) ? gc.puntosTotalesGenerados : 1;

        // Calculamos P_norm (Rango de 0.0 a 1.0)
        float pNorm = (float)sesion.points_P / maxPuntos;

        float factorD = w1 * ((4f - sesion.dificultty_D) / 3f);
        float factorT = w2 * (1f / tSafe);
        float factorM = w3 * (1 / mSafe);
        float factorP = w4 * pNorm;
        sesion.opperabilty = factorD + factorT + factorM + factorP;

    }

    // =================================================================
    // MÉTODOS DE CAPTURA EN TIEMPO REAL PARA LA MATRIZ
    // =================================================================

    // Captura de acciones para la Dificultad por Ticks (Ej: Saltar o Esquivar)
    public void RegistrarAccionJugador()
    {
        if (isSessionActive && currentSession != null)
        {
            currentSession.actionsPerformed_N++;
        }
    }

    // Actualiza cuántas vidas le quedan al jugador
    public void ActualizarVidasRestantes(int vidas)
    {
        if (isSessionActive && currentSession != null)
        {
            currentSession.remainingLives = vidas;
            OnVidasCambiadas?.Invoke(vidas);// Notificar al HUD del cambio de vidas
        }
    }

    //Registra un intento de sincronización en una sesión concreta (funciona incluso si la sesión ya fue finalizada)
    public void RegistrarSincronizacionEnSesion(SessionData sesion, bool fueExitosa, float latenciaMs)
    {
        if (sesion == null) return;

        sesion.syncAttempts++;
        if (fueExitosa) sesion.successfulSyncs++;

        //Guardar la latencia del ultimo intento y recalcular la Tasa de Eficiencia de Sincronización (%)
        sesion.syncLatencyMs = latenciaMs;
        sesion.syncSuccessRate = ((float)sesion.successfulSyncs / sesion.syncAttempts) * 100f;
        sesion.isPendingSync = !fueExitosa;
    }


    // Registra los intentos de sincronización (Para la métrica Offline-First)
    public void RegistrarSincronizacion (bool fueExitosa, float latenciaMs)
    {
        if (isSessionActive && currentSession != null)
        {
            RegistrarSincronizacionEnSesion(currentSession, fueExitosa, latenciaMs);
        }
    }

    //Captura el rendimiento de tu Director IA
    public void RegistrarRendimientoDDA (float currentPerformance, float expectedPerformance)
    {
        if (isSessionActive && currentSession != null)
        {
            currentSession.currentPerformance_Cp = currentPerformance;
            currentSession.expectedPerformance_Ep = expectedPerformance;
        }
    }

    // =====================================================================
    // VALIDACIÓN DE PREDICCIONES DE LA IA (MATRIZ DE CONFUSIÓN + MAE)
    // =====================================================================

    // Registra una predicción de la IA contrastada con lo que realmente ocurrió en la ventana de evaluación.
    // Positivo = "la IA predijo frustración" (intervención de ayuda) y verdad real = "el jugador perdió al menos una vida".
    public void RegistrarPrediccionIA(bool predijoFrustracion, bool ocurrioFrustracion)
    {
        if (!isSessionActive || currentSession == null) return;

        if (predijoFrustracion && ocurrioFrustracion)
        {
            currentSession.verdaderosPositivos++;
        }
        else if (predijoFrustracion && !ocurrioFrustracion)
        {
            currentSession.falsosPositivos++;
        }
        else if (!predijoFrustracion && ocurrioFrustracion)
        {
            currentSession.falsosNegativos++;
        }
        // Los verdaderos negativos no se almacenan: no intervienen en Precisión, Recall ni F1.
    }

    // Registra el error absoluto de una predicción de la IA (MAE = sumaErroresAbsolutos / totalPrediccionesIA).
    public void RegistrarErrorPrediccionIA(float errorAbsoluto)
    {
        if (!isSessionActive || currentSession == null) return;

        currentSession.sumaErroresAbsolutos += Mathf.Max(0f, errorAbsoluto);
        currentSession.totalPrediccionesIA++;
    }

    //Registrar caidas de rendimiento tecnico
    public void RegistrarCaidaFPS(float duracionCaida)
    {
        if (isSessionActive && currentSession != null)
        {
            currentSession.fpsDropTime += duracionCaida;
        }
    }

    //Captura un intento de generacion de trampa
    public void RegistrarIntentoRutaProcedural()
    {
        if(isSessionActive && currentSession != null)
        {
            currentSession.totalProceduralAttempts++;
        }
    }

    //Captura cuandlo la trama encuentra un lugar valido
    public void RegistrarRutaProceduralExitosa()
    {
        if (isSessionActive && currentSession != null)
        {
            currentSession.successfulProceduralSpawns++;
        }
    }

    // Captura Tiempo de Respuesta del Sistema (Tiempo de carga local en ms)
    public void RegistrarTiempoDeRespuesta(float tiempoCargaMs)
    {
        if (isSessionActive && currentSession != null) currentSession.localLoadTimeMs = tiempoCargaMs;
    }

    //Getter para acceder a la sesion actual (util para GameController)
    public SessionData GetCurrentSession() => currentSession;
    // Obtener el estado de la sesión
    public bool IsSessionActive => isSessionActive;


}
