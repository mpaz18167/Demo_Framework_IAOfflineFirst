using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public abstract class CajaInteractiva : MonoBehaviour, IDamage
{
    protected GameController gameController;
    protected bool fueActivada = false;

    protected virtual void Start()
    {
        gameController = FindAnyObjectByType<GameController>();
    }

    public virtual void RecibirDaño(bool esAtaque, bool esPisoton, Vector3 puntoContacto)
    {
        if (fueActivada) return;
        RomperCaja(esAtaque, esPisoton);
    }
    // Método abstracto que debe ser implementado por las clases derivadas, se usa protected para que solo las clases derivadas puedan acceder a él.
    protected abstract void RomperCaja(bool esAtaque, bool esPisoton);

    protected void DesactivarCaja()
    {
        fueActivada = true;
        if (gameController != null)
        {
            gameController.RegistrarObjetoRecolectado(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

}
