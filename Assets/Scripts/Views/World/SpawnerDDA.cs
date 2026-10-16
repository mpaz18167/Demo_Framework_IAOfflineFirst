using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SpawnerDDA : MonoBehaviour
{
    [Header("Catálogo de Elementos Individuales")]
    [SerializeField] private GameObject[] catalogoTrampas; // 1 Nitro, 1 TNT, 1 Enemigo
    [Header("Catálogo de Ayudas (Separadas por Rareza)")]
    [SerializeField] private GameObject[] catalogoAyudasMenores; // Wumpas, Cajas de Madera
    [SerializeField] private GameObject[] catalogoAyudasMayores; // Vidas, Máscaras

    [Header("Catálogo de Formaciones Complejas (Crash)")]
    [Tooltip("Ej: Muros gigantes de Nitro, Torres de TNT")]
    [SerializeField] private GameObject[] formacionesTrampa;

    [Tooltip("Ej: Puentes seguros de cajas, Cuartos de bonus con Wumpas")]
    [SerializeField] private GameObject[] formacionesAyuda;

    [Tooltip("Ej: Pilares de madera con una Nitro escondida en el medio")]
    [SerializeField] private GameObject[] formacionesMixtas;

    [Header("Configuración Espacial")]
    [Tooltip("Arrastra aquí el objeto que representa el final del nivel o la Meta")]
    [SerializeField] private Transform metaNivel;
    [SerializeField] private float radioDeAparicion = 15f;
    [SerializeField] private float distanciaMinimaAlJugador = 6f;
    [SerializeField] private int maximoGruposSimultaneos = 8;

    private Transform jugador;
    private GameController gameController;
    private Queue<GameObject> elementosGenerados = new Queue<GameObject>();
    // Memoria del punto más avanzado que el jugador ha pisado
    private Vector3 fronteraExplorada;
    void Awake()
    {
        GameObject objPlayer = GameObject.FindGameObjectWithTag("Player");
        if (objPlayer != null) jugador = objPlayer.transform;
    }

    void Start()
    {
        gameController = FindAnyObjectByType<GameController>();
        if (jugador != null) fronteraExplorada = jugador.position;
    }

    void OnEnable()
    {
        GameController.OnDificultadCambiada += EvaluarSpawnEnVivo;
        GameController.OnReconfiguracionEntorno += EvaluarSpawnEnRespawn;
    }

    void OnDisable()
    {
        GameController.OnDificultadCambiada -= EvaluarSpawnEnVivo;
        GameController.OnReconfiguracionEntorno -= EvaluarSpawnEnRespawn;
    }

    void Update()
    {
        // Actualizamos la frontera constantemente si el jugador avanza hacia la meta
        if (jugador != null && metaNivel != null)
        {
            float distanciaActual = Vector3.Distance(jugador.position, metaNivel.position);
            float distanciaFrontera = Vector3.Distance(fronteraExplorada, metaNivel.position);

            if (distanciaActual < distanciaFrontera)
            {
                fronteraExplorada = jugador.position;
            }
        }
    }

    private void EvaluarSpawnEnVivo(float factorDificultad)
    {
        //Intentar generar un objeto procedimental si el jugador no lo ve.
        ProcesarSpawn(factorDificultad, true);
    }

    private void EvaluarSpawnEnRespawn()
    {
        if (gameController != null)
        {
            // Generamos ignorando la cámara (la pantalla está en negro)
            ProcesarSpawn(gameController.ObtenerFactorDificultad(), false);
        }
    }

    private void ProcesarSpawn(float factor, bool respetarCamara)
    {
        GameObject prefabElegido = null;
        bool esRecompensaMayor = false;

        if (factor > 1.15f)
        {
            prefabElegido = SeleccionarAleatorio(Random.value > 0.5f ? formacionesTrampa : formacionesMixtas);
        }
        else if (factor > 1.05f)
        {
            prefabElegido = SeleccionarAleatorio(Random.value > 0.7f ? formacionesMixtas : catalogoTrampas);
        }
        else if (factor < 0.85f)
        {
            // Frustración extrema: 85% de dar una ayuda menor, SOLO 15% de soltar una Vida/Máscara
            if (Random.value < 0.15f)
            {
                prefabElegido = SeleccionarAleatorio(catalogoAyudasMayores);
                esRecompensaMayor = true; // Multiplica la distancia para que deba explorarla
            }
            else
            {
                prefabElegido = SeleccionarAleatorio(formacionesAyuda);
            }
        }
        else if (factor < 0.95f)
        {
            // Frustración leve: 95% de Wumpas, 5% milagroso de soltar una Máscara
            prefabElegido = SeleccionarAleatorio(Random.value < 0.05f ? catalogoAyudasMayores : catalogoAyudasMenores);
        }
        else
        {
            // ======================================================
            // ¡NUEVO! ESTADO DE FLOW (Cuando el factor es aprox 1.0f)
            // Lanza cosas normales para que el nivel no se sienta vacío
            // ======================================================
            prefabElegido = SeleccionarAleatorio(catalogoAyudasMenores); // También puedes poner formacionesMixtas
        }
        if (prefabElegido != null) GenerarElemento(prefabElegido, respetarCamara, esRecompensaMayor);
    }


    private GameObject SeleccionarAleatorio(GameObject[] catalogo)
    {
        if (catalogo == null || catalogo.Length == 0) return null;
        return catalogo[Random.Range(0, catalogo.Length)];
    }



    private void GenerarElemento(GameObject prefab, bool respetarCamara, bool esRecompensaMayor)
    {
        if (prefab == null || jugador == null || Camera.main == null) return;

        // Si es una vida/ayuda fuerte, exigimos el doble de distancia para darle "sentido" de exploración
        float distanciaRequerida = esRecompensaMayor ? distanciaMinimaAlJugador * 2.5f : distanciaMinimaAlJugador;

        Vector3 puntoBase = fronteraExplorada;
        Vector3 direccionHaciaMeta = jugador.forward;

        // Trazado de ruta inteligente usando el NavMesh
        if (metaNivel != null)
        {
            NavMeshPath ruta = new NavMeshPath();

            if (NavMesh.CalculatePath(puntoBase, metaNivel.position, NavMesh.AllAreas, ruta) && ruta.corners.Length > 1)
            {
                direccionHaciaMeta = (ruta.corners[1] - puntoBase).normalized;
            }
            else
            {
                direccionHaciaMeta = (metaNivel.position - puntoBase).normalized;
            }
        }

        Quaternion rotacionAlineada = Quaternion.LookRotation(new Vector3(direccionHaciaMeta.x, 0, direccionHaciaMeta.z));

        for (int i = 0; i < 5; i++) // 5 intentos para mayor precisión
        {
            Vector3 direccionAleatoria = Random.insideUnitSphere;

            if (Vector3.Dot(direccionAleatoria, direccionHaciaMeta) < 0)
            {
                direccionAleatoria = -direccionAleatoria;
            }

            Vector3 puntoFinal = puntoBase + (direccionAleatoria * radioDeAparicion);

            if (NavMesh.SamplePosition(puntoFinal, out NavMeshHit hit, radioDeAparicion, NavMesh.AllAreas))
            {
                if (Vector3.Distance(puntoBase, hit.position) > distanciaRequerida)
                {
                    if (respetarCamara)
                    {
                        Vector3 vp = Camera.main.WorldToViewportPoint(hit.position);
                        bool enCamara = vp.z > 0 && vp.x > -0.1f && vp.x < 1.1f && vp.y > -0.1f && vp.y < 1.1f;
                        //if (enCamara) continue;
                    }

                    // Radio de 2.5f para evitar solapamientos con paredes
                    if (Physics.CheckSphere(hit.position + Vector3.up, 0.5f)) continue;

                    if (elementosGenerados.Count >= maximoGruposSimultaneos)
                    {
                        GameObject viejo = elementosGenerados.Dequeue();
                        if (viejo != null) Destroy(viejo);
                    }

                    elementosGenerados.Enqueue(Instantiate(prefab, hit.position, rotacionAlineada));
                    return;
                }
            }
        }
    }
}
