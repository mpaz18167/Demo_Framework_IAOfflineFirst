using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

// Este script es un controlador de juego que se encarga de manejar la lógica principal del juego.
public class GameController : MonoBehaviour
{
    private SaveService saveService;
    private CloudService cloudService;
    private TelemetryService telemetryService;
    private PlayerData playerData;

    // Variable para manejar los controles del jugador
    public bool esModoContrarreloj = false;

    [Header("Sistema de Vidas")]
    public int vidasActuales = 3;

    [Header("Sistema de Checkpoints Fisicos")]
    //ALCANCIA DEL NIVEL: Unica variable afectada durante el gameplay y leida por la IA
    public int puntosNivelActual = 0;
    private int scoreGuardadoEnCheckpoint = 0; // Variable para almacenar el puntaje del jugador al tocar un checkpoint

    [Header("Contabilidad Dinámica (Para Normalización)")]
    public int puntosTotalesGenerados = 0; // El máximo dinámico

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
        if (vidasActuales <=0)
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

    }

    private async void PrepararPartida(string uid)
    {
        System.Diagnostics.Stopwatch cronometroRed = new System.Diagnostics.Stopwatch();

        // Verificar si hay conexión a internet antes de intentar sincronizar con la nube.
        if (Application.internetReachability != NetworkReachability.NotReachable) 
        {
            Debug.Log($"[GameController] Conexión a internet disponible. Intentando sincronizar con la nube...");
            try
            {
                //Medir Telemetria
                cronometroRed.Start(); //Iniciar medicion de latencia

                // Descargar los datos del jugador desde la nube.)
                PlayerData downloadedData = await cloudService.DownloadPlayerData(uid);

                cronometroRed.Stop(); //Detener medicion

                Debug.Log($"[GameController] EXITO. Datos descargados correctamente desde Firebase: {downloadedData.playerId}");
                if (downloadedData != null)
                {
                    // Si los datos descargados no son nulos, se asignan a playerData.
                    playerData = downloadedData;
                    // ¡NUEVO: Actualizamos la pantalla con las vidas guardadas en la nube!
                    vidasActuales = playerData.currentLives;

                }
            }
            catch (System.Exception ex)
            {
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

        //Registro de latencia en la telemetria 
        if (cronometroRed.ElapsedMilliseconds > 0)
        {
            telemetryService.RegistrarSincronizacion(true, cronometroRed.ElapsedMilliseconds);
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
                await cloudService.SyncToCloud(playerData); // Sincronizar los datos del jugador con la nube después de registrar la nueva cuenta.

                Debug.Log($"[GameController] Nueva cuenta registrada para el jugador: {alias} con correo: {correo}. Datos guardados localmente y sincronizados con la nube.");
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
            
            Debug.Log($"[GameController] Respawn ejecutado. Jugador movido a la posición del checkpoint. Puntos restaurados a: {scoreGuardadoEnCheckpoint}");
        }

        OnReconfiguracionEntorno?.Invoke(); // Invocar el evento para reconfigurar el entorno del nivel después del respawn.

        estaEnSecuenciaDeMuerte = false;
    }

    private void EjecutarGameOver()
    {
        Debug.Log($"[GameController] GAME OVER. No quedan vidas restantes.");

        if (playerData != null) playerData.currentLives = 3;


        // Finalizar la sesión de telemetría
        if (telemetryService != null)
        {
            SessionData sesionFinal = telemetryService.FinalizarSesionYCalcular();
            if (sesionFinal != null)
            {
                sesionFinal.endReason = "Death"; // La razón de finalización es muerte
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
                }
            }
        }

        // Aquí puedes agregar la lógica para mostrar la pantalla de Game Over, reiniciar el nivel, etc.
    }

    private async Task TrySyncGameOverDataAsync(SessionData sesionFinal)
    {
        try
        {
            await cloudService.SyncToCloud(playerData);
            await cloudService.SyncTelemetry(sesionFinal);
            Debug.Log("[GameController] Datos de Game Over sincronizados con la nube.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GameController] No se pudieron sincronizar datos de Game Over: {ex.Message}");
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

        //Verificar si se encontraon todos los coleccionables
        if (porcentajeFinal >= 100f)
        {
            record.completionCount++;
            record.allColectiblesFound = true;
            Debug.Log($"[GameController] ¡Nivel {nivelActual} completado al 100%! Récord de progreso guardado.");

        }
        else
        {
            record.completionCount++;
            Debug.Log($"[GameController] Nivel {nivelActual} terminado. Progreso alcanzado: {porcentajeFinal:F1}%");
        }


        //RECALCULAR SCORE GLOBAL: La suma de los puntajes maximos de todos los niveles jugados.
        playerData.score = playerData.levelRecords.Sum(r => r.bestScore);
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
        int siguienteNivel = playerData.level + 1;
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
            sesionFinal.levelId = playerData.highestLevelUnlocked; // Establecer el ID del nivel en los datos de la sesión final como el nivel más alto desbloqueado por el jugador.

            
            ActualizarRecordsDeNivel(sesionFinal); // Actualizar los registros del nivel en los datos del jugador según los datos de la sesión final.

            //OFFLINE-FIRST: Guardar los datos localmente primero.
            saveService.SaveDataLocal(playerData);
            saveService.SaveTelemetryLocal(sesionFinal);


            //NUBE: Intentar sincronizar los datos con la nube.
            if (Application.internetReachability != NetworkReachability.NotReachable)
            {
                Debug.Log($"[GameController] Conexión a internet disponible. Intentando sincronizar con la nube...");
                try
                {
                    // se usa await para esperar a que la tarea de sincronización se complete antes de continuar con la ejecución del código.
                    await cloudService.SyncToCloud(playerData);
                    await cloudService.SyncTelemetry(sesionFinal);

                    Debug.Log($"[GameController] EXITO. Datos subidos correctamente a Firebase.");

                }
                catch (System.Exception syncEx)
                {
                    Debug.LogError($"[GameController] ERROR al sincronizar con la nube: {syncEx.Message}");
                    playerData.isPendingSync = true; // marcar para sincronizacion posterior
                    saveService.SaveDataLocal(playerData);

                }
            }
            else
            {
                Debug.Log($"[GameController] No hay conexión a internet. Los datos se guardarán localmente.");
                playerData.isPendingSync = true; // Marcar para sincronización posterior
                saveService.SaveDataLocal(playerData);
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

    //PUENTE: Permite a la IA leer el puntaje real de forma segura
    public int ObtenerPuntajeActual() {return puntosNivelActual; }

    public int ObtenerPuntajeGlobal() { return playerData != null ? playerData.score : 0; }

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
        Debug.Log($"[GameController] El nivel ahora tiene un máximo de {puntosTotalesGenerados} puntos.");
    }




}
