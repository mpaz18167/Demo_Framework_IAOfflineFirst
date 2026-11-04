using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FeedbackDDA : MonoBehaviour
{
    [Header("Pantalla de Destello (UI Image a pantalla completa)")]
    [SerializeField] private Image destelloBorde;
    [Header("Colores de Feedback")]
    public Color colorAlivio = new Color(0f, 0.8f, 1f, 0.35f);    // Celeste cian (Frustración mitigada)
    public Color colorTension = new Color(1f, 0.4f, 0f, 0.35f);   // Naranja intenso (Aburrimiento / reto)
    [Header("Sonidos Opcionales (AudioClips)")]
    public AudioClip sonidoAlivio;
    public AudioClip sonidoTension;
    private AudioSource audioSource;
    private Coroutine rutinaDestello;


    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (destelloBorde != null) destelloBorde.color = Color.clear;
    }
    private void OnEnable()
    {
        GameController.OnDificultadCambiada += ReaccionarACambioDificultad;
    }
    private void OnDisable()
    {
        GameController.OnDificultadCambiada -= ReaccionarACambioDificultad;
    }

    private void ReaccionarACambioDificultad(float factor)
    {
        if (factor < 0.95f)
        {
            // La IA detectó frustración y redujo la dificultad para ayudarte
            LanzarFeedback(colorAlivio, sonidoAlivio);
        }
        else if (factor > 1.15f)
        {
            // La IA detectó aburrimiento e inyectó más desafío
            LanzarFeedback(colorTension, sonidoTension);
        }
    }
    private void LanzarFeedback(Color colorMeta, AudioClip clip)
    {
        if (destelloBorde != null)
        {
            if (rutinaDestello != null) StopCoroutine(rutinaDestello);
            rutinaDestello = StartCoroutine(RutinaDestello(colorMeta));
        }
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    private IEnumerator RutinaDestello(Color objetivo)
    {
        float duracionSubida = 0.15f;
        float duracionBajada = 0.45f;
        float t = 0f;
        // Subida rápida
        while (t < duracionSubida)
        {
            t += Time.deltaTime;
            destelloBorde.color = Color.Lerp(Color.clear, objetivo, t / duracionSubida);
            yield return null;
        }
        // Desvanecimiento suave
        t = 0f;
        while (t < duracionBajada)
        {
            t += Time.deltaTime;
            destelloBorde.color = Color.Lerp(objetivo, Color.clear, t / duracionBajada);
            yield return null;
        }
        destelloBorde.color = Color.clear;
        rutinaDestello = null;
    }
}
