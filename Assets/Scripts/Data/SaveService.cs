using System;
using System.IO;
using UnityEngine;

public class SaveService : MonoBehaviour
{
    // Método para guardar los datos del jugador en un archivo JSON
    private const string PLAYER_FILE = "player_data.json";

    private const string TELEMETRY_FILE = "telemetry_data.json"; // Nombre del archivo para guardar los datos de telemetría

    //Ruta segura offline para guardar datos en cualquier dispositivo de forma autónoma.
    private string PlayerSavePath => Path.Combine(Application.persistentDataPath, PLAYER_FILE );
    private string TelemetrySavePath => Path.Combine(Application.persistentDataPath, TELEMETRY_FILE);

    public void SaveDataLocal(PlayerData data)
    {
        if (data == null)
        {
            Debug.LogError("[SaveService] Intento de guardar PlayerData null.");
            return;
        }
        
        try
        {
            data.lastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); // Actualizar la marca de tiempo al guardar

            // Convertir los datos del jugador a formato JSON y guardarlos en un archivo
            string json = JsonUtility.ToJson(data, true);

            if (string.IsNullOrEmpty(PlayerSavePath))
            {
                Debug.LogError("[SaveService] Ruta de guardado inválida.");
                return;
            }

            // Guardar el archivo JSON en la ruta especificada
            File.WriteAllText(PlayerSavePath, json);

            Debug.Log($"[SaveService] Datos guardados correctamente en: {PlayerSavePath}");

        }
        catch (UnauthorizedAccessException ex)
        {
            Debug.LogError($"[SaveService] Error de permisos al guardar: {ex.Message}");
        }
        catch (DirectoryNotFoundException ex)
        {
            Debug.LogError($"[SaveService] Directorio no encontrado: {ex.Message}");
        }
        catch (IOException ex)
        {
            Debug.LogError($"[SaveService] Error de I/O al guardar: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveService] Error inesperado al guardar: {ex.Message}");
        }
    }

    public PlayerData LoadDataLocal()
    {
        

        try
        {
            if (!File.Exists(PlayerSavePath))
            {
                Debug.Log("[SaveService] Archivo no encontrado. Creando datos nuevos.");
                PlayerData newData = new PlayerData();
                SaveDataLocal(newData);
                return newData; // Retorna un objeto PlayerData vacío si no existe el archivo
            }


            // Leer el archivo JSON y convertirlo a un objeto PlayerData
            string json = File.ReadAllText(PlayerSavePath);

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[SaveService] Archivo vacío. Retornando datos nuevos.");
                return new PlayerData();
            }

            PlayerData loadedData = JsonUtility.FromJson<PlayerData>(json);
            
            if (loadedData == null)
            {
                Debug.LogWarning("[SaveService] La deserialización retornó null. Retornando datos nuevos.");
                return new PlayerData();
            }

            Debug.Log($"[SaveService] Datos cargados correctamente: {loadedData.playerId}");
            return loadedData;

        }
        catch (FileNotFoundException ex)
        {
            Debug.LogWarning($"[SaveService] Archivo no encontrado: {ex.Message}. Creando datos nuevos.");
            return new PlayerData();
        }
        catch (UnauthorizedAccessException ex)
        {
            Debug.LogError($"[SaveService] Error de permisos al cargar: {ex.Message}");
            return new PlayerData();
        }
        catch (IOException ex)
        {
            Debug.LogError($"[SaveService] Error de I/O al cargar: {ex.Message}");
            return new PlayerData();
        }
        catch (ArgumentException ex)
        {
            Debug.LogError($"[SaveService] Ruta inválida: {ex.Message}");
            return new PlayerData();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveService] Error inesperado al cargar datos: {ex.Message}");
            return new PlayerData();
        }

    }

    public void SaveTelemetryLocal (SessionData sessionData)
    {
        if (sessionData == null)
        {
            Debug.LogError("[SaveService] Intento de guardar SessionData null.");
            return;
        }


        try
        {
            // Convertir los datos de telemetría a formato JSON y guardarlos en un archivo
            string json = JsonUtility.ToJson(sessionData, true);
            
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("[SaveService] No se pudo serializar SessionData");
                return;
            }


            // Guardar el archivo JSON en la ruta especificada
            File.WriteAllText(TelemetrySavePath, json);
            Debug.Log($"[SaveService] Datos de telemetría guardados correctamente en: {TelemetrySavePath}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Debug.LogError($"[SaveService] Error de permisos al guardar telemetría: {ex.Message}");
        }
        catch (DirectoryNotFoundException ex)
        {
            Debug.LogError($"[SaveService] Directorio no encontrado: {ex.Message}");
        }
        catch (IOException ex)
        {
            Debug.LogError($"[SaveService] Error de I/O al guardar telemetría: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveService] Error inesperado al guardar telemetría: {ex.Message}");
        }
    }
}
