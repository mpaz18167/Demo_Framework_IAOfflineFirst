using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SocialPlatforms;

// Este script es un controlador de juego que se encarga de manejar la lógica principal del juego.
public class GameController : MonoBehaviour
{
    private SaveService saveService;
    private CloudService cloudService;
    private TelemetryService telemetryService;
    private PlayerData playerData;

    public PlayerData DatosJugador => playerData; // Acceso de solo lectura para el menú de progreso

    // Variable para manejar los controles del jugador
    public bool esModoContrarreloj = false;

    private bool partidaPreparada = false; // NUEVO: evita doble inicialización de sesión/telemetría

    [Header("Sistema de Vidas")]
    public int vidasActuales = 3;

    [Header("Sistema de Checkpoints Fisicos")]
    //ALCANCIA DEL NIVEL: Unica variable afectada durante el gameplay y leida por la IA
    public int puntosNivelActual = 0;
    private int scoreGuardadoEnCheckpoint = 0; // Variable para almacenar el puntaje del jugador al tocar un checkpoint

    [Header("Contabilidad Dinámica (Para Normalización)")]
    public int puntosTotalesGenerados = 0; // El máximo dinámico

    [Header("Progreso dinámico (ajústalo al agregar niveles)")]
    public int nivelesTotales = 1; // NUEVO: cuántos niveles tiene el juego (1 mientras solo exista SampleScene)
    public int nivelActualId = 1;  // NUEVO: ID del nivel en juego (lo declara ConfigNivel en cada escena)


    [Header("Mecánica Vidas Extra (100 = 1 Vida)")]
    public int contadorPuntos = 0; // Se reinicia al llegar a 100

    private Vector3 posicionRespawn = Vector3.zero;// Variable para almacenar la posición de respawn del jugador
    private List<GameObject> objetosRecolectadosRecientemente = new List<GameObject>(); // Lista para almacenar los objetos recolectados recientemente>


    [Header("Eventos de DDA (IA Adaptativa)")]
    private float factorDificultadActual = 1.0f;
    public delegate void DificultadCambiadaDelegate(float factor);
    public static event DificultadCambiadaDelegate OnDificultadCambiada;

    [Header("Memoria del Director IA (Hide & Seek)")]
    [System.NonSerialized]
    public Dictionary<string, int> memoriaEscondites = new Dictionary<string, int>();


    [Header("Estado del Jugador")]
    public bool jugadorEstaASalvo = false; // Indica si el jugador está en una Zona Segura
    public bool jugadorEscondido = false; // Avisar que el jugador es invencible



    [Header("Control de Estados")]
    [Tooltip("Apagalo para el Pre-Test. Enciendelo para el Post-Test")]
    public bool activarDDA = true;
    public bool estaEnSecuenciaDeMuerte = false; // Candado para evitar -400 vidas

    //----Monitoreo de RED----
    public delegate void ConexionDelegate(bool hayConexion);
    public static event ConexionDelegate OnEstadoConexionCambiado; // Se usara en el HUD para mostrar el estado de la red
    public static event Action OnRecordActualizado; // Avisa al HUD que cambio el record historico del nivel


    private NetworkReachability estadoRedAnterior = NetworkReachability.NotReachable;
    private bool reintentoDeSyncEnCurso = false;
    void OnEnable()
    {

        // Suscribirse al evento de autenticación del usuario para preparar la partida cuando el usuario se autentique
        AuthManager.OnUserAuthenticated += PrepararPartida;
    }

    void OnDisable()
    {

        // Cancelar la suscripción al evento de autenticación del usuario
        AuthManager.OnUserAuthenticated -= PrepararPartida;
    }

    void Start()
    {
        // Obtener referencias a los servicios necesarios.
        saveService = GetComponent<SaveService>();
        cloudService = GetComponent<CloudService>();
        telemetryService = GetComponent<TelemetryService>();

        if (saveService == null || cloudService == null || telemetryService == null)
        {
            Debug.LogError("[GameController] Uno o más servicios no están asignados en el Inspector o no existen como componentes.");
            return;
        }

        // NUEVO: leer el ID del nivel desde la escena (si no hay ConfigNivel, queda 1)
        ConfigNivel cfgNivel = FindAnyObjectByType<ConfigNivel>();
        if (cfgNivel != null) nivelActualId = cfgNivel.nivelId;


        //Guardamos la pisicion inicial del jugador para el respawn en caso de muerte antes de tocar un checkpoint.
        // Inicializar la posición de respawn en el origen del mundo
        posicionRespawn = Vector3.zero;

        // Cargar los datos del jugador desde el almacenamiento local al iniciar el juego.
        playerData = saveService.LoadDataLocal();

        if (playerData == null)
        {
            Debug.LogError("[GameController] No se pudieron cargar datos del jugador. Creando datos nuevos.");
            playerData = new PlayerData();
        }

        // Sincronizar vidas locales con los datos persistentes
        vidasActuales = playerData.currentLives;
        //puntosNivelActual = playerData.score;

        //Candado de reinicio. Si recarga el juego muerto, le damos 3 vidas
        if (vidasActuales <= 0)
        {
            vidasActuales = 3;
            playerData.currentLives = 3;
        }

        Debug.Log($"[GameController] Datos locales del jugador cargados: {playerData.playerId}");
        Debug.Log($"[GameController] Puntaje Histórico Global: {playerData.score}. Vidas: {vidasActuales}");

        if (!string.IsNullOrEmpty(playerData.playerId))
        {
            telemetryService.IniciarSesion(playerData.currentDifficulty, playerData.playerId, esModoContrarreloj);
            Debug.Log($"[GameController] (Modo Local/Pruebas) Sesión de telemetría iniciada para: {playerData.playerId}");
        }


        //Inicializar el estado del monitor de red
        estadoRedAnterior = Application.internetReachability;
    }

    void Update()
    {
        //Listener de estado de red (costo por frame: una sola comparación, sin congelamientos)
        NetworkReachability estadoRedActual = Application.internetReachability;
        if (estadoRedActual != estadoRedAnterior)
        {
            bool hayConexion = estadoRedActual != NetworkReachability.NotReachable;
            OnEstadoConexionCambiado?.Invoke(hayConexion);//el HUD cambia el indicador de red

            if (hayConexion)
            {
                Debug.Log("[GameController] Conexión a internet restaurada.");
                if (playerData != null && playerData.isPendingSync && !reintentoDeSyncEnCurso)
                {
                    
                    Debug.Log("[GameController] Intentando sincronizar datos pendientes con la nube...");
                    _ = ReintentarSincronizacionPendienteSync();
                    
                }

            }
            else
            {
                Debug.Log("[GameController] Conexión a internet perdida.");
            }

            estadoRedAnterior = estadoRedActual;

        }
    }

    private async Task ReintentarSincronizacionPendienteSync()
    {
        if (reintentoDeSyncEnCurso) return; // Evitar múltiples reintentos simultáneos
        if (cloudService == null || saveService == null || playerData == null)
        {
            Debug.Log("[GameController] Error: Uno o más servicios o datos del jugador son nulos.");
            return;
        }

        if (!playerData.isPendingSync)
        {
            Debug.Log("[GameController] No hay datos pendientes para sincronizar.");
            return;
        }

        reintentoDeSyncEnCurso = true;

        try
        {
            bool playerOk = await cloudService.SyncToCloud(playerData);

            bool telemetryOk = true;

            SessionData telemetriaLocal = saveService.LoadTelemetryLocal();
            if (telemetriaLocal != null && telemetriaLocal.isPendingSync)
            {
                telemetryOk = await cloudService.SyncTelemetry(telemetriaLocal);
                if (telemetryOk)
                {
                    saveService.SaveTelemetryLocal(telemetriaLocal);
                }
            }

            if (playerOk && telemetryOk)
            {
                playerData.isPendingSync = false;
                saveService.SaveDataLocal(playerData);
                Debug.Log("[GameController] Sincronización pendiente completada con éxito.");
                // RANKING ONLINE: al reconectar, también reintentamos subir el récord (respaldo del respaldo)
                _ = SubirPuntajeSeguroAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GameController] Falló el reintento de sincronización: {ex.Message}");
        }
        finally
        {
            reintentoDeSyncEnCurso = false;
        }
    }

    private async void PrepararPartida(string uid)
    {

        if (partidaPreparada) { Debug.Log("[GameController] PrepararPartida ya ejecutada (login posterior no reinicia la sesión)."); return; } // NUEVO
        partidaPreparada = true; // NUEVO


        System.Diagnostics.Stopwatch cronometroRed = new System.Diagnostics.Stopwatch();

        // Estado del intento de sincronización (alimenta la Tasa de Eficiencia de Sincronización Offline y la Latencia)
        bool huboIntentoDeRed = false;
        bool sincronizacionExitosa = false;


        // Verificar si hay conexión a internet antes de intentar sincronizar con la nube.
        if (Application.internetReachability != NetworkReachability.NotReachable) 
        {
            Debug.Log($"[GameController] Conexión a internet disponible. Intentando sincronizar con la nube...");
            try
            {
                //Medir Telemetria
                huboIntentoDeRed = true;
                cronometroRed.Start(); //Iniciar medicion de latencia

                // Descargar los datos del jugador desde la nube.)
                PlayerData downloadedData = await cloudService.DownloadPlayerData(uid);

                cronometroRed.Stop(); //Detener medicion
                sincronizacionExitosa = true; // La nube respondió (aunque no exista documento, la red funcionó)

                if (downloadedData != null)
                {
                    // NUEVO: la nube NUNCA borra lo local
                    // 1) Si la nube viene sin records pero el local tiene, conservar los locales
                    if ((downloadedData.levelRecords == null || downloadedData.levelRecords.Count == 0)
                        && playerData != null && playerData.levelRecords != null && playerData.levelRecords.Count > 0)
                    {
                        downloadedData.levelRecords = playerData.levelRecords;
                        downloadedData.score = Mathf.Max(downloadedData.score, playerData.score);
                    }

                    // 2) Identidad: alias/correo elegidos en el menú no los pisa una nube "vacía"
                    if (!string.IsNullOrEmpty(playerData.playerName) && playerData.playerName != "Anonimo"
                        && (string.IsNullOrEmpty(downloadedData.playerName) || downloadedData.playerName == "Anonimo"))
                    {
                        downloadedData.playerName = playerData.playerName;
                    }
                    if (!string.IsNullOrEmpty(playerData.email) && string.IsNullOrEmpty(downloadedData.email))
                    {
                        downloadedData.email = playerData.email;
                    }

                    playerData = downloadedData;

                    // 3) Vidas: la nube nunca quita vidas ya obtenidas
                    vidasActuales = Mathf.Max(vidasActuales, playerData.currentLives);

                    Debug.Log($"[GameController] EXITO. Datos descargados desde Firebase: {downloadedData.playerId}. Vidas: {vidasActuales}, records: {playerData.levelRecords.Count}");

                }
                else
                {
                    Debug.Log($"[GameController] La nube aún no tiene datos para {uid}. Se conservan los datos locales.");
                }
            }
            catch (System.Exception ex)
            {
                cronometroRed.Stop();
                sincronizacionExitosa = false;
                Debug.Log($"[GameController] ERROR al sincronizar con la nube: {ex.Message}");
            }
        }
        else
        {
            Debug.Log($"[GameController] No hay conexión a internet. Los datos se usarán desde el almacenamiento local.");
        }


        // Asignar el ID del jugador autenticado a los datos del jugador.
        playerData.playerId = uid;
        // Guardar los datos del jugador localmente después de asignar el ID.
        saveService.SaveDataLocal(playerData); 
        Debug.Log($"[GameController] ID del jugador autenticado asignado: {playerData.playerId} y guardado localmente.");

        // Iniciar la sesión de telemetría con la dificultad inicial y el ID del jugador.
        telemetryService.IniciarSesion(playerData.currentDifficulty, playerData.playerId, esModoContrarreloj);
        Debug.Log($"[GameController] Sesión de telemetría iniciada para el jugador: {playerData.playerId} con dificultad: {playerData.currentDifficulty}");

        //Registro de resultado y latencia en la telemetría.
        //Sin conexión NO se cuenta como intento de sincronización: la tasa mide la eficiencia cuando hay red.
        if (huboIntentoDeRed)
        {
            telemetryService.RegistrarSincronizacion(sincronizacionExitosa, cronometroRed.ElapsedMilliseconds);
        }
    }

    // Método para registrar una nueva cuenta de jugador.
    public async Task RegistrarNuevaCuenta(string correo, string password, string alias)
    {
        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(alias))
        {
            Debug.LogError("[GameController] Parametros invalidos para Registrar Nueva cuenta Async.");
            return;
        }

        try
        {
            AuthManager authManager = FindAnyObjectByType<AuthManager>();
            if (authManager == null) 
            {
                Debug.LogError("[GameController] No se encontró AuthManager en la escena");
                return;
            }

            await authManager.LinkAccount(correo, password, alias); // Llamar al método LinkAccount del AuthManager para registrar la nueva cuenta.

            playerData.playerName = alias; // Asignar el alias del jugador a los datos del jugador.
            playerData.email = correo; // Asignar el correo del jugador a los datos del jugador.
            saveService.SaveDataLocal(playerData); // Guardar los datos del jugador localmente después de registrar la nueva cuenta.
            
            if (Application.internetReachability != NetworkReachability.NotReachable)
            {
                bool syncExitosa = await cloudService.SyncToCloud(playerData); // Sincronizar los datos del jugador con la nube después de registrar la nueva cuenta.

                if (syncExitosa)
                {
                    Debug.Log($"[GameController] Nueva cuenta registrada para el jugador: {alias} con correo: {correo}. Datos guardados localmente y sincronizados con la nube.");
                }
                else
                {
                    playerData.isPendingSync = true;
                    saveService.SaveDataLocal(playerData);
                    Debug.LogWarning($"[GameController] Nueva cuenta registrada para {alias}, pero la sincronización con la nube falló (quedará pendiente).");
                }
            }

            else
            {
                Debug.Log($"[GameController] Nueva cuenta registrada para el jugador: {alias}. Datos guardados localmente (sincronización pendiente).");
                playerData.isPendingSync = true;
                saveService.SaveDataLocal(playerData);
            }

        }

        catch (Exception ex)
        {
            Debug.LogError($"[GameController] Error al registrar nueva cuenta: {ex.Message}");
        }
        
    }

    public void CambiarAlias(string nuevoAlias)
    {
        playerData.playerName = nuevoAlias;
        saveService.SaveDataLocal(playerData); // Guardar el nuevo alias localmente.
        Debug.Log($"[GameController] Alias del jugador cambiado a: {nuevoAlias} y guardado localmente.");
    }

    public void SumarPuntos(int cantidadPuntos)
    {
        if (playerData == null)
        {
            Debug.LogError("[GameController] playerData es null. No se pueden sumar puntos.");
            return;
        }

        puntosNivelActual += cantidadPuntos; // Incrementar la puntuación del jugador en 50.
        telemetryService.RegistrarPuntaje(cantidadPuntos); // Registrar el puntaje en la sesión de telemetría.

        // 2. JUGABILIDAD (Crash): Acumulación hasta 100
        contadorPuntos += cantidadPuntos;
        if(contadorPuntos >= 100)
        {
            contadorPuntos -= 100; // Reiniciar el contador de puntos después de otorgar una vida extra.
            SumarVida(1); // Otorgar una vida extra al jugador.
        }

        Debug.Log($"[GameController] +{cantidadPuntos} Puntos asignados.");
        Debug.Log($"[GameController] Puntuación actual del jugador: {puntosNivelActual}");
        
    }

    public void SumarVida(int cantidad = 1)
    {
        vidasActuales += cantidad;
        if (playerData != null) playerData.currentLives = vidasActuales;
        // ¡NUEVO: Avisar a la telemetría que ganamos una vida!
        if (telemetryService != null) telemetryService.ActualizarVidasRestantes(vidasActuales);
        Debug.Log($"[GameController] +1 Vida Extra. Vidas actuales: {vidasActuales}"); 
    }

    //Funcion para el modo Contrarreloj
    public void RestarTiempoContrarreloj(float segundos)
    {
        if (esModoContrarreloj)
        {
            telemetryService.RestarTiempoCaja(segundos);
            Debug.Log($"[GameController] Tiempo restado: -{segundos}");
        }
    }

    // Registrar objetos para el checkpoitn Hibrido
    public void RegistrarObjetoRecolectado(GameObject objeto)
    {
        
        if (!objetosRecolectadosRecientemente.Contains(objeto))
        {
            objetosRecolectadosRecientemente.Add(objeto); // Agregar el objeto a la lista de objetos recolectados recientemente.
            Debug.Log($"[GameController] Objeto recolectado registrado: {objeto.name}");
            objeto.SetActive(false); // Lo apagamos en lugar de destruirlo
        }
        else
        {
            Debug.Log($"[GameController] Objeto {objeto.name} ya estaba registrado como recolectado recientemente.");
        }
    }

    public void TocarCheckpoint(int numChekpoint, float nuevoPorcentaje, Vector3 posicionDelCheckpoint)
    {
        telemetryService.ActualizarProgreso(numChekpoint, nuevoPorcentaje); // Actualizar el progreso del jugador en la sesión de telemetría.


        // Guardar la posición del checkpoint para el respawn del jugador.
        posicionRespawn = posicionDelCheckpoint;
        scoreGuardadoEnCheckpoint = puntosNivelActual; // Guardar el puntaje actual del jugador al tocar el checkpoint.
       
        //Como cruzo el checkpoint, destruimos las cajas anteriores para que ya no vuelvan a aparecer.
        foreach (GameObject obj in objetosRecolectadosRecientemente)
        {
            if (obj != null) 
            {
                Destroy(obj);

            }
        }

        objetosRecolectadosRecientemente.Clear(); //Limpiamos la lista para el siguiente tramo.

        Debug.Log($"[GameController] Checkpoint {numChekpoint} alcanzado. Progreso del nivel: {nuevoPorcentaje * 100}%. Puntos: {scoreGuardadoEnCheckpoint}");
    }
    //--- SECUENCIA DE MUERTE ---
    public void IniciarMuerte()
    {
        // Si ya está ejecutándose una muerte, ignoramos llamadas repetidas de los frames
        if (estaEnSecuenciaDeMuerte) return;
        StartCoroutine(SecuenciaDeMuerte());
    }

    private IEnumerator SecuenciaDeMuerte()
    { 
        estaEnSecuenciaDeMuerte = true;

        PlayerMovement player = FindAnyObjectByType<PlayerMovement>();

        if (player != null)
        {
            player.PermitirControles(false);
            if (FadeUI.Instancia != null) FadeUI.Instancia.FundidoAHaciaNegro(); // NUEVO: negro cubre los 2.5 s
            Debug.Log($"[GameController] Jugador muerto. Controles del jugador deshabilitados.");

        }

        //Avisar a la telemetria que el jugador Murio
        telemetryService.RegistrarMuerte();

        yield return new WaitForSeconds(2.5f); // Esperar 2.5 segundos antes de continuar con la secuencia de muerte.

        vidasActuales--; // Reducir el número de vidas actuales del jugador en 1.

        // NUEVO: Evitar números negativos
        if (vidasActuales < 0) vidasActuales = 0;

        if (playerData != null )
        {
            playerData.currentLives = vidasActuales; //Sincronizar vidas con PlayerData
        }
        Debug.Log($"[GameController] Vidas restantes: {vidasActuales}");

        //Actualizar vidas para el indice de Perdida de vida
        if (telemetryService != null) telemetryService.ActualizarVidasRestantes(vidasActuales);

        if (vidasActuales > 0)
        {
            EjecutarRespawn(player); // Si el jugador aún tiene vidas, ejecutar el respawn.
            if (FadeUI.Instancia != null) FadeUI.Instancia.FundidoHaciaClaro(); // NUEVO: reaparece y vuelve el mundo
        }
        else
        {
            EjecutarGameOver(); // Si el jugador no tiene vidas restantes, ejecutar el Game Over.


        }
    }

    //Evento para forzar la mutacion de cajas/enemigos durante el respawn post-muerte
    public delegate void ReconfiguracionEntornoDelegate();
    public static event ReconfiguracionEntornoDelegate OnReconfiguracionEntorno;

    private void EjecutarRespawn(PlayerMovement player)
    {
        puntosNivelActual = scoreGuardadoEnCheckpoint; // Restaurar el puntaje del jugador al momento de tocar el checkpoint. Pierde los puntos obtenidos después de tocar el checkpoint.

        //El jugador murio, asi que revivimos las cajas que rompio en este tramo fallido.
        foreach (GameObject obj in objetosRecolectadosRecientemente)
        {
            if (obj !=null)
            {
                obj.SetActive(true);
            }
        }
        objetosRecolectadosRecientemente.Clear(); //Limpiamos la memoria para su nuevo intento


        if (player != null) // Verificar si el jugador no es nulo antes de intentar teletransportarlo.
        {
            player.Teletransportar(posicionRespawn); // Mover al jugador a la posición del último checkpoint alcanzado.
            player.PermitirControles(true); // Habilitar los controles del jugador después del respawn.
            jugadorEscondido = false; // FIX: si moriste escondido, la IA quedaba ciega para siempre tras respawn
            Debug.Log($"[GameController] Respawn ejecutado. Jugador movido a la posición del checkpoint. Puntos restaurados a: {scoreGuardadoEnCheckpoint}");
        }

        OnReconfiguracionEntorno?.Invoke(); // Invocar el evento para reconfigurar el entorno del nivel después del respawn.

        estaEnSecuenciaDeMuerte = false;
    }

    private void EjecutarGameOver()
    {
        Debug.Log($"[GameController] GAME OVER. No quedan vidas restantes.");

        if (playerData != null) playerData.currentLives = 3;

        if (FadeUI.Instancia != null) FadeUI.Instancia.FundidoAHaciaNegro(); // NUEVO: se queda en negro (fin de sesión)

        // Finalizar la sesión de telemetría
        if (telemetryService != null)
        {
            SessionData sesionFinal = telemetryService.FinalizarSesionYCalcular();
            if (sesionFinal != null)
            {
                sesionFinal.endReason = "Death"; // La razón de finalización es muerte
                sesionFinal.levelId = nivelActualId; // NUEO: etiquetar el record con el nivel realmente jugado (antes quedaba en 1)
                ActualizarRecordsDeNivel(sesionFinal);

                // Guardar datos localmente
                if (saveService != null)
                {
                    //playerData.score = Math.Max(playerData.score, sesionFinal.points_P);
                    saveService.SaveDataLocal(playerData);
                    saveService.SaveTelemetryLocal(sesionFinal);
                }

                // Intentar sincronizar con la nube si hay conexión
                if (Application.internetReachability != NetworkReachability.NotReachable && cloudService != null)
                {
                    _ = TrySyncGameOverDataAsync(sesionFinal);
                    _ = SubirPuntajeSeguroAsync(); // RANKING ONLINE: sube el récord también en el camino de muerte (fire-and-forget, jamás interrumpe)
                }
            }
        }

        // Aquí puedes agregar la lógica para mostrar la pantalla de Game Over, reiniciar el nivel, etc.
    }

    private async Task TrySyncGameOverDataAsync(SessionData sesionFinal)
    {
        System.Diagnostics.Stopwatch cronometro = new System.Diagnostics.Stopwatch();
        bool playerSyncExitosa = false;

        // 1) Subir los datos del jugador (midiendo resultado y latencia reales del intento)
        try
        {
            cronometro.Start();
            playerSyncExitosa = await cloudService.SyncToCloud(playerData);
            cronometro.Stop();
        }
        catch (Exception ex)
        {
            cronometro.Stop();
            Debug.LogWarning($"[GameController] No se pudieron sincronizar datos de Game Over: {ex.Message}");
        }

        // Registrar el intento/latencia en la sesión (Tasa de Eficiencia de Sincronización Offline)
        if (telemetryService != null)
        {
            telemetryService.RegistrarSincronizacionEnSesion(sesionFinal, playerSyncExitosa, cronometro.ElapsedMilliseconds);
        }

        if (!playerSyncExitosa && playerData != null)
        {
            playerData.isPendingSync = true;
            saveService?.SaveDataLocal(playerData);
        }

        // Persistir localmente el resultado del intento (Offline-First) antes de subir la telemetría
        saveService?.SaveTelemetryLocal(sesionFinal);

        // 2) Subir la telemetría (incluye las métricas de sincronización recién registradas)
        try
        {
            bool telemetrySyncExitosa = await cloudService.SyncTelemetry(sesionFinal);
            if (telemetrySyncExitosa)
            {
                Debug.Log("[GameController] Datos de Game Over sincronizados con la nube.");
            }
            else
            {
                sesionFinal.isPendingSync = true;
            }
        }
        catch (Exception ex)
        {
            sesionFinal.isPendingSync = true;
            Debug.LogWarning($"[GameController] No se pudo sincronizar la telemetría de Game Over: {ex.Message}");
        }
    }

    private void ActualizarRecordsDeNivel(SessionData sesionTerminada)
    {

        if (sesionTerminada == null)
        {
            Debug.LogError("[GameController] No se puede actualizar registros: sesionTerminada es null.");
            return;
        }

        if (playerData == null || playerData.levelRecords == null)
        {
            Debug.LogError("[GameController] PlayerData o levelRecords es null.");
            return;
        }

        int nivelActual = sesionTerminada.levelId;// Obtener el ID del nivel actual desde la sesión terminada.
        LevelRecord record = playerData.levelRecords.FirstOrDefault(r => r.levelId == nivelActual);// Buscar el registro del nivel actual en los datos del jugador.

        if (record == null)
        {
            record = new LevelRecord { 
                levelId = nivelActual, 
                bestTime = float.MaxValue, 
                bestScore= 0, 
                timesPlayed=0, 
                completionCount=0
            }; // Si no existe un registro para el nivel actual, crear uno nuevo con un tiempo inicial alto.

            playerData.levelRecords.Add(record); // Agregar el nuevo registro a la lista de registros del jugador.
        }

        //NUEVO: Sumamos un intento a las estasdisticas de este nivel(Para la telemetria)
        record.timesPlayed++;

        if (sesionTerminada.isTimeTrial) //Si es contrareloj.
        {
            if (sesionTerminada.time_T < record.bestTime)
            {
                record.bestTime = sesionTerminada.time_T; // Actualizar el mejor tiempo si el tiempo de la sesión terminada es menor.
                Debug.Log($"[GameController] Nuevo mejor tiempo para el nivel {nivelActual}: {record.bestTime} segundos.");
            }
        }
        else
        {
            if (puntosNivelActual > record.bestScore)
            {
                record.bestScore = puntosNivelActual; // Actualizar el puntaje más alto si el puntaje de la sesión terminada es mayor.
                Debug.Log($"[GameController] Nuevo puntaje más alto para el nivel {nivelActual}: {record.bestScore} puntos.");
            }
        }

        
        //NUEVO: Guardar el maximo progreso alcanzado en la sesion de este nivel
        if(sesionTerminada.levelProgress > record.maxProgressPercentage)
        {
            record.maxProgressPercentage = sesionTerminada.levelProgress;
        }

        //Mostrar el porcentaje exacto con el que termina
        float porcentajeFinal = sesionTerminada.levelProgress * 100f;

        // Verificar si se llegó al 100% del nivel (meta)
        if (porcentajeFinal >= 100f)
        {
            record.completionCount++; // NUEVO: solo cuenta como completado al llegar a la meta
            record.allColectiblesFound = true;
            Debug.Log($"[GameController] ¡Nivel {nivelActual} completado al 100%! Récord de progreso guardado.");
        }
        else
        {
            // NUEVO: aquí ANTES también hacía completionCount++ (quedaba igual que timesPlayed)
            Debug.Log($"[GameController] Nivel {nivelActual} terminado. Progreso alcanzado: {porcentajeFinal:F1}%");
        }

        // NUEVO: % de colección = puntos actuales / puntos totales del nivel (se guarda el máximo)
        float ratioColeccion = puntosTotalesGenerados > 0
            ? Mathf.Clamp01((float)puntosNivelActual / puntosTotalesGenerados) : 0f;
        if (ratioColeccion > record.coleccionRatio) record.coleccionRatio = ratioColeccion;

        //RECALCULAR SCORE GLOBAL: La suma de los puntajes maximos de todos los niveles jugados.
        playerData.score = playerData.levelRecords.Sum(r => r.bestScore);
        playerData.totalProgressPercentage = CalcularProgresoTotal(); // NUEVO: el campo ya existía, solo se alimenta (se sube a la nube solo)
        OnRecordActualizado?.Invoke(); // Notificar a los suscriptores que el record ha sido actualizado
    }


    //Funcion para cuando el jugador toca la meta del nivel
    public void LlegarALaMeta(float porcentajeFinal)
    {
        Debug.Log("[GameController] Nivel Completado. El jugador ha tocado la meta del nivel!");

        //Deshabilitar controles para celebrar
        PlayerMovement player = FindAnyObjectByType<PlayerMovement>();

        if (player != null) player.PermitirControles(false);

        // ¡NUEVO: Seguro final para que el JSON se vaya con las vidas correctas!
        if (telemetryService != null) telemetryService.ActualizarVidasRestantes(vidasActuales);
        //Actualizar la telemetria con el % final de cajas
        telemetryService.ActualizarProgreso(99, porcentajeFinal); //99 indica el porcentaje final de la meta

        //Desbloquear el siguiente nivel en el historial global
        int siguienteNivel = nivelActualId + 1;                        // NUEVO: desde el nivel JUGADO (antes playerData.level, que nunca cambiaba)
        playerData.level = Mathf.Max(playerData.level, siguienteNivel); // NUEVO: antes nunca se actualizaba → desbloqueo trabado en 2
        if (siguienteNivel > playerData.highestLevelUnlocked)
        {
            playerData.highestLevelUnlocked = siguienteNivel;
            Debug.Log($"[GameController] ¡Nuevo nivel desbloqueado: Nivel {siguienteNivel}!");
        }

        EjecutarGuardadoYSincronizacion("Level Complete");
    }

    private async Task EjecutarGuardadoYSincronizacionAsync(string motivoFin = "ManualSave")
    {
        try
        {
            // Verificar que los servicios estén inicializados
            if (telemetryService == null || saveService == null)
            {
                Debug.LogError("[GameController] Servicios no inicializados. No se puede ejecutar guardado y sincronización.");
                return;
            }
            SessionData sesionFinal = telemetryService.FinalizarSesionYCalcular(); // Finalizar la sesión de telemetría y obtener los datos finales de la sesión.

            if (sesionFinal == null)
            {
                Debug.LogError("[GameController] No hay sesión activa para finalizar.");
                return;
            }

            sesionFinal.endReason = motivoFin; // Establecer la razón de finalización de la sesión como "ManualSave".
            sesionFinal.levelId = nivelActualId; // Establecer el ID del nivel en los datos de la sesión final como el nivel más alto desbloqueado por el jugador.

            
            ActualizarRecordsDeNivel(sesionFinal); // Actualizar los registros del nivel en los datos del jugador según los datos de la sesión final.

            //OFFLINE-FIRST: Guardar los datos localmente primero.
            saveService.SaveDataLocal(playerData);


            //NUBE: Intentar sincronizar los datos con la nube.
            if (Application.internetReachability != NetworkReachability.NotReachable)
            {
                Debug.Log($"[GameController] Conexión a internet disponible. Intentando sincronizar con la nube...");
                System.Diagnostics.Stopwatch cronometroSync = new System.Diagnostics.Stopwatch();
                bool playerSyncExitosa = false;
                try
                {
                    cronometroSync.Start();
                    playerSyncExitosa = await cloudService.SyncToCloud(playerData);
                    cronometroSync.Stop();
                }
                catch (System.Exception syncEx)
                {
                    cronometroSync.Stop();
                    Debug.LogError($"[GameController] ERROR al sincronizar con la nube: {syncEx.Message}");
                }

                // Registrar intento y latencia en la sesión final (Tasa de Eficiencia de Sincronización Offline + Latencia)
                telemetryService.RegistrarSincronizacionEnSesion(sesionFinal, playerSyncExitosa, cronometroSync.ElapsedMilliseconds);

                if (!playerSyncExitosa)
                {
                    playerData.isPendingSync = true; // marcar para sincronizacion posterior
                    saveService.SaveDataLocal(playerData);
                }

                // Guardar localmente la telemetría con el resultado del intento recién registrado (Offline-First)
                saveService.SaveTelemetryLocal(sesionFinal);

                try
                {
                    bool telemetrySyncExitosa = await cloudService.SyncTelemetry(sesionFinal);
                    if (telemetrySyncExitosa)
                    {
                        Debug.Log($"[GameController] EXITO. Datos subidos correctamente a Firebase.");
                    }
                    else
                    {
                        sesionFinal.isPendingSync = true;
                        saveService.SaveTelemetryLocal(sesionFinal);
                    }
                }
                catch (System.Exception syncEx)
                {
                    sesionFinal.isPendingSync = true;
                    saveService.SaveTelemetryLocal(sesionFinal);
                    Debug.LogError($"[GameController] ERROR al sincronizar la telemetría con la nube: {syncEx.Message}");
                }


                // RANKING ONLINE: solo se intenta con red; si falla, no afecta nada (ya estamos en el bloque online)
                _ = SubirPuntajeSeguroAsync();

            }
            else
            {
                Debug.Log($"[GameController] No hay conexión a internet. Los datos se guardarán localmente.");
                playerData.isPendingSync = true; // Marcar para sincronización posterior

                saveService.SaveDataLocal(playerData);
                // Sin red NO se cuenta como intento de sincronización: la tasa mide la eficiencia cuando hay conectividad.

                saveService.SaveTelemetryLocal(sesionFinal); // Guardar localmente la telemetría con el resultado del intento recién registrado (Offline-First)
            }
        }
        catch (Exception ex)

        {
            Debug.LogError($"[GameController] Error inesperado en EjecutarGuardadoYSincronizacion: {ex.Message}");
        }

    }

    //Wrapper para llamar desde evento (que espera void)
    private void EjecutarGuardadoYSincronizacion(String motivoFin)
    {
        _ = EjecutarGuardadoYSincronizacionAsync(motivoFin);
    }

    // NUEVO: progreso total = media del % de cada nivel (0..100).
    // Con 1 nivel = % de ese nivel; al agregar niveles, sube nivelesTotales y el denominador se actualiza solo.
    public float CalcularProgresoTotal()
    {
        if (playerData == null) return 0f;
        int N = Mathf.Max(1, nivelesTotales);
        float suma = 0f;
        for (int i = 1; i <= N; i++)
        {
            LevelRecord r = playerData.levelRecords.FirstOrDefault(x => x.levelId == i);
            if (r == null) continue; // nivel sin jugar = 0
            float avance = Mathf.Clamp01(r.maxProgressPercentage);      // avance por checkpoints (meta = 1)
            float coleccion = Mathf.Clamp01(r.coleccionRatio);          // puntos recolectados
            suma += avance * 0.7f + coleccion * 0.3f;                   // 70% avance + 30% colección (ajustable)
        }
        return (suma / N) * 100f;
    }


    //PUENTE: Permite a la IA leer el puntaje real de forma segura
    public int ObtenerPuntajeActual() {return puntosNivelActual; }

    public int ObtenerPuntajeGlobal() { return playerData != null ? playerData.score : 0; }

    // Ranking online: intenta subir el récord; jamás lanza ni interrumpe el guardado
    private async Task SubirPuntajeSeguroAsync()
    {
        try
        {
            if (cloudService != null) await cloudService.SubirPuntajeALeaderboard(playerData);
        }
        catch (Exception e) { Debug.LogWarning($"[GameController] Ranking omitido: {e.Message}"); }
    }


    //Sistema DDA (IA)
    public void AjustarDificultadIA(float factor)
    {
        factorDificultadActual = factor;
        playerData.currentDifficulty = factor;
        OnDificultadCambiada?.Invoke(factor);
    }

    public float ObtenerFactorDificultad() {return factorDificultadActual; }

    //=========== Parte relacionada a la IA ===========
    //Metodo para registrar el habito del jugador
    public void RegistrarUsoDeEscondite(string etiqueta)
    {
        if (memoriaEscondites.ContainsKey(etiqueta))
        {
            memoriaEscondites[etiqueta]++;
        }
        else
        {
            memoriaEscondites.Add(etiqueta, 1);
        }
        Debug.Log($"[Memoria IA] Historial actualizado: {etiqueta} usado {memoriaEscondites[etiqueta]} veces");
    }

    //Puente para que el Arbol de comportamiento consulte los escondites mas usados
    public List<string> ObtenerEsconditesPeligrosos(int umbralDeAbuso = 3)
    {
        List<string> esconditesPeligrosos = new List<string>();
        foreach (var escondite in memoriaEscondites)
        {
            if (escondite.Value >= umbralDeAbuso)
            {
                esconditesPeligrosos.Add(escondite.Key);
            }
        }
        return esconditesPeligrosos;
    }

    //Puente: Para que las zonas Seguras le avisen al sistema y a la IA
    public void EstablecerEstadoSeguro(bool estado)
    {
        jugadorEstaASalvo = estado;
        if (estado)
        {
            Debug.Log("[GameController] Jugador entró en Zona Segura. IA notificada.");
        }
        else
        {
            Debug.Log("[GameController] Jugador salió de la Zona Segura. Peligro reactivado.");
        }
    }

    // Esta función será llamada por cada caja o fruta cuando nazca en el mundo
    public void RegistrarPuntosDisponibles(int cantidad)
    {
        puntosTotalesGenerados += cantidad;
        //Silenciado: se dispora una vez por caja y ensucia la consola/performance durante las sesiones de prueba
        //Debug.Log($"[GameController] El nivel ahora tiene un máximo de {puntosTotalesGenerados} puntos.");
    }

    public bool EstablecerAlias(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias)) return false;
        alias = alias.Trim();

        if (alias.Length < 3 || alias.Length > 15 || alias.Contains("@"))
        {
            Debug.LogWarning("[GameController] Alias inválido: 3 a 15 caracteres y sin «@»."); return false;
        }

        playerData.playerName = alias;   // El alias ES playerName: sin campos nuevos
        playerData.isPendingSync = true; // Si no hay red, se subirá al reconectar
        saveService.SaveDataLocal(playerData); // Escritura inmediata: funciona 100% offline

        AuthManager authManager = FindAnyObjectByType<AuthManager>();
        if (authManager != null) _ = authManager.SetUserAlias(alias); // best-effort: DisplayName en Firebase

        Debug.Log($"[GameController] Alias establecido: {alias}");
        return true;
    }

    // Descarga el progreso de la cuenta con la sesión iniciada y lo aplica localmente.
    // NO toca telemetría ni vidas: es una operación de "jugador", no de "sesión".
    public async Task<bool> RecuperarProgresoDeLaNube()
    {
        if (cloudService == null || saveService == null || playerData == null) return false;

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.LogWarning("[GameController] Sin conexión: no se puede recuperar progreso de la nube.");
            return false;
        }

        AuthManager authManager = FindAnyObjectByType<AuthManager>();
        string uid = (authManager != null && !string.IsNullOrEmpty(authManager.UserIdActual))
            ? authManager.UserIdActual          // UID de la cuenta con la que se hizo login
            : playerData.playerId;              // fallback: identidad local

        PlayerData nube = await cloudService.DownloadPlayerData(uid);
        if (nube == null)
        {
            Debug.LogWarning("[GameController] La nube no tiene datos para esta cuenta.");
            return false;
        }

        playerData = nube;                                  // La nube manda: era el objetivo del jugador
        vidasActuales = Mathf.Max(vidasActuales, nube.currentLives); // nunca quita vidas de la sesión en curso
        saveService.SaveDataLocal(playerData);              // a partir de ahora, la PC local tiene ese progreso

        Debug.Log($"[GameController] Progreso recuperado: nivel {nube.highestLevelUnlocked}, récord global {nube.score}, alias {nube.playerName}.");
        return true;
    }

    // NUEVO: descarga el progreso de la nube SIN aplicarlo — no toca nada local.
    // Misma lógica de uid que RecuperarProgresoDeLaNube (consistencia garantizada).
    public async Task<PlayerData> DescargarNubeSoloLectura()
    {
        if (cloudService == null || playerData == null) return null;
        if (Application.internetReachability == NetworkReachability.NotReachable) return null;

        AuthManager authManager = FindAnyObjectByType<AuthManager>();
        string uid = (authManager != null && !string.IsNullOrEmpty(authManager.UserIdActual))
            ? authManager.UserIdActual
            : playerData.playerId;

        return await cloudService.DownloadPlayerData(uid);  // GET de Firestore: NO escribe, NO aplica
    }




    // Botón "Subir mi progreso" del menú: fuerza una sincronización reutilizando el método validado
    public void SincronizarAhora()
    {
        if (playerData == null) return;
        playerData.isPendingSync = true;
        _ = ReintentarSincronizacionPendienteSync(); // tiene sus propios candados internos
    }

}
