using UnityEngine;

public interface IDamage
{
    void RecibirDaño(bool esAtaque, bool esPisoton, Vector3 puntoContacto);
}