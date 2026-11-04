using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
// Se usaran Behavior Trees y no maquinas de estado finitas

public class EnemigoIA : MonoBehaviour
{
    private Node nodoRaiz;
    private NavMeshAgent agente;
    private GameController gameController;

    [Header("Atributos del Enemigo")]
    public Transform jugador;
    public float radioVisionBase = 15f;// NUEVO: Variable base para el Inspector
    [HideInInspector] public float radioVision = 15f;
    public float velocidadBase = 3.5f;

    [Header("Ajustes Físicos de Visión")]
    [Tooltip("Ajusta esto según la altura real de tu Jefe")]
    public float alturaOjosJefe = 1.7f;
    public float alturaPechoJugador = 0.6f;

    [Header("Ruta e Patrullaje")]
    public Transform[] puntosRuta;
    [HideInInspector] public int indiceRuta = 0;

    [Header("Memoria Auditiva")]
    [HideInInspector] public bool escuchoRuido = false;
    [HideInInspector] public Vector3 posicionRuido = Vector3.zero;

    [Header("Ataque y persecución")]
    [Tooltip("Multiplicador de velocidad al perseguir (antes era 1.4 fijo)")]
    public float multiplicadorPersecucion = 1.15f;
    public float radioAtaque = 2.0f;
    public float cooldownAtaque = 1.0f;
    [HideInInspector] public float ultimoAtaque = -999f;

    [Header("Aturdimiento por explosión (TNT/Nitro)")]
    [Tooltip("Segundos que dura el aturdimiento")]
    public float duracionAturdimiento = 3f;
    [Tooltip("Velocidad multiplicada mientras dure (0.4 = 40% de velocidad)")]
    public float factorAturdimiento = 0.4f;
    private float finAturdimiento = -999f;
    private float multiplicadorVelocidadActual = 1f; // runtime: 1 = normal

    [Header("Flash de la mina (arma)")]
    [Tooltip("Radio del flash: dentro + con línea de vista clara = lo aturde")]
    public float radioFlash = 8f;
    [Tooltip("Segundos que queda K.O. (muy poco tiempo, una vez por mina)")]
    public float duracionFlash = 1.2f;
    private float finFlash = -999f;
    private bool flashActivo = false;



    [Header("Distractor (mina de ruido)")]
    [Tooltip("Si te ve a esta distancia o menos, la mina NO lo distrae")]
    public float radioDistraccion = 6f;
    [Tooltip("Segundos que ignora al jugador mientras investiga el ruido")]
    public float duracionDistraccion = 8f;
    [HideInInspector] public bool distraidoPorMina = false;
    private float finDistraccion = -999f;

    [Header("Wobble visual durante el K.O. (sin assets)")]
    public float intensidadWobble = 8f;      // grados de inclinación
    public float frecuenciaWobble = 25f;     // velocidad del temblor
    private Transform modeloVisual;
    private Quaternion rotOriginalModelo;
    private Vector3 escalaOriginalModelo;

    [Header("Memoria de persecución (Hide & Seek)")]
    public float radioVerEscondite = 10f;
    public float memoriaPersecucion = 10f;
    [HideInInspector] public Transform esconditeConocido = null;
    [HideInInspector] public float ultimaPersecucion = -999f;
    private AnimacionEnemigoVisual anim; // se llena en Start

    void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        gameController = FindAnyObjectByType<GameController>();

        anim = GetComponentInChildren<AnimacionEnemigoVisual>();

        foreach (Transform h in transform)
        {
            if (h.GetComponentInChildren<Animator>() != null)
            {
                modeloVisual = h;
                rotOriginalModelo = h.localRotation;
                escalaOriginalModelo = h.localScale;
                break;
            }
        }
        // NUEVO: Autodetectar al jugador por si olvidas arrastrarlo en el Inspector
        if (jugador == null)
        {
            GameObject objJugador = GameObject.FindGameObjectWithTag("Player");
            if (objJugador != null) jugador = objJugador.transform;
            else Debug.LogError("[EnemigoIA] ¡CRÍTICO! No se encontró al jugador. Revisa el Tag 'Player'.");
        }

        radioVision = radioVisionBase; // Inicializamos con la base

        //Conexion al script DirectorIA 
        GameController.OnDificultadCambiada += AdaptarDificultad;

        ConstruirArbol();

    }
    void OnDestroy()
    {
        GameController.OnDificultadCambiada -= AdaptarDificultad;
    }

    private void AdaptarDificultad(float factorDDA)
    {
        //Alterar sentidos del enemigo
        radioVision = radioVisionBase * factorDDA;
        agente.speed = velocidadBase * factorDDA;

    }

    // La explosión NO lo mata: lo ATURDE y baja su velocidad unos segundos
    public void Ralentizar()
    {
        finAturdimiento = Time.time + duracionAturdimiento;
        multiplicadorVelocidadActual = Mathf.Min(multiplicadorVelocidadActual, factorAturdimiento); // si ya está aturdido, no lo acelera
        Debug.Log($"[EnemigoIA] ¡Aturdido por la explosión! ×{factorAturdimiento} velocidad por {duracionAturdimiento}s");
    }

    public void AturdirFlash(float duracion)
    {
        finFlash = Time.time + duracion;
        flashActivo = true;
        Debug.Log($"[EnemigoIA] ¡FLASH de la mina! Aturdido {duracion:F1}s.");
    }


    //Sistema de visión realista con Raycast (No atraviesa paredes)
    public bool PuedeVerAlJugador(GameController controller)
    {
        // Si el jugador está en un casillero, es invisible
        if (controller == null || controller.jugadorEscondido || jugador == null) return false;

        float distancia = Vector3.Distance(transform.position, jugador.position);

        // 1. Verificamos si está dentro del radio matemático
        if (distancia <= radioVision)
        {
            // Levantamos el rayo 1.5m para que salga de la "cabeza" del enemigo y no del suelo
            Vector3 origenRayo = transform.position + (Vector3.up * alturaOjosJefe);

            Vector3 objetivoRayo = jugador.position + (Vector3.up * alturaPechoJugador);

            // 2. Trazamos un Raycast (láser) hacia el jugador
            Vector3 direccionHaciaJugador = (objetivoRayo - origenRayo).normalized;

            // Disparamos un láser de Rayos X que atraviesa todo a su paso
            RaycastHit[] hits = Physics.RaycastAll(origenRayo, direccionHaciaJugador, radioVision);

            // Ordenamos los choques desde el más cercano al más lejano
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                // 1. Si el láser choca con el propio cuerpo del Jefe, lo ignoramos y el rayo sigue
                if (hit.transform.root == this.transform.root) continue;

                // 2. Si choca con frutas Wumpa o Checkpoints invisibles (Triggers), los ignora
                if (hit.collider.isTrigger) continue;

                // 3. Si lo primero SÓLIDO que toca es el jugador ¡Inicia persecución!
                if (hit.collider.CompareTag("Player"))
                {
                    // VERDE: ¡Te veo perfectamente!
                    Debug.DrawRay(origenRayo, direccionHaciaJugador * hit.distance, Color.green);
                    return true;
                }

                // ROJO: Choqué con un obstáculo (Pared, suelo, o tu propio modelo sin la etiqueta correcta)
                Debug.DrawRay(origenRayo, direccionHaciaJugador * hit.distance, Color.red);
                return false;
            }
            // AMARILLO: El láser no chocó con nada sólido
            Debug.DrawRay(origenRayo, direccionHaciaJugador * radioVision, Color.yellow);
        }
        return false; // Una pared bloqueó el láser o está muy lejos
    }

    //Puente para que el Distractor le hable al enemigo
    public void AlertarPorRuido(Vector3 origenRuido)
    {
        bool meVe = PuedeVerAlJugador(gameController);
        float distancia = jugador != null ? Vector3.Distance(transform.position, jugador.position) : 999f;

        // CASO 1 (ya existía): te ve y está MUY CERCA → la mina no sirve, sigue contigo
        if (meVe && distancia <= radioDistraccion)
        {
            Debug.Log("[EnemigoIA] Escuché la mina, pero estás muy cerca. ¡Te ignoro!");
            return;
        }

        // NUEVO: aunque te esté persiguiendo, si está LEJOS el ruido SÍ lo distrae:
        // suelta la persecución, va al ruido y te retoma al llegar
        escuchoRuido = true;
        posicionRuido = origenRuido;

        if (meVe)
        {
            distraidoPorMina = true;
            finDistraccion = Time.time + duracionDistraccion;
            Debug.Log("[EnemigoIA] ¡Me distraes! Dejo la persecución e investigo el ruido.");
        }
        else
        {
            Debug.Log("[EnemigoIA] Escuché el ruido. Voy a investigar.");
        }
    }

    public void ConocerEsconditeVisto(Transform escondite)
    {
        esconditeConocido = escondite;
        Debug.Log("[EnemigoIA] ¡Te vi entrar al escondite! Voy a buscarte.");
    }

    void Update()
    {// FLASH de la mina → el árbol está en pausa: no se mueve, no persigue, no ataca
        if (flashActivo)
        {
            if (agente != null) agente.isStopped = true;

            if (flashActivo)
            {
                if (agente != null) agente.isStopped = true;

                // NUEVO: temblor/oscilación del modelo mientras dure el K.O.
                if (modeloVisual != null)
                {
                    modeloVisual.localRotation = rotOriginalModelo
                        * Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * frecuenciaWobble) * intensidadWobble);
                    modeloVisual.localScale = escalaOriginalModelo * (1f + Mathf.Sin(Time.time * (frecuenciaWobble * 0.8f)) * 0.04f);
                }

                if (Time.time >= finFlash)
                {
                    flashActivo = false;
                    if (agente != null) agente.isStopped = false;

                    // NUEVO: restaurar la pose original al despertar
                    if (modeloVisual != null)
                    {
                        modeloVisual.localRotation = rotOriginalModelo;
                        modeloVisual.localScale = escalaOriginalModelo;
                    }
                    Debug.Log("[EnemigoIA] Recuperado del flash. Sigo con mi comportamiento normal.");
                }
                return;
            }

            if (Time.time >= finFlash)
            {
                flashActivo = false;
                if (agente != null) agente.isStopped = false; // los nodos retoman el control
                Debug.Log("[EnemigoIA] Recuperado del flash. Sigo con mi comportamiento normal.");
            }
            return;
        }

        //El arbol se evalua en cada frame, sin usar pesadas redes neuronales
        if (nodoRaiz != null)
        {
            nodoRaiz.Evaluate();
        }

        // El árbol reescribe la velocidad CADA frame → el aturdimiento se aplica DESPUÉS
        if (multiplicadorVelocidadActual < 1f)
        {
            if (Time.time > finAturdimiento) multiplicadorVelocidadActual = 1f; // se recupera solo
            else agente.speed *= multiplicadorVelocidadActual;
        }

        // Si nunca llega al ruido, la distracción expira y vuelve a su rutina
        if (distraidoPorMina && Time.time > finDistraccion)
        {
            distraidoPorMina = false;
            Debug.Log("[EnemigoIA] La distracción expiró. Volviendo a mi rutina.");
        }
    }


    // ========================================================
    // CONSTRUCCIÓN DEL ÁRBOL DE COMPORTAMIENTO
    // ========================================================

    private void ConstruirArbol()
    {
        //Nodo 1: Atacar (Solo si esta muy cerca)
        Node nodoAtacar = new AccionAtacar(agente, jugador, this, gameController);


        //Nodo 2: Perseguir (solo si el jugador esta en su radio de visión)
        Node nodoPerseguir = new AccionPerseguir(agente, jugador, this, gameController);

        //Nodo 3: La rama inteligente (Solo se activa si el jugador abusa de escondite)
        Node nodoInspeccionarEscondite = new AccionInspeccionar(agente,gameController,this);
       
        //Nodo 4: Inspeccionar Ruido
        Node nodoInvestigarRuido = new AccionInvestigarRuido(agente, this);

        //Nodo 5: Patrullar (Comportamiento por defecto)
        Node nodoPatrullar = new AccionPatrullar(agente, this);

        //Armar el Selector Raiz (Intentos de arriba hacia abajo)
        nodoRaiz = new Selector(new List<Node>
        {
            nodoAtacar,
            nodoPerseguir,
            nodoInspeccionarEscondite,
            nodoInvestigarRuido, // Se ejecuta si no te ve y no estás abusando de escondites
            nodoPatrullar
        });
    }

    // ========================================================
    // DEFINICIÓN DE LOS NODOS DE ACCIÓN (LAS HOJAS)
    // ========================================================

    public class AccionAtacar : Node
    {
        private NavMeshAgent agente;
        private Transform jugador;
        private GameController controller;
        private EnemigoIA cerebro;

        public AccionAtacar(NavMeshAgent ag, Transform jg, EnemigoIA bt, GameController ctrl)
        {
            agente = ag; jugador = jg; cerebro = bt; controller = ctrl;
        }
        public override NodeState Evaluate()
        {
            //Si el jugador está escondido, la IA no puede atacarlo directamente.
            if (controller.jugadorEscondido || controller.estaEnSecuenciaDeMuerte)
            {
                return NodeState.Failure;
            }

            // Máscara Nivel 3 (Aku Aku): se resbala y no puede atacar (tu diseño)
            PlayerMovement playerMov = jugador.GetComponent<PlayerMovement>();
            if (playerMov != null && playerMov.nivelMascara == 3)
            {
                return NodeState.Failure;
            }

            // Distancia HORIZONTAL: el eje Y ya no genera falsos positivos/negativos
            Vector3 dir = jugador.position - agente.transform.position;
            dir.y = 0f;

            if (dir.magnitude < cerebro.radioAtaque)
            {
                // Cooldown: un golpe por contacto, no uno por frame
                if (Time.time - cerebro.ultimoAtaque < cerebro.cooldownAtaque)
                    return NodeState.Running;

                cerebro.ultimoAtaque = Time.time;

                // MISMO flujo de dano que el resto de enemigos:
                // respeta mascaras, invencibilidad temporal, y solo mata con 0 mascaras
                if (playerMov != null)
                    playerMov.RecibirDaño(false, false, agente.transform.position);

                if (cerebro.anim != null) cerebro.anim.Atacar(); // feedback visual del golpe
                return NodeState.Running;
            }

            return NodeState.Failure;
        }

    }

    public class AccionPerseguir : Node
    {
        private NavMeshAgent agente;
        private Transform jugador;
        private EnemigoIA cerebro;
        private GameController controller;

        public AccionPerseguir (NavMeshAgent ag, Transform jg, EnemigoIA bt, GameController ctrl)
        {
            agente = ag; jugador = jg; cerebro = bt; controller = ctrl;
            
        }

        public override NodeState Evaluate()
        {
            if (cerebro.distraidoPorMina)
            {
                agente.speed = cerebro.velocidadBase * controller.ObtenerFactorDificultad();
                return NodeState.Failure;
            }

            if (controller.jugadorEscondido || controller.estaEnSecuenciaDeMuerte)
            {
                //Restaurar velocidad de caminata al perderlo de vista
                agente.speed = cerebro.velocidadBase * controller.ObtenerFactorDificultad();
                return NodeState.Failure;
            }

            if (cerebro.PuedeVerAlJugador(controller))
            {
                agente.isStopped = false; //Reanudar si estaba frenado
                float factorDDA = controller.ObtenerFactorDificultad();
                agente.speed = cerebro.velocidadBase * cerebro.multiplicadorPersecucion * factorDDA;
                agente.SetDestination(jugador.position);
                cerebro.ultimaPersecucion = Time.time; //Memoriza el momento de persecucion
                return NodeState.Running;
            }

            //Si lo acaba de perder de vista hace menos de 2.5s, camina a su ultima posicion
            if(Time.time - cerebro.ultimaPersecucion < 2.5f)
            {
                cerebro.posicionRuido = jugador.position;
                cerebro.escuchoRuido = true;
            }

            //restarura velocidad si se sale del radio de vision
            agente.speed = cerebro.velocidadBase * controller.ObtenerFactorDificultad();
            return NodeState.Failure;// Si no lo ve, falla
        }
    }

    //Nodo Inteligente: Busqueda aleatoria o por memoria listada
    public class AccionInspeccionar : Node
    {
        private NavMeshAgent agente;
        private GameController controller;
        private Escondite[] todosLosEscondites;
        private Transform destinoActual;
        private EnemigoIA cerebro;

        public AccionInspeccionar(NavMeshAgent ag, GameController ctrl, EnemigoIA bt) 
        { 
            agente = ag; 
            controller = ctrl; 
            cerebro = bt;
            todosLosEscondites = Object.FindObjectsByType<Escondite>(FindObjectsInactive.Exclude);
        }

        public override NodeState Evaluate()
        {
            if (cerebro.distraidoPorMina) return NodeState.Failure;

            if (cerebro.esconditeConocido == null && Time.time - cerebro.ultimaPersecucion > cerebro.memoriaPersecucion)
            {
                destinoActual = null;
                return NodeState.Failure;
            }

            // Prioridad 1: Va exactamente donde te vio meterte
            if (cerebro.esconditeConocido != null)
            {
                destinoActual = cerebro.esconditeConocido;
            }

            //Proteccion Anti-Crasheo
            //Si el controlador es nulo, o la lista de escondites esta vacia (porque aún no has puesto ninguno en el nivel), abortamos la acción sin crashear.
            if (controller == null || todosLosEscondites == null || todosLosEscondites.Length == 0 || controller.estaEnSecuenciaDeMuerte)
            {
                return NodeState.Failure;
            }

            if (controller.jugadorEstaASalvo)
            {
                destinoActual = null;
                return NodeState.Failure; //La IA "se rinde" temporalmente
            }

            //restaurar velocida de caminata normal durante la busqueda
            agente.speed = cerebro.velocidadBase * controller.ObtenerFactorDificultad();

            List<string> etiquetasPeligrosas = controller.ObtenerEsconditesPeligrosos(3);

            if (etiquetasPeligrosas.Count > 0 && destinoActual == null)
            {
                string etiquetaObjetivo = etiquetasPeligrosas[Random.Range(0, etiquetasPeligrosas.Count)];

                foreach (Escondite esc in todosLosEscondites)
                {
                    
                    // Ya no filtramos por esc.esZonaSegura, solo por la etiqueta
                    if (esc != null && esc.etiquetaEscondite == etiquetaObjetivo)
                    {
                        destinoActual = esc.transform;
                        Debug.Log($"[EnemigoIA] Buscando en tu lugar favorito: {etiquetaObjetivo}");
                        break;
                    }
                }
            }

            else if (destinoActual == null && controller.jugadorEscondido)
            {
                List<Escondite> candidatosCercanos = new List<Escondite>(todosLosEscondites);

                candidatosCercanos.Sort((a, b) =>
                            Vector3.Distance(a.transform.position, cerebro.transform.position).
                            CompareTo(Vector3.Distance (b.transform.position,cerebro.transform.position))
                            
                            );

                int limimte = Mathf.Min(2, candidatosCercanos.Count);
                Escondite esconditeSospechoso = candidatosCercanos[Random.Range(0, limimte)];

                if (esconditeSospechoso != null)
                {
                    destinoActual = esconditeSospechoso.transform;
                    Debug.Log($"[EnemigoIA] Intuyendo presencia cerca de: {esconditeSospechoso.etiquetaEscondite}");
                }

            }

            

            //Proteccion anticrash 2: Validar que el NavMeshAgent exista y tenga destino
            if (destinoActual != null && agente != null)
            {
                agente.SetDestination(destinoActual.position);

                if (agente.remainingDistance < 2f && !agente.pathPending)
                {
                    Escondite esconditeLlegada = destinoActual.GetComponent<Escondite>();

                    if (esconditeLlegada != null && esconditeLlegada.JugadorEstaAqui())
                    {
                        Debug.Log("[EnemigoIA] TE ATRAPE EN EL CASILLERO!");
                        esconditeLlegada.ForzarSalida(); // <-- Te expulsa automáticamente

                        PlayerMovement pm = cerebro.jugador.GetComponent<PlayerMovement>();

                        if (pm != null) pm.RecibirDaño(false, false, destinoActual.position);
                        else controller.IniciarMuerte();

                  
                        destinoActual = null;
                        cerebro.esconditeConocido = null;
                        return NodeState.Success;
                    }

                    // Si revisó y no estabas, limpia el destino y vuelve a patrullar
                    Debug.Log("[EnemigoIA] Revisé el escondite pero estaba vacío.");
                    destinoActual = null;
                    cerebro.esconditeConocido = null;
                    return NodeState.Failure;
                }
                return NodeState.Running;
            }
            return NodeState.Failure;
        }
    }



    public class AccionPatrullar : Node
    {
        private NavMeshAgent agente;
        private EnemigoIA cerebro;
        private bool estaMirandoAlrededor = false;
        private float tiempoMirando = 0f;
        public AccionPatrullar(NavMeshAgent ag, EnemigoIA bt) { agente = ag; cerebro = bt; }

        public override NodeState Evaluate()
        {
            if (estaMirandoAlrededor)
            {
                tiempoMirando += Time.deltaTime;
                agente.transform.Rotate(0, 60 * Time.deltaTime, 0);

                if (tiempoMirando >3f)
                {
                    estaMirandoAlrededor = false;
                    tiempoMirando = 0f;
                    IrSiguientePuntoRuta();
                }
                return NodeState.Running;
            }

            if (!agente.pathPending && agente.remainingDistance < 0.5f)
            {
                estaMirandoAlrededor = true;
                agente.isStopped = true;
            }
            else
            {
                agente.isStopped = false;
            }
            return NodeState.Running;
        }

        private void IrSiguientePuntoRuta()
        {
            if (cerebro.puntosRuta.Length == 0) return;
            agente.SetDestination(cerebro.puntosRuta[cerebro.indiceRuta].position);
            cerebro.indiceRuta = (cerebro.indiceRuta + 1) % cerebro.puntosRuta.Length;
        }
    }


    public class AccionInvestigarRuido: Node
    {
        private NavMeshAgent agente;
        private EnemigoIA cerebro;
        private bool inspeccionando = false; 
        private float finInspeccion = 0f;    

        public AccionInvestigarRuido(NavMeshAgent ag, EnemigoIA bt)
        {
            agente = ag; cerebro = bt;
        }

        public override NodeState Evaluate()
        {
            if (!cerebro.escuchoRuido)
            {
                inspeccionando = false; // si el ruido se apagó, resetea el estado
                return NodeState.Failure;
            }

            agente.SetDestination(cerebro.posicionRuido);

            // ¿Ya llegamos al ruido? (margen de 2 metros)
            if (agente.remainingDistance < 2.0f && !agente.pathPending)
            {
                // INSPECCIÓN: mira alrededor 1.5 s antes de retomar su rutina
                // (ventana de escape para el jugador; si te ve antes, te persigue igual)
                if (!inspeccionando)
                {
                    inspeccionando = true;
                    finInspeccion = Time.time + 1.5f;
                    agente.isStopped = true;
                    Debug.Log("[EnemigoIA] Llegué al ruido. Mirando alrededor...");
                    return NodeState.Running;
                }

                if (Time.time < finInspeccion) return NodeState.Running; // sigue inspeccionando

                inspeccionando = false;
                agente.isStopped = false;
                cerebro.escuchoRuido = false;        // Borramos la memoria auditiva
                cerebro.distraidoPorMina = false;    // termina la distracción → comportamiento anterior
                Debug.Log("[EnemigoIA] No hay nada aquí. Volviendo a mi rutina.");
                return NodeState.Success;
            }

            // Si lo interrumpieron y lo sacaron del sitio, reinicia la inspección
            if (agente.hasPath && agente.remainingDistance >= 2.0f) inspeccionando = false;

            return NodeState.Running;
        }
    }

}
