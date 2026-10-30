using System;
using System.Collections.Generic;


//Clase usada para alamacenar los datos del jugador y poder guardarlos en un archivo JSON

[Serializable]
public class PlayerData
{
    public string playerId;// Identificador único del jugador
    public string playerName; // Nombre del jugador (opcional)
    public string email; // Correo electrónico del jugador (opcional)
    public int level;

    //ScoreGlobal (Suma de los bestScore de todos los niveles completados)
    public int score; 

    public float currentDifficulty;
    public bool isPendingSync;// Indica si los datos del jugador están pendientes de sincronización
    public long lastModified;// Marca de tiempo de la última modificación de los datos del jugador

    //Niveles y Progresion
    public float totalProgressPercentage; // Porcentaje total de progreso del jugador en el juego
    public int currentLives; // Número de vidas actuales del jugador
    public int highestLevelUnlocked; // Nivel más alto desbloqueado por el jugador

    //Records por nivel
    public List<LevelRecord> levelRecords = new List<LevelRecord>(); // Lista de registros de nivel del jugador>
    //Datos predeterminados para el jugador al iniciar el juego por primera vez

    public PlayerData()
    {
        playerId = Guid.NewGuid().ToString(); // Generar un identificador único para el jugador
        playerName = "Anonimo"; // Nombre predeterminado del jugador
        email = ""; // Correo electrónico predeterminado del jugador

        level = 1; // Nivel inicial del jugador
        score = 0; // Puntuación inicial del jugador

        currentDifficulty = 1.0f; // Valor predeterminado de dificultad (Facil)
        isPendingSync = true;// Los datos del jugador están pendientes de sincronización al inicio
        lastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); // Marca de tiempo de la última modificación (en segundos desde la época Unix)

        totalProgressPercentage = 0f; // Porcentaje total de progreso inicial del jugador
        currentLives = 3; // Número de vidas inicial del jugador
        highestLevelUnlocked = 1; // Nivel más alto desbloqueado inicial del jugador
    }
}

[Serializable]
public class LevelRecord
{
    public int levelId; // ID del nivel
    public int bestScore; // Mejor Puntuacion obtenida en el nivel
    public float bestTime; // Mejor tiempo de completación (Modo contrarreloj)
    public bool allColectiblesFound; // Indica si se han encontrado todos los coleccionables en el nivel
    public int timesPlayed;// Intentos totales de este Nivel (Telemetria)
    public int completionCount;//Veces que se llego a la meta (Telemetría)
    public float maxProgressPercentage;//Guarda el mayor % de progreso
    public float coleccionRatio; // NUEVO: puntos recolectados / puntos totales del nivel (0..1)

    public LevelRecord()
    {
        levelId = 0;
        bestScore = 0;
        bestTime = float.MaxValue;// Se incializa al maximo para que cualquier tiempo sea menor
        allColectiblesFound = false;
        timesPlayed = 0;
        completionCount = 0;
        maxProgressPercentage = 0f;
        coleccionRatio = 0f;
    }

    [Serializable]
    public class ListaRecords { public List<LevelRecord> records = new List<LevelRecord>(); } // NUEVO: envoltorio para serializar records a Firestore
}


