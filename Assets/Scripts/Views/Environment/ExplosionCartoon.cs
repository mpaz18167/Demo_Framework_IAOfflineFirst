using System.Collections;
using UnityEngine;

public class ExplosionCartoon : MonoBehaviour
{
    [Header("Ajustes de Onda Expansiva")]
    public float tamañoFinal = 4.5f;
    public float duracion = 0.35f;

    [Header("Color (se 'enfriando': amarillo -> naranja -> rojo)")]
    public Color colorInicial = new Color(1f, 0.92f, 0.35f); // amarillo brillante
    public Color colorMedio = new Color(1f, 0.5f, 0.08f);  // naranja
    public Color colorFinal = new Color(0.7f, 0.12f, 0.04f); // rojo oscuro

    [Header("Giro aleatorio")]
    public bool girar = true;
    public float velocidadGiro = 200f; // grados/seg (el signo es aleatorio)

    private Renderer rend;
    private MaterialPropertyBlock mpb;
    private float giro;

    private void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend == null) rend = GetComponentInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();
        giro = girar ? Random.Range(-velocidadGiro, velocidadGiro) : 0f;
        transform.localScale = Vector3.zero;
        StartCoroutine(RutinaExplosion());
    }
    private IEnumerator RutinaExplosion()
    {
        float t = 0f;
        float dur = Mathf.Max(0.1f, duracion);
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dur);

            // 1) Escala: estallido elástico con "pop" (overshoot) y colapso final
            float expand = Mathf.Clamp01(p / 0.85f);
            float escala = tamañoFinal * EaseOutBack(expand);
            if (p > 0.85f)
            {
                float cerrar = (p - 0.85f) / 0.15f;
                escala *= 1f - cerrar * cerrar; // último tramo: se encoge hasta desaparecer
            }
            transform.localScale = Vector3.one * Mathf.Max(0.001f, escala);

            // 2) Color: amarillo -> naranja -> rojo oscuro
            Color c;
            if (p < 0.4f) c = Color.Lerp(colorInicial, colorMedio, p / 0.4f);
            else c = Color.Lerp(colorMedio, colorFinal, (p - 0.4f) / 0.6f);

            if (rend != null)
            {
                rend.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", c);      // URP Lit/Unlit
                mpb.SetColor("_Color", c);          // shaders con _Color
                mpb.SetColor("_EmissionColor", c);  // si el material tiene emisión
                rend.SetPropertyBlock(mpb);
            }

            // 3) Giro para que cada explosión se vea distinta
            if (giro != 0f) transform.Rotate(0f, 0f, giro * Time.deltaTime, Space.Self);

            yield return null;
        }
        Destroy(gameObject);
    }

    // Curva elástica con pequeño rebote (estilo cartoon)
    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float u = x - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }
}
