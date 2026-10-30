using System.Collections;
using UnityEngine;

public class FadeUI : MonoBehaviour
{

    public static FadeUI Instancia { get; private set; }

    [Tooltip("CanvasGroup del panel negro a pantalla completa")]
    public CanvasGroup panelOscuro;
    public float duracionFade = 0.7f;

    private Coroutine rutinaActual;

    private void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this;
        if (panelOscuro != null) panelOscuro.alpha = 0f;
    }

    public void FundidoAHaciaNegro()
    {
        if (rutinaActual != null) StopCoroutine(rutinaActual);
        rutinaActual = StartCoroutine(Fundido(1f));
    }

    public void FundidoHaciaClaro()
    {
        if (rutinaActual != null) StopCoroutine(rutinaActual);
        rutinaActual = StartCoroutine(Fundido(0f));
    }

    private IEnumerator Fundido(float alphaFinal)
    {
        float inicio = panelOscuro.alpha;
        float t = 0f;
        while (t < duracionFade)
        {
            t += Time.deltaTime;
            panelOscuro.alpha = Mathf.Lerp(inicio, alphaFinal, t / duracionFade);
            yield return null;
        }
        panelOscuro.alpha = alphaFinal;
    }
}
