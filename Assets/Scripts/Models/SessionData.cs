using System;

[Serializable]
public class SessionData
{
    //TELEMETRIA: Clase usada para almacenar los datos de la sesión y poder guardarlos en un archivo JSON
    public string sessionId; // Identificador único de la sesión
    public string playerId;
    public int levelId; // Identificador del nivel o escena del juego

    //Contexto de finalizacion
    public string endReason; // Razón por la cual la sesión terminó (por ejemplo, "Death", "Level Complete", "Quit")
    public bool isTimeTrial; // Indica si la sesión es un contrarreloj o no
    public string zoneType; // Tipo de zona o área del juego
    public int checkpointsReached; // Número de puntos de control alcanzados durante la sesión
    public float levelProgress; // Porcentaje de progreso en el nivel (0.0 a 1.0)


    //Variables de telemetria bruta (Entradas de las ecuaciones)
    public float dificultty_D;
    public float time_T;
    public int movement_M;
    public int points_P;
    public int maxPoints_P; // NUEVO: Total de Wumpas/Puntos disponibles en el nivel
    public int deaths; //Necesario para calcular el OGD


    public float opperabilty; //Indice de Operabilidad ISO/IEC 25010
    public float ogd;        //Difucultad Objetiva del Juego


 // =========================================================
 // NUEVAS VARIABLES PARA LA MATRIZ DE OPERACIONALIZACIÓN
 // =========================================================

    // --- Experiencia del Jugador (Operabilidad e Inmersión) ---
    public int startingLives;           // Vidas totales al inicio de la sesión
    public int remainingLives;          // Vidas restantes al final de la sesión
    public float lifeLossIndex;         // Índice de pérdida de vida por encuentro
    public float ease;                  // Cálculo de Facilidad Percibida (EASE)
    public float flowIndex;             // Índice de Flujo
    public float difficultyByTicks;     // Cálculo de Dificultad Objetiva por Ticks
    public float performanceDDA;        // Rendimiento del Ajuste Dinámico de Dificultad (Performance DDA)

    // --- Variables Crudas para el Cálculo de Dificultad (Ticks) ---
    public int actionsPerformed_N;      // N: Total de acciones realizadas (saltos, esquives)
    public float idealReferenceValue_Z; // Z: Valor de referencia ideal preestablecido para el reto
    public int telemetryTicks_D;        // D: Número de ticks o intervalos de tiempo transcurridos

    // --- Variables Crudas para el Rendimiento DDA ---
    public float currentPerformance_Cp;   // Cp: Desempeño actual del jugador
    public float expectedPerformance_Ep;  // Ep[t]: Desempeño esperado por el sistema

    // --- Rendimiento del motor (Estabilidad técnica) ---
    public float localLoadTimeMs;       // Tiempo de carga local (ms) -> Tiempo de Respuesta del Sistema
    public float fpsDropTime;           // Tiempo total con caídas de FPS (para calcular Uptime)
    public float uptimePercentage;      // Disponibilidad Operativa Continua (Uptime %)
    public float crashFreeTime;         // Tiempo sin fallos (para calcular Estabilidad)
    public float stabilityPercentage;   // Estabilidad del juego (%)

    // --- Arquitectura Offline-First (Persistencia de Datos) ---
    public int syncAttempts;            // Total de Intentos de reconexión
    public int successfulSyncs;         // Sincronizaciones exitosas
    public float syncSuccessRate;       // Tasa de Eficiencia de Sincronización Offline (%)
    public float syncLatencyMs;         // Latencia de Sincronización de Datos (Nube - Local)

    // --- Inteligencia Artificial Adaptativa (Orquestación Dinámica) ---
    // NOTA: Precision, Recall, F1, MAE y Tasa de rutas válidas se calcularán a nivel global o post-procesado,
    // pero podemos guardar el conteo de rutas por sesión si lo deseas.
    // --- Métricas Procedimentales (Adaptado a tus Cajas/Obstáculos) ---
    public int totalProceduralAttempts; // Intentos de generar cajas/trampas
    public int successfulProceduralSpawns; // Cajas/trampas generadas con éxito
    public float routeSuccessRate; // Tasa de Éxito (%)

    // --- Métricas de Predicción de IA (Matriz de Confusión y Error) ---
    public int verdaderosPositivos;     // VP
    public int falsosPositivos;         // FP
    public int falsosNegativos;         // FN
    public float sumaErroresAbsolutos;  // Sumatoria de |Y_real - Y_pred|
    public int totalPrediccionesIA;     // 'n' para el MAE

    public float precisionIA;           // PRECISION
    public float recallIA;              // SENSIBILIDAD (RECALL)
    public float f1ScoreIA;             // NUEVO: F1-SCORE
    public float maeIA;                 // ERROR ABSOLUTO MEDIO (MAE)



    // =========================================================
    // BANDERAS DE ESTADO
    // =========================================================
    public bool isPendingSync; // Indica si los datos de la sesión están pendientes de sincronización
    public bool isDDAActive; //false = pre-Test, True= post-Test


    public SessionData()
    {
        sessionId = Guid.NewGuid().ToString(); // Generar un identificador único para la sesión
        playerId = ""; // Inicializar el ID del jugador como una cadena vacía
        levelId = 1;
        endReason = "";
        isTimeTrial = false;
        zoneType = "";
        checkpointsReached = 0;
        levelProgress = 0f;

        dificultty_D = 1.0f; // Valor predeterminado de dificultad (Fácil)
        time_T = 0f; // Tiempo inicial de la sesión
        movement_M = 0; // Movimientos iniciales de la sesión
        points_P = 0; // Puntos iniciales de la sesión
        maxPoints_P = 100; // Valor predeterminado de puntos máximos 
        deaths = 0;

        opperabilty = 0f; // Valor predeterminado de operabilidad
        ogd = 0f;

        //Inicializar Nuevas variables para la evaluacion
        startingLives = 3;// Asuire 3 vidas por defecto, ajústalo según tu juego
        remainingLives = 3;
        lifeLossIndex = 0f;
        ease = 0f;
        flowIndex = 0f;
        difficultyByTicks = 0f;
        performanceDDA = 0f;
        
        actionsPerformed_N = 0;
        idealReferenceValue_Z = 0f;
        telemetryTicks_D = 0;

        currentPerformance_Cp = 0;
        expectedPerformance_Ep = 0;


        localLoadTimeMs = 0f;
        fpsDropTime= 0f;
        uptimePercentage = 100f;
        crashFreeTime = 0f;
        stabilityPercentage = 100f;

        syncAttempts = 0;
        successfulSyncs = 0;
        syncSuccessRate = 0f;
        syncLatencyMs = 0f;

        totalProceduralAttempts   = 0;
        successfulProceduralSpawns = 0;
        routeSuccessRate = 0f;

        verdaderosPositivos = 0;
        falsosPositivos = 0;
        falsosNegativos = 0;
        sumaErroresAbsolutos = 0f;
        totalPrediccionesIA = 0;

        precisionIA = 0f;
        recallIA = 0f;
        f1ScoreIA = 0f;
        maeIA = 0f;


        isPendingSync = true; // Los datos de la sesión están pendientes de sincronización al inicio
        isDDAActive = false;
    }


}
