using UnityEngine;
using UnityEngine.UIElements;

public class Loot_Spawner_Behaviour : MonoBehaviour
{
    private pool_generic PG;
    public Texture2D Map_Data_Cache;
    public float scale = 1;
    public int cell_count = 10;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PG = transform.GetComponent<pool_generic>();
        Random.InitState(200);
        GenerateLootMap(50);
        GenerateLoot(Map_Data_Cache);
    }

    void GenerateLootMap(int length)
    {
        Texture2D Map = new Texture2D(length, length);
        for (int i = 0; i < length; i++)
        {
            for (int j = 0; j < length; j++)
            {
                Map.SetPixel(j, i, Color.black);
            }
        }
        Map.Apply();


        for (int i = 0; i < cell_count; i++)
        {
            int xCoord = Random.Range(0, length + 1);
            int yCoord = Random.Range(0, length + 1);

            Map.SetPixel(xCoord, yCoord, Color.white);
        }
        Map.Apply();
        //transform.GetComponent<Renderer>().material.mainTexture = Map;
        Map_Data_Cache = Map;
    }
    
    void GenerateLoot(Texture2D lootMap)
    {
        int offset = lootMap.width/2;
        for (int i = 0; i < lootMap.height; i++)
        {
            for (int j = 0; j < lootMap.width; j++)
            {
                if(lootMap.GetPixel(j, i) == Color.white)
                {
                    int count = Random.Range(3, 6);
                    Vector3 spawn_pos = new Vector3(
                        (transform.position.x + j - offset) * scale,
                        8f,
                        (transform.position.z + i - offset) * scale);
                    Vector3 offset_vector = Vector3.zero;
                    offset_vector.x = Random.Range(2, 4);
                    offset_vector.z = Random.Range(2, 4);
                    for(int k=0; k<count; k++)
                    {
                        GameObject new_loot_instance = PG.CreateItem();
                        PG.OnGet(new_loot_instance);
                        new_loot_instance.transform.position = spawn_pos + offset_vector * k;

                        offset_vector.x = Random.Range(2, 4);
                        offset_vector.z = Random.Range(2, 4);
                    }
                }
            }
        }
    }
}
