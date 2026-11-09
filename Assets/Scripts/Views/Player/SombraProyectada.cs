using UnityEngine;
using UnityEngine.Rendering;

// VISUAL ONLY (aditivo): sombra proyectada debajo del personaje para ver DÓNDE caerás.
// Solo LEE posiciones; no escribe en ningún sistema de juego.
// Funciona en el Player y en enemigos: cada uno genera SU sombra sola al arrancar.
[DisallowMultipleComponent]
public class SombraProyectada : MonoBehaviour
{
    [Header("Ajustes")]
    public float radioBase = 0.45f;            // radio en el suelo
    public float alturaMaximaVisible = 8f;     // altura a la que desaparece del todo
    public float crecimientoPorAltura = 0.12f; // crece por metro de altura (indica dónde caerás)
    public float separacionSuelo = 0.02f;      // separación anti z-fighting
    public float opacidad = 0.55f;
    [Tooltip("Marcado = solo visible en el aire")]
    public bool soloEnAire = false;

    [Header("Forma")]
    [Tooltip("1 = círculo perfecto. Ajusta X/Y para óvalo (ej. 1.2 / 0.8)")]
    public float formaAncho = 1f;
    public float formaAlto = 1f;

    [Header("Material (opcional)")]
    [Tooltip("Vacío = se genera solo con Sprites/Default. Si lo creas a mano, usa Sprites/Default y NO le asignes textura (esta script la pone)")]
    public Material materialSombra;

    private Transform sombra;
    private Renderer rend;
    private MaterialPropertyBlock mpb;
    private Texture2D textura;
    private CharacterController cc;
    private Collider col;

    private void Start()
    {
        Shader shader = Shader.Find("Sprites/Default");
        Material mat = materialSombra != null ? materialSombra
                     : (shader != null ? new Material(shader) : null);
        if (mat == null)
        {
            Debug.LogWarning("[SombraProyectada] Asigna un material con shader Sprites/Default (para builds).");
            enabled = false;
            return;
        }

        textura = TexturaCircular(128); // círculo suave generado por código
        cc = GetComponent<CharacterController>();
        col = GetComponent<Collider>();

        // FIX: en vez de un Quad (cuadrado si el material no recorta el alfa), se genera
        // una malla de DISCO real → la forma ya es circular aunque el material sea opaco.
        GameObject go = new GameObject("Sombra");
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = CrearMallaDisco(48);
        rend = go.AddComponent<MeshRenderer>();
        rend.sharedMaterial = mat;
        rend.shadowCastingMode = ShadowCastingMode.Off;
        rend.receiveShadows = false;

        sombra = go.transform;
        mpb = new MaterialPropertyBlock();
    }

    private void LateUpdate()
    {
        if (sombra == null) return;

        float alturaOrigen = 1.5f;
        Vector3 origen = transform.position + Vector3.up * alturaOrigen;
        // FIX: QueryTriggerInteraction no tiene "Include" → "Collide" = incluye triggers
        RaycastHit[] golpes = Physics.RaycastAll(origen, Vector3.down, alturaOrigen + 80f, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(golpes, (a, b) => a.distance.CompareTo(b.distance));

        bool haySuelo = false;
        RaycastHit suelo = default;
        foreach (RaycastHit g in golpes)
        {
            if (g.collider.transform.root == transform.root) continue;              // ignora TU collider
            if (g.collider.isTrigger && g.collider.GetComponent<IDamage>() == null)
                continue; // pickups/escondites no son suelo... pero los enemigos (IDamage) SÍ
            suelo = g; haySuelo = true; break;
        }

        if (!haySuelo) { sombra.gameObject.SetActive(false); return; }
        sombra.gameObject.SetActive(true);

        // Pies reales: CharacterController si existe, si no su collider
        float piesY = transform.position.y;
        if (cc != null) piesY = cc.bounds.min.y;
        else if (col != null) piesY = col.bounds.min.y;

        float altura = Mathf.Max(0f, piesY - suelo.point.y);

        sombra.position = suelo.point + suelo.normal * separacionSuelo;
        sombra.rotation = Quaternion.Euler(90f, 0f, 0f); // siempre plano, sin heredar el giro del personaje
        float escala = radioBase * 2f * (1f + altura * crecimientoPorAltura);
        sombra.localScale = new Vector3(escala * formaAncho, escala * formaAlto, 1f); // óvalo opcional

        float alfa = Mathf.Clamp01(1f - altura / Mathf.Max(0.1f, alturaMaximaVisible)) * opacidad;
        if (soloEnAire && altura <= 0.06f) alfa = 0f;

        mpb.Clear();
        mpb.SetTexture("_MainTex", textura);
        mpb.SetColor("_Color", new Color(0f, 0f, 0f, alfa));    // Sprites/Default
        mpb.SetColor("_BaseColor", new Color(0f, 0f, 0f, alfa)); // fallback URP
        rend.SetPropertyBlock(mpb);
    }

    // Malla de disco (círculo lleno) generada por código: 48 segmentos, UV radiales
    // para que la textura suave se mapee desde el centro.
    private static Mesh CrearMallaDisco(int segmentos)
    {
        Vector3[] vertices = new Vector3[segmentos + 1];
        Vector2[] uvs = new Vector2[segmentos + 1];
        vertices[0] = Vector3.zero;               // centro
        uvs[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < segmentos; i++)
        {
            float angulo = (i / (float)segmentos) * Mathf.PI * 2f;
            Vector2 dir = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));
            vertices[i + 1] = dir * 0.5f;                 // radio 0.5 → diámetro 1 (la escala manda)
            uvs[i + 1] = dir * 0.5f + Vector2.one * 0.5f;
        }

        int[] triangulos = new int[segmentos * 3];
        for (int i = 0; i < segmentos; i++)
        {
            triangulos[i * 3] = 0;                                 // centro
            triangulos[i * 3 + 1] = ((i + 1) % segmentos) + 1;     // siguiente (cierra el círculo)
            triangulos[i * 3 + 2] = i + 1;                         // vértice actual
        }

        Mesh malla = new Mesh { name = "DiscoSombra" };
        malla.vertices = vertices;
        malla.uv = uvs;
        malla.triangles = triangulos;
        malla.RecalculateNormals();
        malla.RecalculateBounds();
        return malla;
    }

    private static Texture2D TexturaCircular(int lado)
    {
        Texture2D tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Vector2 c = new Vector2(lado * 0.5f, lado * 0.5f);
        for (int y = 0; y < lado; y++)
            for (int x = 0; x < lado; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) / (lado * 0.5f);
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a); // smooth = bordes suaves
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        return tex;
    }
}