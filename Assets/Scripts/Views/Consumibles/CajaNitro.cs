using UnityEngine;

public class CajaNitro : CajaInteractiva
{
    [SerializeField] private float radioExplosion = 2.5f;

    protected override void RomperCaja(bool esAtaque, bool esPisoton) => Explotar();
    private void OnCollisionEnter(Collision collision)
    {
        if ( collision.gameObject.CompareTag("Player")) 
        {
            Explotar();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Explotar();
        }
    }


    private void Explotar()
    {
        if (fueActivada) return;
        fueActivada = true;

        Collider[] afectados = Physics.OverlapSphere(transform.position, radioExplosion);

        foreach (Collider col in afectados)
        {
            if (col.CompareTag("Player"))
            {
                PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
                if (player != null)
                {
                    player.RecibirDaño(false, false, transform.position);
                }
            }
            else if (col.gameObject != this.gameObject)
            {
                IDamage elemento = col.GetComponent<IDamage>();
                if (elemento != null)
                {
                    //Detona en cadena otras cajas o destruye enemigos
                    elemento.RecibirDaño(true, false, transform.position);
                }
            }
        }
        DesactivarCaja();

    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radioExplosion);

    }
}
