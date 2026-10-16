using Firebase;
using Firebase.Extensions;
using System;
using UnityEngine;

public class FirebaseInit : MonoBehaviour
{

    public static event Action OnFirebaseInitialized; // Evento para notificar cuando Firebase se ha inicializado correctamente

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("[FirebaseInit] Intentando conectar Firebase...");

        // Verificar y corregir dependencias de Firebase
        // Continuar en el hilo principal para evitar problemas de 
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                FirebaseApp app = FirebaseApp.DefaultInstance;// Inicializar 

                OnFirebaseInitialized?.Invoke();// Disparar el evento para notificar que Firebase se ha inicializado correctamente

                Debug.Log("[FirebaseInit] Firebase conectado correctamente.");
                // Aquí se puede inicializar cualquier servicio de Firebase extra que se necesite
            }
            else
            {
                Debug.LogError($"[FirebaseInit] No se pudo conectar a Firebase: {dependencyStatus}");
            }
        });

    }
}
