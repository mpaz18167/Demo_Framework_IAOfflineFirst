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
    private AnimacionEnemigoVisual anim; // se llena en Start

    void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        gameController = FindAnyObjectByType<GameController>();

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

    // NUEVO: Sistema de visión realista con Raycast (No atraviesa paredes)
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
        // Usamos el nuevo sistema de visión con físicas reales
        if (PuedeVerAlJugador(gameController))
        {
            Debug.Log("[EnemigoIA] Escuché la mina, pero te estoy viendo directamente. ¡Te ignoro!");
            return; // Abortamos la distracción
        }

        // Si la pared bloqueó su visión, entonces SÍ va a investigar la mina
        escuchoRuido = true;
        posicionRuido = origenRuido;
    }

    void Update()
    {
        //El arbol se evalua en cada frame, sin usar pesadas redes neuronales
        if (nodoRaiz != null)
        {
            nodoRaiz.Evaluate();
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
       
        //Nodo 4: Patrullar (Comportamiento por defecto)
        Node nodoPatrullar = new AccionPatrullar(agente, this);

        //Nodo 5: Inspeccionar Ruido
        Node nodoInvestigarRuido = new AccionInvestigarRuido(agente, this);

        //Armar el Selector Raiz (Intentos de arriba hacia abajo)
        nodoRaiz = new Selector(new List<Node>
        {
            nodoAtacar,
            nodoPerseguir,
            nodoInspeccionarEscondite, 
            nodoPatrullar,
            nodoInvestigarRuido // Se ejecuta si no te ve y no estás abusando de escondites
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
            if (controller.jugadorEscondido || controller.estaEnSecuenciaDeMuerte)
            {
                //Restaurar velocidad de caminata al perderlo de vista
                agente.speed = cerebro.velocidadBase * controller.ObtenerFactorDificultad();
                return NodeState.Failure;
            }


            // NUEVO Y CORREGIDO: Ahora el nodo pregunta si el rayo láser de visión te está tocando
            // CORRECCIÓN VITAL: Ahora la IA usa el láser para comprobar si te ve
            if (cerebro.PuedeVerAlJugador(controller))
            {
                float factorDDA = controller.ObtenerFactorDificultad();
                agente.speed = cerebro.velocidadBase * 1.4f * factorDDA;
                agente.SetDestination(jugador.position);
                return NodeState.Running;
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
                        Debug.Log("[EnemigoIA] TE ATRAPE");
                        controller.IniciarMuerte();
                        destinoActual = null;
                        return NodeState.Success;
                    }

                    // Si revisó y no estabas, limpia el destino y vuelve a patrullar
                    Debug.Log("[EnemigoIA] Revisé el escondite pero estaba vacío.");
                    destinoActual = null;
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

        public AccionInvestigarRuido(NavMeshAgent ag, EnemigoIA bt)
        {
            agente = ag;cerebro = bt;
        }

        public override NodeState Evaluate()
        {
            // Si no hay ruido registrado, este nodo fracasa y deja que el enemigo patrulle normal
            if (!cerebro.escuchoRuido)
            {
                return NodeState.Failure;
            }

            // Si hay un ruido, caminamos hacia esa posición
            agente.SetDestination(cerebro.posicionRuido);

            // Verificamos si ya llegamos al lugar del ruido (margen de 2 metros)
            if (agente.remainingDistance < 2.0f && !agente.pathPending)
            {
                Debug.Log("[EnemigoIA] Llegué a la zona del ruido. No hay nada aquí.");
                cerebro.escuchoRuido = false; // Borramos la memoria auditiva
                return NodeState.Success;
            }

            return NodeState.Running;
        }
    }

}
