using UnityEngine;

public class GeneralManager : MonoBehaviour
{
    public static GeneralManager instance;

    public Tiles tilescript;
    public TerrainGenerator terrainGenerator;
    public Player player;
    public EnemyBase[] Enemies;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (instance == null)
            instance = this;

        tilescript = GameObject.FindFirstObjectByType<Tiles>();
        terrainGenerator = GameObject.FindFirstObjectByType<TerrainGenerator>();
        player = GameObject.FindFirstObjectByType<Player>();

        //Invoke("spawnPlayer", .1f);

        EnemyBase[] e = GameObject.FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);

        Enemies = e;

        int LastBotID = 0;
        int LastDogID = 0;

        foreach(EnemyBase enemy in Enemies)
        {
            if (enemy.GetType() == typeof(Enemy2))
            {
                LastBotID++;
                enemy.ID = "Bot"+LastBotID.ToString();
            }
            else if (enemy.GetType() == typeof(EnemyDog))
            {
                LastDogID++;
                enemy.ID = "Dog" + LastDogID.ToString();
            }
        }

        Camera.main.transform.position = player.transform.position + new Vector3(0,15,-10);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void spawnPlayer()
    {
        Vector3 playerSpawnPos = new Vector3(terrainGenerator.xSize / 2, 0, terrainGenerator.zSize / 2);
        player.transform.position = tilescript.GetNodeFromWorldPosition(playerSpawnPos).worldPos;

    }
}
