using Unity.Cinemachine;
using UnityEngine;

public class CajaNitro : CajaInteractiva
{
    [Header("Ajustes de Explosión")]
    [SerializeField] private float radioExplosion = 2.5f;
    [SerializeField] private GameObject prefabExplosion; // Arrastra aquí el Prefab_ExplosionCartoon

    
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
        if (fueActivada) return; // Candado heredado: evita cadenas infinitas Nitro↔Nitro
        fueActivada = true;

        if (prefabExplosion != null)
            Instantiate(prefabExplosion, transform.position, Quaternion.identity);

        CamaraShake.Sacudir(0.55f, 0.25f);

        foreach (EnemigoIA e in FindObjectsByType<EnemigoIA>(FindObjectsInactive.Exclude))

            e.AlertarPorRuido(transform.position);

        Collider[] afectados = Physics.OverlapSphere(transform.position, radioExplosion);

        foreach (Collider col in afectados)
        {
            EnemigoIA jefe = col.GetComponentInParent<EnemigoIA>();
            if (jefe != null) jefe.Ralentizar();

            if (col.CompareTag("Player"))
            {
                PlayerMovement player = col.GetComponent<PlayerMovement>();
                if (player != null && !player.esInvulnerable)
                    player.RecibirDaño(false, false, transform.position);
            }
            else if (col.gameObject != this.gameObject)
            {
                IDamage elemento = col.GetComponent<IDamage>();
                if (elemento != null)
                    elemento.RecibirDaño(true, false, transform.position);
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
