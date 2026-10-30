using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class EscudosVisuales : MonoBehaviour
{
    [Header("Escudos (arrastra 3, hijos del Player)")]
    public GameObject[] escudos;
    public float radio = 0.8f;
    public float alturaCabeza = 1.7f;

    [Header("Orbita")]
    public float giroNormal = 90f;
    public float giroNivel3 = 220f;

    [Header("Brillo nivel 3 (necesita EmissionColor en ToonCel)")]
    public Renderer[] renderersPlayer;      // vacio = auto (excluye escudos)
    public Color colorBrillo = new Color(1f, 0.82f, 0.2f);
    [Range(0f, 5f)] public float intensidadBrillo = 1.8f;
    public float pulsosPorSegundo = 2.5f;

    [Header("Feedback visual al PERDER mascara")]
    public Color colorFlashDanio = new Color(1f, 0.25f, 0.2f);
    public float duracionFlash = 0.3f;
    public float intervaloParpadeo = 0.06f;

    [Header("Retroceso atras (LateUpdate, no se corta al moverte)")]
    public bool aplicarRetroceso = true;
    [Tooltip("Segundos hasta que empiece = sincrono con el impacto de Hit_A")]
    public float retrasoRetroceso = 0.12f;
    public float fuerzaRetroceso = 8f;      // suave: "el golpe te empuja"
    public float amortiguacion = 4f;
    public float duracionRetroceso = 0.4f;
    public float empujeVertical = 0.35f;

    [Header("Boost de velocidad al perder mascara (escrita en campo PUBLICO)")]
    public bool activarBoost = true;
    [Range(1f, 1.4f)] public float multiplicadorBoost = 1.2f;
    public float duracionBoost = 1f;

    private PlayerMovement player;
    private GameController gameController;
    private Animator animatorVisual;
    private CharacterController controller;
    private int nivelPrevio;
    private float angulo;
    private Vector3 velocidadRetroceso;
    private float tiempoRetroceso;
    private bool parpadeando;
    private MaterialPropertyBlock mpb;

    private void Awake()
    {
        player = GetComponentInParent<PlayerMovement>();
        controller = GetComponentInParent<CharacterController>();
        animatorVisual = GetComponentInChildren<Animator>();
        gameController = FindAnyObjectByType<GameController>();
        mpb = new MaterialPropertyBlock();
        RecolectarRenderers();
    }

    private void RecolectarRenderers()
    {
        if (renderersPlayer != null && renderersPlayer.Length > 0) return;
        var lista = new System.Collections.Generic.List<Renderer>();
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            bool esEscudo = false;
            if (escudos != null)
                foreach (GameObject e in escudos)
                    if (e != null && r.transform.IsChildOf(e.transform)) { esEscudo = true; break; }
            if (!esEscudo) lista.Add(r);
        }
        renderersPlayer = lista.ToArray();
    }

    private void Start()
    {
        nivelPrevio = player != null ? player.nivelMascara : 0;
        ActualizarEscudos(nivelPrevio);
        PonerTinte(Color.clear);
    }

    private void ActualizarEscudos(int nivel)
    {
        for (int i = 0; escudos != null && i < escudos.Length; i++)
            if (escudos[i] != null) escudos[i].SetActive(i < nivel);
    }

    private void Update()
    {
        if (player == null) return;

        int nivel = player.nivelMascara;

        if (nivel != nivelPrevio)
        {
            bool perdio = nivel < nivelPrevio;
            ActualizarEscudos(nivel);
            if (perdio)
            {
                float duracionInvuln = player.duracionParpadeoDanio; // SOY LECTURA
                StopAllCoroutines(); // solo corta nuestras coroutines visuales
                StartCoroutine(FeedbackDanio(duracionInvuln));
                StartCoroutine(BoostVelocidad());
                if (aplicarRetroceso) Invoke(nameof(DispararRetroceso), retrasoRetroceso);
            }
            if (nivel != 3 && !parpadeando) PonerTinte(Color.clear);
            nivelPrevio = nivel;
        }

        float giro = nivel == 3 ? giroNivel3 : giroNormal;
        angulo += giro * Time.deltaTime;
        int activos = Mathf.Min(nivel, escudos != null ? escudos.Length : 0);
        for (int i = 0; escudos != null && i < escudos.Length; i++)
        {
            GameObject e = escudos[i];
            if (e == null) continue;
            bool on = i < activos;
            if (e.activeSelf != on) e.SetActive(on);
            if (!on) continue;
            float a = (angulo + i * 360f / Mathf.Max(1, activos)) * Mathf.Deg2Rad;
            e.transform.localPosition = new Vector3(Mathf.Sin(a) * radio, alturaCabeza, Mathf.Cos(a) * radio);
            e.transform.Rotate(Vector3.up, giro * 2f * Time.deltaTime, Space.Self);
        }

        if (nivel == 3 && !parpadeando)
            PonerTinte(colorBrillo * (intensidadBrillo * (0.7f + 0.3f * Mathf.Sin(Time.time * pulsosPorSegundo * 6.283f))));
    }

    // Boost: escribe en tu campo PUBLICO velocidadBase y lo restaura exacto
    private System.Collections.IEnumerator BoostVelocidad()
    {
        if (!activarBoost || player == null) yield break;
        player.velocidadBase *= multiplicadorBoost;
        yield return new WaitForSeconds(duracionBoost);
        player.velocidadBase /= multiplicadorBoost; // multiplicativo = exacto
    }

    private void DispararRetroceso()
    {
        Vector3 dir;
        if (animatorVisual != null) dir = -animatorVisual.transform.forward;
        else dir = -transform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) dir = -transform.forward;
        velocidadRetroceso = dir.normalized * fuerzaRetroceso + Vector3.up * empujeVertical;
        tiempoRetroceso = duracionRetroceso;
    }

    private void LateUpdate()
    {
        if (tiempoRetroceso > 0f && controller != null && controller.enabled
            && (gameController == null || !gameController.estaEnSecuenciaDeMuerte))
        {
            controller.Move(velocidadRetroceso * Time.deltaTime);
            velocidadRetroceso = Vector3.Lerp(velocidadRetroceso, Vector3.zero,
                                              amortiguacion * Time.deltaTime);
            tiempoRetroceso -= Time.deltaTime;
        }
        else if (tiempoRetroceso > 0f)
        {
            tiempoRetroceso = 0f;
        }
    }

    private System.Collections.IEnumerator FeedbackDanio(float duracionInvuln)
    {
        parpadeando = true;

        float tFlash = 0f;
        while (tFlash < duracionFlash)
        {
            float k = 1f - (tFlash / duracionFlash);
            PonerTinte(colorFlashDanio * (2.5f * k));
            tFlash += Time.deltaTime;
            yield return null;
        }

        float fin = Time.time + Mathf.Max(0f, duracionInvuln - duracionFlash);
        bool visible = true;
        while (Time.time < fin)
        {
            visible = !visible;
            foreach (Renderer r in renderersPlayer) if (r != null) r.enabled = visible;
            PonerTinte(visible ? Color.clear : colorFlashDanio * 0.8f);
            yield return new WaitForSeconds(intervaloParpadeo);
        }

        foreach (Renderer r in renderersPlayer) if (r != null) r.enabled = true;
        PonerTinte(nivelPrevio == 3 ? colorBrillo * intensidadBrillo : Color.clear);
        parpadeando = false;
    }

    private void PonerTinte(Color c)
    {
        if (renderersPlayer == null) return;
        mpb.Clear();
        if (c != Color.clear) mpb.SetColor("_EmissionColor", c);
        foreach (Renderer r in renderersPlayer) if (r != null) r.SetPropertyBlock(mpb);
    }
}