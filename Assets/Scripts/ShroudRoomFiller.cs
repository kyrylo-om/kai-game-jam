using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ShroudRoomFiller : MonoBehaviour
{
    [Header("Growth Settings")]
    public float targetHeight = 10f;
    public float timeToFill = 15f;
    
    [Header("Density")]
    public float particlesPerCubicMeter = 5f;

    [Header("Player Submersion Logic")]
    public Transform player;             // Drag your player here
    public float playerHeadOffset = 1.8f; // How high is the player's head/camera from their feet?
    public float maxSubmergedTime = 5f;   // How many seconds before they die?
    
    private float currentChokeTimer = 0f;

    private ParticleSystem ps;
    private ParticleSystem.ShapeModule shape;
    private ParticleSystem.EmissionModule emission;
    
    private float currentHeight = 0.1f;
    private float boxWidth;
    private float boxDepth;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        shape = ps.shape;
        emission = ps.emission;

        boxWidth = shape.scale.x;
        boxDepth = shape.scale.z;
    }

    void Update()
    {
        HandleGasGrowth();
        CheckPlayerSubmersion();
    }

    private void HandleGasGrowth()
    {
        if (currentHeight < targetHeight)
        {
            currentHeight += (targetHeight / timeToFill) * Time.deltaTime;
            currentHeight = Mathf.Min(currentHeight, targetHeight);

            Vector3 newScale = shape.scale;
            newScale.z = currentHeight;
            shape.scale = newScale;

            Vector3 newPos = shape.position;
            newPos.z = currentHeight / 2f; 
            shape.position = newPos;

            float currentVolume = boxWidth * currentHeight * boxDepth;
            emission.rateOverTime = currentVolume * particlesPerCubicMeter;
        }
    }

    private void CheckPlayerSubmersion()
    {
        if (player == null) return;

        // 1. Calculate where the top of the gas is in World Space
        // (Assuming this GameObject is placed exactly on the floor)
        float gasTopWorldY = transform.position.y + currentHeight;

        // 2. Calculate where the player's head is
        float playerHeadY = player.position.y + playerHeadOffset;

        // 3. Are they submerged?
        if (playerHeadY < gasTopWorldY)
        {
            // Player is drowning in the binary shroud!
            currentChokeTimer += Time.deltaTime;
            
            // Optional: Print a warning so you can test it
            Debug.Log($"Submerged! Time until death: {maxSubmergedTime - currentChokeTimer:F1}s");

            if (currentChokeTimer >= maxSubmergedTime)
            {
                KillPlayer();
            }
        }
        else
        {
            // Player is above the gas line. 
            // In Enshrouded, the timer usually resets or ticks down quickly when you catch your breath.
            if (currentChokeTimer > 0)
            {
                currentChokeTimer -= Time.deltaTime * 2f; // Recovers 2x as fast
                currentChokeTimer = Mathf.Max(currentChokeTimer, 0f);
            }
        }
    }

    private void KillPlayer()
    {
        Debug.Log("PLAYER HAS DIED FROM BINARY GAS!");
        // Hook into your game designer's actual death/respawn script here!
        
        // Example: player.GetComponent<HealthController>().Die();
        
        // Reset timer so it doesn't spam the console
        currentChokeTimer = 0f; 
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return; // Only draw while the game is running

        Gizmos.color = new Color(0f, 0.5f, 1f, 0.5f); // Semi-transparent blue

        // Calculate the center of the current spawn box
        Vector3 center = transform.position;
        center.y += currentHeight / 2f; // Offset by half height

        // Draw the volume
        Gizmos.DrawWireCube(center, new Vector3(boxWidth, currentHeight, boxDepth));
    }
}