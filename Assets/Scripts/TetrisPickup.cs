using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TetrisPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    public TetrominoType pieceType = TetrominoType.I;
    public int amount = 1;

    [Header("Floating Animation")]
    public float floatSpeed = 2f;
    public float floatHeight = 0.2f;
    public float rotateSpeed = 45f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;

        // Safety check to ensure the collider is a trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void Update()
    {
        // Simple math to make it hover up and down and slowly spin
        transform.position = startPos + new Vector3(0f, Mathf.Sin(Time.time * floatSpeed) * floatHeight, 0f);
        transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Make sure your player GameObject has the tag "Player" in the Inspector!
        if (collision.CompareTag("Player"))
        {
            // Find the builder and give it the pieces
            GridBuildController builder = Object.FindFirstObjectByType<GridBuildController>();
            if (builder != null)
            {
                builder.AddToInventory(pieceType, amount);

                // Add a sound effect or particle pop here later!
                Debug.Log($"Picked up {amount}x {pieceType} block!");

                Destroy(gameObject);
            }
        }
    }
}
