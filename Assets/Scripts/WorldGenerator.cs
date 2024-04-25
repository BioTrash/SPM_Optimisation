using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;
using Random = UnityEngine.Random;

public class WorldGenerator : MonoBehaviour
{
    public static WorldGenerator SharedInstance;
    public List<GameObject> pooledObjects;
    public List<GameObject> obstaclePool;
    public HashSet<Vector2> noiseList;
    public Dictionary<int, HashSet<GameObject>> chunkPool;
    
    [Header("World")]
    public int width = 200;
    public int height = 200;
    public float percentageBlocks = 0.35f;
    public float noiseScale = 15.0f;
    private int fixedFrameCounter = 0;
    private float sample = 0.0f;
    public int chunkSize = 25;

    // Frequency at which to call the function
    public int callFrequency = 60;

    [Header("Materials")]
    public Material planeMaterial;
    public Material obstacleMaterial;

    [Header("AI")]
    public GameObject pawnDirectoryInstance; 
    public GameObject AIPrefab; //objectToPool
    public int numAI = 500; //amountToPool
    
    public GameObject player;
    private EnemyPawn updatedEnemyPawn;
    private int startChunk;
    
    private Vector3 v3pos;
    
    void Awake()
    {
        noiseList = new HashSet<Vector2>();
        pooledObjects = new List<GameObject>();
        chunkPool = new Dictionary<int, HashSet<GameObject>>();
        SharedInstance = this;
    }

    void Start()
    {
        // Iterate through the obstaclePool and group elements into chunks
        foreach (GameObject obstacle in obstaclePool)
        {
            // Calculate the current chunk index based on obstacle position
            int chunkX = (int)(obstacle.transform.position.x / chunkSize) + 1;
            int chunkZ = (int)(obstacle.transform.position.z / chunkSize) + 1;
            int chunkKey = (chunkZ - 1) * (width / chunkSize) + chunkX;

            // Add obstacle to the corresponding chunk
            if (!chunkPool.ContainsKey(chunkKey))
            {
                chunkPool[chunkKey] = new HashSet<GameObject>();
            }

            chunkPool[chunkKey].Add(obstacle);
        }

        Debug.Log(chunkPool.Count);
        
        GameObject tmp;
        for (int i = 0; i < numAI; i++)
        {
            if (i == 1)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        sample = 0.0f;
                        
                        if (x > 0 && y > 0 && x < width - 1 && y < height - 1)
                        {
                            sample = Mathf.PerlinNoise((float)x / width * noiseScale, (float)y / height * noiseScale);
                        }

                        if (sample < percentageBlocks)
                        {
                            noiseList.Add(new Vector2(x, y));
                            
                        }
                    }
                }
            }
            
            tmp = Instantiate(AIPrefab);
            tmp.SetActive(false);
            tmp.GetComponent<EnemyPawn>().sharedInstance = SharedInstance;
            pooledObjects.Add(tmp);
        }
        
        PlaceAI();

        startChunk = PlayerChunk();
    }

    private void FixedUpdate()
    {
        fixedFrameCounter++;

        if (fixedFrameCounter % callFrequency != 0) return;
        
        foreach (GameObject enemy in pooledObjects)
        {
            float distance = Vector3.Distance(player.transform.position, enemy.transform.position);
            if (Mathf.Abs(distance) <= 50.0f)
            {
                enemy.SetActive(true);
            }
            else
            {
                enemy.SetActive(false);
                enemy.GetComponent<EnemyPawn>().UpdatePosition();
            }
        }
  
        

        if (PlayerChunk() != startChunk)
        {
            HashSet<GameObject> retrievedElements = chunkPool[startChunk];
            HashSet<GameObject> newRetrievedElements = chunkPool[PlayerChunk()];
            
            foreach (GameObject element in retrievedElements)
            {
                element.SetActive(false);
            }  
            
            foreach (GameObject element in newRetrievedElements)
            {
                element.SetActive(true);
            }

            startChunk = PlayerChunk();
        }
    }

    private int PlayerChunk()
    {
        int chunkX = (int)(player.transform.position.x / chunkSize) + 1;
        int chunkZ = (int)(player.transform.position.z / chunkSize) + 1;
        return (chunkZ - 1) * (width / chunkSize) + chunkX;
    }

    [ContextMenu("Generate New World")]
    public void BuildWorld()
    {
        /*
         * Generate the entire map
         * Split the map in 25x25 chunks (make size a public variable)
         * Put all elements in a Map, each chunk is a key (int) while each element is a HashSet<GameObject>
         * SetActive(true) on chunk with the player as well as all directly adjacent chunks, including diagonally
         * Everytime player moves check whether chunk has changed
         * SetActive(false) on all chunks that are no longer directly adjacent to chunk with the player
         */
        
        
        // Log execution time.
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        obstaclePool = new List<GameObject>();
        // NOTE: Clean up previous world so we don't double up.
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        // Create ground plane.

        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        // NOTE: Planes are scaled 10x.
        plane.transform.localScale = new Vector3(width, 10.0f, height) * 0.1f;
        // NOTE: Origo of a plane is in the middle. Move lower left corner to zero.
        plane.transform.position = new Vector3(width * 0.5f - 0.5f, 0.0f, height * 0.5f - 0.5f);
        plane.GetComponent<Renderer>().material = planeMaterial;
        plane.transform.SetParent(transform);

        int completedColumns = 0;

        // Create obstacles/geometry.
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (x >= chunkSize)
                {
                    x += chunkSize * completedColumns;
                    if (y >= height)
                    {
                        y = 0;
                        completedColumns++;
                    }
                }
                sample = 0.0f;
                // NOTE: Add a solid border and generate inside with perlin noise.
                if (x > 0 && y > 0 && x < width - 1 && y < height - 1)
                {
                    // TIP: Detta uträkningen är mindre resurskrävadne än att kolla igenom en kollektion. Försök att göra den här uträkningen på varje fiende men låte den kolla endast position runt om sig, säg 20.0f runt om sig
                    sample = Mathf.PerlinNoise((float)x / width * noiseScale, (float)y / height * noiseScale);
                }

                if (sample < percentageBlocks)
                {
                    // NOTE: Larger noice gives higher blocks.
                    float obstacleHeight = 3.0f - sample * 2.0f;
                    GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obstacle.transform.position = new Vector3(x, obstacleHeight * 0.5f, y);
                    obstacle.transform.localScale = new Vector3(1.0f, obstacleHeight, 1.0f);
                    obstacle.transform.SetParent(transform);
                    obstacle.GetComponent<Renderer>().material = obstacleMaterial;
                    obstacle.SetActive(false);
                    obstaclePool.Add(obstacle);
                }
            }
        }



        // Logging 
        stopwatch.Stop();
        Debug.LogFormat("[WorldGenerator::BuildWorld] Execution time: {0}ms", stopwatch.ElapsedMilliseconds);
        
    }

    void PlaceAI()
    {
        // Log execution time.
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        
        //float placementChance = numAI / ((1.0f - percentageBlocks) * ((width - 2) * (height - 2)));

        foreach(GameObject pawn in pooledObjects)
        {
            /*Vector3 point1 = samplePos + AIPrefab.GetComponent<CapsuleCollider>().height * 0.5f * Vector3.up;
            Vector3 point2 = samplePos - AIPrefab.GetComponent<CapsuleCollider>().height * 0.5f * Vector3.up;
            float radius = AIPrefab.GetComponent<CapsuleCollider>().radius;

            Collider[] colliders = Physics.OverlapCapsule(point1, point1, radius);*/
            
            if (pawn)
            {
                float x = Random.Range(0, width);
                float y = Random.Range(0, height);

                Vector3 samplePos = new Vector3(x, AIPrefab.GetComponent<CapsuleCollider>().height * 0.5f, y);
                Quaternion rotation = Quaternion.AngleAxis(Random.value * 360.0f, Vector3.up);
                
                pawn.transform.SetParent(pawnDirectoryInstance.transform);
                pawn.transform.position = samplePos;
                pawn.transform.rotation = rotation;
                pawn.SetActive(true);
            }
        }
        
        // Logging 
        stopwatch.Stop();
        Debug.LogFormat("[WorldGenerator::PlaceAI] Execution time: {0}ms", stopwatch.ElapsedMilliseconds);
    }
}
