using System.Collections;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instancia { get; private set; }
    [Header("Pistas de Música")]
    [Tooltip("Arrastra aquí tu canción principal de fondo")]
    public AudioClip musicaTranquila;
    [Tooltip("Opcional: música rápida cuando el enemigo te persigue")]
    public AudioClip musicaTension;
    [Header("Ajustes")]
    [Range(0f, 1f)] public float volumenGeneral = 0.5f;
    public float velocidadTransicion = 1.5f;
    private AudioSource audioSource;
    private Coroutine rutinaCambio;
    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;
        DontDestroyOnLoad(gameObject);
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D: suena en toda la pantalla
        audioSource.volume = volumenGeneral;
    }
    private void Start()
    {
        if (musicaTranquila != null)
        {
            ReproducirPista(musicaTranquila);
        }
        else
        {
            Debug.LogWarning("[MusicaManager] No has asignado el AudioClip de musicaTranquila en el Inspector.");
        }
    }
    public void ReproducirPista(AudioClip nuevaPista)
    {
        if (nuevaPista == null || audioSource.clip == nuevaPista) return;
        if (rutinaCambio != null) StopCoroutine(rutinaCambio);
        rutinaCambio = StartCoroutine(TransicionPista(nuevaPista));
    }
    private IEnumerator TransicionPista(AudioClip nuevaPista)
    {
        // Fade out
        while (audioSource.volume > 0.05f)
        {
            audioSource.volume -= Time.deltaTime * velocidadTransicion;
            yield return null;
        }
        audioSource.clip = nuevaPista;
        audioSource.Play();
        // Fade in
        while (audioSource.volume < volumenGeneral)
        {
            audioSource.volume += Time.deltaTime * velocidadTransicion;
            yield return null;
        }
        audioSource.volume = volumenGeneral;
        rutinaCambio = null;
    }
}
