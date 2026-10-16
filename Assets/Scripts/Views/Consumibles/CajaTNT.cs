using System.Collections;
using UnityEngine;

public class CajaTNT : CajaInteractiva
{
    [SerializeField] private float tiempoDetonacion = 3.0f;
    [SerializeField] private float radioExplosion = 3.5f;

    protected override void RomperCaja(bool esAtaque, bool esPisoton)
    {
        if (esAtaque) DetonarInmediato();
        else if (esPisoton) StartCoroutine(CuentaRegresiva());

    }

    private IEnumerator CuentaRegresiva()
    {
        fueActivada = true;
        yield return new WaitForSeconds(tiempoDetonacion);
        DetonarInmediato();

    }

    private void DetonarInmediato()
    {
        Collider[] afectados = Physics.OverlapSphere(transform.position, radioExplosion);
        foreach (Collider col in afectados)
        {
            if (col.CompareTag("Player"))
            {
                PlayerMovement player = col.GetComponent<PlayerMovement>();
                if (player != null && !player.esInvulnerable && gameController != null)
                {
                    gameController.IniciarMuerte();
                }
            }
            else
            {
                IDamage obj = col.GetComponent<IDamage>();
                if (obj != null && col.gameObject != this.gameObject)
                {
                    obj.RecibirDaño(true, false, transform.position);
                }
            }
            
        }
        DesactivarCaja();
    }
}
