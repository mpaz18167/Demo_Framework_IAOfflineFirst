using UnityEngine;

public class CajaMetalica : CajaInteractiva
{
    [Header("Plataformeo Indestructible")]
    [SerializeField] private bool esTrampolinMetalico = false;
    [SerializeField] private float impulsoTrampolin = 20f;

    protected override void RomperCaja(bool esAtaque, bool esPisoton)
    {
        if (esPisoton)
        {
            if (esTrampolinMetalico)
            {
                PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
                if (player != null) player.AplicarImpulso(impulsoTrampolin);
            }

            // Si es hierro normal y no es trampolín, no hacemos nada. 
            // PlayerMovement.cs ya le aplicó la fuerza de 9f por defecto.
        }
        // NUNCA llamamos a DesactivarCaja(). Es indestructible.
        if (esAtaque || !esPisoton)
        {
            Debug.Log("[Caja Metálica] ¡Clank! Sonido metálico, inquebrantable.");
            // Aquí podrías reproducir un sonido metálico o una animación de impacto, pero no se destruye.
        }
    }

}
