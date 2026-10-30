using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DirectorIA : MonoBehaviour
{

    [Header("Parametros de Flujo DDA")]
    public float umbralAburrimiento = 0.75f; //Si el indice es mayor, el juego es muy facil
    public float umbralFrustracion = 0.35f; //Si el indice es menor, el juego es muy dificil
    
    private GameController gameController;
    private TelemetryService telemetryService;
    private float tiempoDeEvaluacion = 5f; // Cada X segundos la IA evalua al jugador 

    [Header("Generación Procedural (PCG)")]
    public GameObject trampaPrefab; // Arrastra aquí tu prefab (ej. una zona de pinchos o caja explosiva)
    public Transform jugador;       // Referencia para saber dónde generar el obstáculo
    public float radioDeAparicion = 15f; // Distancia máxima desde el jugador para instanciar
    public int maximoTrampasSimultaneas = 15; // 🛡️ LÍMITE: Máximo número de trampas activas a la vez

    // Lista para llevar el control de las trampas creadas
    private Queue<GameObject> trampasActivas = new Queue<GameObject>();

    [Header("Validación de Predicciones de IA (Matriz de Confusión + MAE)")]
    // Estado de la predicción hecha en el tick anterior, que se contrasta con la realidad en el siguiente tick
    private bool hayPrediccionPendiente = false;
    private bool predijoFrustracion = false;
    private float indiceFlujoPredicho = 0f;
    private int vidasEnElUltimoTick = 0;

    void Start()
    {
        gameController = FindAnyObjectByType<GameController>();
        telemetryService = FindAnyObjectByType<TelemetryService>();

        //Ejecutar la evaluacion en un ciclo repetitivo pero pausado para no saturar el procesador.
        InvokeRepeating(nameof(EvaluarRendimientoJugador), tiempoDeEvaluacion, tiempoDeEvaluacion);//
    }

    private void EvaluarRendimientoJugador()
    {
        if (gameController == null) return;
        
        //Si el DDA esta apagado, forzamos la dificultad a 1 (Estatico) y abortamos la IA.
        if (!gameController.activarDDA)
        {
            gameController.AjustarDificultadIA(1.0f);
            return;
        }

        // Extraer Telemetria en tiempo real
        int puntosActuales = gameController.ObtenerPuntajeActual();
        int vidas = gameController.vidasActuales;
        float tiempoJugado = Time.timeSinceLevelLoad;

        // Si por alguna razón no se encontró el servicio en Start, se re-busca (una sola vez, sin castigar el frame)
        if (telemetryService == null) telemetryService = FindAnyObjectByType<TelemetryService>();

        // Sumar +1 al Tick de Telemetría (D) cada vez que la IA evalúa al jugador
        if (telemetryService != null && telemetryService.IsSessionActive)
        {
            telemetryService.GetCurrentSession().telemetryTicks_D++;
        }
        //Calcular la funcion de Aptitud (Cp - Desempeño actual)
        float indiceFlujo = CalcularAptitud(puntosActuales, vidas, tiempoJugado);

        // 1) Contrastar la predicción del tick anterior con lo realmente ocurrido en esta ventana
        //    (alimenta la Matriz de Confusión VP/FP/FN y el MAE de la Variable Independiente)
        ValidarPrediccionAnterior(vidas, indiceFlujo);

        // 2) Guardar la predicción actual para validarla en el próximo tick:
        //    la IA predice "frustración" si el índice de flujo cae bajo el umbral de frustración
        predijoFrustracion = indiceFlujo < umbralFrustracion;
        indiceFlujoPredicho = indiceFlujo;
        vidasEnElUltimoTick = vidas;
        hayPrediccionPendiente = true;

        //El Desempeño Esperado (Ep) es el punto medio entre el aburrimiento y la frustración
        float desempeñoEsperado = (umbralAburrimiento + umbralFrustracion) / 2f;

        //Mandar datos a telemetría
        if (telemetryService != null)
        {
            telemetryService.RegistrarRendimientoDDA(indiceFlujo, desempeñoEsperado);
        }
        //Tomar Decision de Ajuste Dinamico (DDA)
        AjustarDificultad(indiceFlujo);
    }

    // Contrasta lo que la IA predijo en el tick anterior con la verdad real de la ventana evaluada.
    // VERDAD REAL: el jugador perdió al menos una vida => hubo frustración durante la ventana.
    private void ValidarPrediccionAnterior(int vidasActuales, float indiceFlujoActual)
    {
        if (!hayPrediccionPendiente) return;

        if (telemetryService == null || !telemetryService.IsSessionActive)
        {
            hayPrediccionPendiente = false; // La sesión cambió: se descarta la predicción anterior
            return;
        }

        bool ocurrioFrustracion = vidasActuales < vidasEnElUltimoTick;
        telemetryService.RegistrarPrediccionIA(predijoFrustracion, ocurrioFrustracion);

        // MAE: modelo de predicción por persistencia (ŷ(t+1) = y(t)), error absoluto vs. el valor real
        float errorPrediccion = Mathf.Abs(indiceFlujoActual - indiceFlujoPredicho);
        telemetryService.RegistrarErrorPrediccionIA(errorPrediccion);
    }

    private float CalcularAptitud(int puntos, int vidas, float tiempo)
    {
        //Evitamos dividir por cero en el primer segundo del juego
        if (tiempo <= 0.1f)
        {
            return umbralFrustracion + 1f;

        }

        float indice = (puntos / tiempo) * vidas;
        return indice;
    }

    private void AjustarDificultad(float indice)
    {
        if (indice > umbralAburrimiento)
        {
            Debug.Log($"[Director IA] Estado: Aburrimiento (Indice: {indice:F2}). Aumentando Dificultad.");
            // Le manda un factor mayor a 1 para hacer a los enemigos más rápidos y a las cajas menos valiosas
            gameController.AjustarDificultadIA(1.5f);

            GenerarTrampaProcedural();

        }
        else if (indice < umbralFrustracion)
        {
            Debug.Log($"[Director IA] Estado: Frustración (Indice: {indice:F2}). Reduciendo Dificultad.");
            // Le manda un factor menor a 1 para ralentizar al enemigo y ayudar al jugador
            gameController.AjustarDificultadIA(0.8f);
        }
        else
        {
            Debug.Log($"[Director IA] Estado: ZONA DE FLOW (Indice: {indice:F2}). Dificultad Normal.");
            // Restaura todo a la normalidad
            gameController.AjustarDificultadIA(1.0f);
        }
    }

    private void GenerarTrampaProcedural()
    {
        if (trampaPrefab == null || jugador == null) return;

        if (telemetryService != null) telemetryService.RegistrarIntentoRutaProcedural();


        // Damos un máximo de 3 intentos para encontrar un punto ciego válido.
        // Esto evita bucles infinitos que puedan congelar o cerrar inesperadamente el juego.
        for (int i = 0; i < 3; i++)
        {
            //Calcular un vector aleatorio alrededor del jugador
            Vector3 direccionAleatoria = Random.insideUnitSphere * radioDeAparicion;
            direccionAleatoria += jugador.position;

            NavMeshHit hit;

            //Evaluar si el punto calculado cae sobre el NavMesh 
            if (NavMesh.SamplePosition(direccionAleatoria, out hit, radioDeAparicion, NavMesh.AllAreas))
            {
                //Verificar que no aparezca exactamente encima del jugador
                if (Vector3.Distance(jugador.position, hit.position) > 5f)
                {
                    // Convertimos la posición 3D del mundo a coordenadas 2D de la pantalla (0 a 1)
                    Vector3 puntoEnPantalla = Camera.main.WorldToViewportPoint(hit.position);

                    // Si Z > 0, está frente a la cámara. Si X o Y están entre -0.2 y 1.2, está visible o muy cerca del borde.
                    bool estaEnCamara = puntoEnPantalla.z > 0 &&
                                        puntoEnPantalla.x > -0.2f && puntoEnPantalla.x < 1.2f &&
                                        puntoEnPantalla.y > -0.2f && puntoEnPantalla.y < 1.2f;

                    if(!estaEnCamara)
                    {
                       // --- NUEVO: mismos guards que SpawnerDDA (killzone + suelo + flotando) ---
                        bool sobreZonaMuerte = false;
                        foreach (Collider col in Physics.OverlapSphere(hit.position, 0.6f))
                            if (col.GetComponent<DeathZone>() != null) { sobreZonaMuerte = true; break; }
                        if (sobreZonaMuerte) continue;

                        if (Physics.Raycast(hit.position + Vector3.up, Vector3.down, out RaycastHit suelo, 8f))
                        {
                            if (suelo.collider.GetComponent<DeathZone>() != null) continue; // el suelo ES la killzone
                        }
                        else continue; // flotando sobre el vacío

                        if (hit.position.y < jugador.position.y - 15f) continue;

                        // --- FIN guards (el resto del bloque original sigue igual) ---


                        // CONTROL DE LÍMITES (Gestión de Memoria)
                        if (trampasActivas.Count >= maximoTrampasSimultaneas)
                        {
                            // Destruimos la trampa más antigua (FIFO)
                            GameObject trampaVieja = trampasActivas.Dequeue();
                            if (trampaVieja != null)
                            {
                                Destroy(trampaVieja);
                                Debug.Log("[Director IA] Límite alcanzado. Trampa antigua destruida para optimizar recursos.");
                            }
                        }

                        GameObject nuevaTrampa = Instantiate(trampaPrefab, hit.position, Quaternion.identity);
                        trampasActivas.Enqueue(nuevaTrampa);

                        //Informar que la trampa se genero exitosamente
                        if (telemetryService != null) telemetryService.RegistrarRutaProceduralExitosa();

                        Debug.Log($"[Director IA] Alteración del entorno: Nueva trampa generada en {hit.position}");
                        return;
                    }
                }
            }
        }
        Debug.Log("[Director IA] No se encontró un punto ciego válido para instanciar la trampa en este momento.");
    }

}

