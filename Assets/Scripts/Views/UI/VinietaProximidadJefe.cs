using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class VinietaProximidadJefe : MonoBehaviour
{
    [Header("Ajustes de Tensión")]
    public float distanciaPeligro = 8.5f;
    public Color colorPeligro = new Color(0.85f, 0f, 0f, 0.45f);
    private Image imagenVinieta;
    private Transform jugador;
    private EnemigoIA jefe;
    private GameController gc;
    private void Awake()
    {
        imagenVinieta = GetComponent<Image>();
        imagenVinieta.color = Color.clear;
    }
    private void Start()
    {
        gc = FindAnyObjectByType<GameController>();
        jefe = FindAnyObjectByType<EnemigoIA>();
        GameObject objPlayer = GameObject.FindGameObjectWithTag("Player");
        if (objPlayer != null) jugador = objPlayer.transform;
    }
    private void Update()
    {
        // Si el jugador está escondido en un casillero, a salvo o el jefe no existe: calma total
        if (jefe == null || jugador == null || (gc != null && (gc.jugadorEscondido || gc.jugadorEstaASalvo)))
        {
            imagenVinieta.color = Color.Lerp(imagenVinieta.color, Color.clear, Time.deltaTime * 5f);
            return;
        }
        float distancia = Vector3.Distance(jugador.position, jefe.transform.position);
        if (distancia < distanciaPeligro)
        {
            // Latido más rápido y marcado cuanto más cerca está
            float factorCercania = 1f - (distancia / distanciaPeligro);
            float latido = (Mathf.Sin(Time.time * (6f + factorCercania * 6f)) + 1f) * 0.5f;
            Color objetivo = colorPeligro;
            objetivo.a = colorPeligro.a * factorCercania * (0.5f + latido * 0.5f);
            imagenVinieta.color = objetivo;
        }
        else
        {
            imagenVinieta.color = Color.Lerp(imagenVinieta.color, Color.clear, Time.deltaTime * 3f);
        }
    }
}
