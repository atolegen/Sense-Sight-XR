using UnityEngine;
using Convai.Scripts.Runtime.Core;

public class ConvaiProximityTrigger : MonoBehaviour
{
    public float interactionDistance = 2.0f; // Distance to trigger
    private ConvaiNPC npc;
    private bool hasStarted = false; // Prevent repeated starts

    void Start()
    {
        npc = GetComponent<ConvaiNPC>();
        if (npc == null)
        {
            Debug.LogError("ConvaiNPC component not found on this GameObject!");
        }
    }

    void Update()
    {
        // Check distance to main camera (HoloLens user)
        if (Camera.main != null && npc != null && !hasStarted)
        {
            float distance = Vector3.Distance(transform.position, Camera.main.transform.position);
            if (distance <= interactionDistance)
            {
                npc.SendTextDataAsync("Hello, I'm here to help with your virtual markers!");
                hasStarted = true; // Only start once
            }
        }
    }
}