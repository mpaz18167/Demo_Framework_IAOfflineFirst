using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

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

    public async Task SyncToCloud(PlayerData data)
    {
        // si db es null, no se puede sincronizar
        if (db == null)
        {
            Debug.Log("[CloudService] Intentado subir Datos, pero Firebase no está inicializado.");
            return;
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
            {"isPendingSync", false }
        };
        try
        {
            await docRef.SetAsync(cloudData); // Guardar los datos en Firestore

            data.isPendingSync = false; // Marcar los datos como sincronizados
            Debug.Log($"[CloudService] Datos del jugador {data.playerId} sincronizados correctamente con la nube.");

        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CloudService] Error al sincronizar datos del jugador {data.playerId} con la nube: {e.Message}");
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

    public async Task SyncTelemetry(SessionData session)
    {
        if (db == null)
        {
            Debug.Log("[CloudService] Intentado subir Datos de Telemetría, pero Firebase no está inicializado.");
            return;
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
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CloudService] Error al sincronizar datos de Telemetría {session.sessionId} en la nube: {e.Message}");
        }
    }

    public async Task<PlayerData> DownloadPlayerData(string uid)
    {
        if (db==null)
        {
            Debug.Log("[CloudService] Intentado descargar Datos, pero Firebase no está inicializado.");
            return null;
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
                };
                Debug.Log($"[CloudService] Datos del jugador {uid} descargados correctamente desde la nube.");
                return downloadedData; 
            }

            else
            {
                Debug.Log($"[CloudService] No se encontraron datos para el jugador {uid} en la nube. Retornando null.");
                return null;
            }
            
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CloudService] Error al descargar datos del jugador {uid} desde la nube: {e.Message}");
            return null;
        }
    }

}
