using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static LevelRecord;

public class CloudService : MonoBehaviour
{

    private FirebaseFirestore db;

    void OnEnable()
    {
        FirebaseInit.OnFirebaseInitialized += InicializarBaseDeDatos;// Suscribirse al evento de inicialización de Firebase

    }

    void OnDisable()
    {
        FirebaseInit.OnFirebaseInitialized -= InicializarBaseDeDatos; // Desuscribirse del evento de inicialización de Firebase
    }

    private void InicializarBaseDeDatos()
    {
        db = FirebaseFirestore.DefaultInstance;
        Debug.Log("[CloudService] Firebase Firestore inicializado correctamente.");

    }

    public async Task<bool> SyncToCloud(PlayerData data)
    {
        // si db es null, no se puede sincronizar
        if (db == null)
        {
            Debug.Log("[CloudService] Intentado subir Datos, pero Firebase no está inicializado.");
            return false;
        }

        // Referencia al documento del jugador en Firestore
        DocumentReference docRef = db.Collection("players").Document(data.playerId); 

        // Diccionario para almacenar los datos del jugador
        Dictionary<string, object> cloudData = new Dictionary<string, object> 
        {
            {"playerId", data.playerId },
            {"playerName", data.playerName },
            {"email", data.email },
            {"level", data.level },
            {"score", data.score },

            {"currentDifficulty", data.currentDifficulty },
            {"totalProgressPercentage", data.totalProgressPercentage },
            {"currentLives", data.currentLives },
            {"highestLevelUnlocked", data.highestLevelUnlocked },
            {"lastModified", data.lastModified },
            {"isPendingSync", false },
            {"levelRecords", JsonUtility.ToJson(new ListaRecords { records = data.levelRecords }) } // NUEVO: antes los records nunca se subían
        };
        try
        {
            await docRef.SetAsync(cloudData); // Guardar los datos en Firestore

            data.isPendingSync = false; // Marcar los datos como sincronizados
            Debug.Log($"[CloudService] Datos del jugador {data.playerId} sincronizados correctamente con la nube.");
            return true;

        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CloudService] Error al sincronizar datos del jugador {data.playerId} con la nube: {e.Message}");
            return false;
        }
    }

    //NUEVO: Funcion para buscar el correo usando el nombre de usuario 
    public async Task<string> BuscarCorreoPorAlias (string alias)
    {
        if (db == null)
        {
            return null; // Firebase no está inicializado
        }

        try
        {
            Query query = db.Collection("players").WhereEqualTo("playerName", alias);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Any()) // Any() devuelve true si hay al menos un documento en la colección
            {
                return snapshot.Documents.First().GetValue<string>("email"); // Retorna el correo del primer documento encontrado
            }
            return null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CloudService] Error al buscar correo por alias {alias}: {e.Message}");
            return null; // En caso de error, devolver null
        }
    }

    public async Task<bool> SyncTelemetry(SessionData session)
    {
        if (db == null)
        {
            Debug.Log("[CloudService] Intentado subir Datos de Telemetría, pero Firebase no está inicializado.");
            return false;
        }

        if (string.IsNullOrEmpty(session.sessionId))
        {
            Debug.LogError("[CloudService] No se encontro sessionId. Generando uno nuevo para la sesión.");
            session.sessionId= Guid.NewGuid().ToString(); // Generar un nuevo sessionId si no existe
        }

        // Referencia al documento de la sesión en Firestore
        DocumentReference docRef = db.Collection("telemetry").Document(session.sessionId); 

        // Diccionario para almacenar los datos de la sesión
        Dictionary<string, object> cloudData = new Dictionary<string, object> 
        {

            // Contexto de Partida
            {"sessionId", session.sessionId },
            {"playerId", session.playerId },
            {"levelId", session.levelId },
            {"endReason", session.endReason },
            {"isTimeTrial", session.isTimeTrial }, //modo contrareloj
            {"zoneType", session.zoneType },
            {"checkpointsReached", session.checkpointsReached },
            {"levelProgress", session.levelProgress },

            // Variables de telemetría bruta y OGD
            {"dificultty_D", session.dificultty_D },
            {"time_T", session.time_T },
            {"movement_M", session.movement_M },
            {"points_P", session.points_P },
            {"deaths", session.deaths },
            {"opperabilty", session.opperabilty },
            {"ogd", session.ogd },

            // Experiencia del Jugador e Inmersión
            {"startingLives", session.startingLives },
            {"remainingLives", session.remainingLives },
            {"lifeLossIndex", session.lifeLossIndex },
            {"ease", session.ease },
            {"flowIndex", session.flowIndex },
            {"difficultyByTicks", session.difficultyByTicks },
            {"performanceDDA", session.performanceDDA },

            // Dificultad por Ticks e IA
            {"actionsPerformed_N", session.actionsPerformed_N },
            {"idealReferenceValue_Z", session.idealReferenceValue_Z },
            {"telemetryTicks_D", session.telemetryTicks_D },
            {"currentPerformance_Cp", session.currentPerformance_Cp },
            {"expectedPerformance_Ep", session.expectedPerformance_Ep },

            // Rendimiento del Motor y Estabilidad
            {"localLoadTimeMs", session.localLoadTimeMs },
            {"fpsDropTime", session.fpsDropTime },
            {"uptimePercentage", session.uptimePercentage },
            {"crashFreeTime", session.crashFreeTime },
            {"stabilityPercentage", session.stabilityPercentage },

            // Arquitectura Offline-First
            {"syncAttempts", session.syncAttempts },
            {"successfulSyncs", session.successfulSyncs },
            {"syncSuccessRate", session.syncSuccessRate },
            {"syncLatencyMs", session.syncLatencyMs },

            // Generación Procedural y Banderas
            {"totalProceduralRoutes", session.totalProceduralAttempts },
            {"successfulRoutes", session.successfulProceduralSpawns },
            {"routeSuccessRate", session.routeSuccessRate },
            {"isDDAActive", session.isDDAActive },
            {"isPendingSync", false }
        };
        try
        {
            await docRef.SetAsync(cloudData); // Guardar los datos en Firestore
            session.isPendingSync = false; // Marcar los datos como sincronizados
            Debug.Log($"[CloudService] Datos de Telemetría {session.sessionId} sincronizados correctamente en la nube.");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CloudService] Error al sincronizar datos de Telemetría {session.sessionId} en la nube: {e.Message}");
            return false;
        }
    }

    // Descarga los datos del jugador desde la nube.
    // Lanza excepción si la red/Firebase falla (el llamador registra el intento como fallido);
    // devuelve null si la nube respondió correctamente pero aún no existe documento del jugador.
    public async Task<PlayerData> DownloadPlayerData(string uid)
    {
        if (db==null)
        {
            throw new System.InvalidOperationException("[CloudService] Firebase no está inicializado; no se pueden descargar los datos.");
        }

        try
        {
            DocumentSnapshot snapshot = await db.Collection("players").Document(uid).GetSnapshotAsync(); // Obtener el documento del jugador desde Firestore
            if (snapshot.Exists)
            {
                // Convertir los datos del documento a un objeto PlayerData
                PlayerData downloadedData = new PlayerData
                {
                    playerId = snapshot.GetValue<string>("playerId") ?? uid,
                    playerName = snapshot.ContainsField("playerName") ? snapshot.GetValue<string>("playerName") : "Anonimo",
                    email = snapshot.ContainsField("email") ? snapshot.GetValue<string>("email") : "",

                    level = snapshot.ContainsField("level") ? snapshot.GetValue<int>("level") : 1,
                    score = snapshot.ContainsField("score") ? snapshot.GetValue<int>("score") : 0,
                    currentDifficulty = snapshot.ContainsField("currentDifficulty") ? snapshot.GetValue<float>("currentDifficulty") : 1.0f,
                    isPendingSync = snapshot.ContainsField("isPendingSync") ? snapshot.GetValue<bool>("isPendingSync") : false,
                    lastModified = snapshot.ContainsField("lastModified") ? snapshot.GetValue<long>("lastModified") : DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    totalProgressPercentage = snapshot.ContainsField("totalProgressPercentage") ? snapshot.GetValue<float>("totalProgressPercentage") : 0f,
                    currentLives = snapshot.ContainsField("currentLives") ? snapshot.GetValue<int>("currentLives") : 3,
                    highestLevelUnlocked = snapshot.ContainsField("highestLevelUnlocked") ? snapshot.GetValue<int>("highestLevelUnlocked") : 1,
                    levelRecords = snapshot.ContainsField("levelRecords") // NUEVO: antes siempre llegaba vacío y borraba tus records
                        ? JsonUtility.FromJson<ListaRecords>(snapshot.GetValue<string>("levelRecords")).records
                        : new List<LevelRecord>(),
                };
                Debug.Log($"[CloudService] Datos del jugador {uid} descargados correctamente desde la nube.");
                return downloadedData; 
            }

            else
            {
                // El jugador es nuevo: la nube SÍ respondió, pero aún no existe documento.
                // NO es un error -> se retorna null para que el llamador conserve los datos
                // locales y registre la sincronización como EXITOSA (la red funcionó).
                Debug.Log($"[CloudService] El jugador {uid} aún no tiene datos en la nube (primera vez). Se conservan los datos locales.");
                return null;

            }
            
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CloudService] Error al descargar datos del jugador {uid} desde la nube: {e.Message}");
            throw; // Lanza la excepción para que el llamador pueda manejarla (por ejemplo, para marcar un intento de sincronización como fallido)
        }
    }


    // ==================== TABLA DE CLASIFICACIÓN ONLINE (OPCIONAL) ====================
    [Serializable]
    public class EntradaRanking
    {
        public string alias;
        public int score;
        public long lastModified;
    }

    // Sube/actualiza el récord del jugador en la colección "scores" (upsert: 1 doc por jugador, sin duplicados)
    public async Task<bool> SubirPuntajeALeaderboard(PlayerData data)
    {
        if (db == null || data == null || data.score <= 0) return false; // Sin datos o puntaje vacío: nada que subir
        if (data.score <= 0)
        {
            Debug.Log("[CloudService] Ranking: no hay puntaje que subir (score = 0).");
            return false;
        }

        if (Application.internetReachability == NetworkReachability.NotReachable) return false; // SIN RED: sale sin intentar (sin errores)

        try
        {
            Dictionary<string, object> entrada = new Dictionary<string, object>
        {
            { "playerId", data.playerId },
            { "alias", string.IsNullOrEmpty(data.playerName) ? "Anonimo" : data.playerName },
            { "score", data.score },
            { "lastModified", DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
        };
            await db.Collection("scores").Document(data.playerId).SetAsync(entrada); // SetAsync = escritura atómica que reemplaza
            Debug.Log($"[CloudService] Puntaje {data.score} subido al ranking online.");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CloudService] Ranking no subido (se omitirá, no crashea): {e.Message}");
            return false; // NUNCA lanza excepción: el ranking jamás rompe la partida
        }
    }

    // Descarga el Top 10. Devuelve null si no hay red/falla → el llamador muestra el fallback local
    public async Task<List<EntradaRanking>> ObtenerTop10Ranking()
    {
        if (db == null)
        {
            Debug.LogWarning("[CloudService] Ranking: db no inicializado aún.");
            return null;
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)// Modo local inmediato
        {
            Debug.Log("[CloudService] Ranking: sin red → modo local inmediato.");
            return null;
        }


            try
        {
            Query consulta = db.Collection("scores").OrderByDescending("score").Limit(10);

            // TIMEOUT de 15 s: protege contra "red conectada pero sin internet" (portal cautivo, avión, etc.)
            var peticion = consulta.GetSnapshotAsync();
            var limite = Task.Delay(TimeSpan.FromSeconds(15));
            if (await Task.WhenAny(peticion, limite) != peticion)
            {
                Debug.LogWarning("[CloudService] Ranking: tiempo de espera agotado. Se usará el modo local.");
                return null;
            }

            QuerySnapshot snap = await peticion;
            List<EntradaRanking> top = new List<EntradaRanking>();
            foreach (DocumentSnapshot doc in snap.Documents)
            {
                top.Add(new EntradaRanking
                {
                    alias = doc.ContainsField("alias") ? doc.GetValue<string>("alias") : "Anonimo",
                    score = doc.ContainsField("score") ? doc.GetValue<int>("score") : 0,
                    lastModified = doc.ContainsField("lastModified") ? doc.GetValue<long>("lastModified") : 0
                });
            }

            if (top.Count == 0)
                Debug.LogWarning("[CloudService] Ranking: online OK pero la colección 'scores' está vacía (aún no se subió ningún puntaje).");
            return top;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CloudService] Ranking no disponible (modo local): {e.Message}");
            return null;
        }
    }

}
