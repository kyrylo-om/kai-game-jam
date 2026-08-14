using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileMaster : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var hueta = GetComponent<Tilemap>();
        var x = hueta.GetTile(new Vector3Int(5, 0, 0));
        x = x.CloneViaFakeSerialization() as TileBase;
        Debug.Log(x);
        //fill 0-5 square
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                hueta.SetTile(new Vector3Int(i, j, 0), x);
            }
        }
        //update
        hueta.RefreshAllTiles();
    }
}
