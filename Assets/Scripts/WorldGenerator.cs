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
    
    [Header("World")]
    public int width = 200;
    public int height = 200;
    public float percentageBlocks = 0.35f;
    public float noiseScale = 15.0f;
    private int fixedFrameCounter = 0;

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
    
    private Vector3 v3pos;
    
    void Awake()
    {
        SharedInstance = this;
        //PlaceAI();
    }

    void Start()
    {
        pooledObjects = new List<GameObject>();
        GameObject tmp;
        for (int i = 0; i < numAI; i++)
        {
            tmp = Instantiate(AIPrefab);
            tmp.SetActive(false);
            pooledObjects.Add(tmp);
        }
        
        PlaceAI();
    }

    private void FixedUpdate()
    {
        fixedFrameCounter++;

        if (fixedFrameCounter % callFrequency != 0) return;
        
        foreach (GameObject enemy in pooledObjects)
        {
            float distance = Vector3.Distance(player.transform.position, enemy.transform.position);
            if (Mathf.Abs(distance) <= 50f)
            {
                enemy.SetActive(true);
            }
            else
            {
                enemy.SetActive(false);
                enemy.GetComponent<EnemyPawn>().UpdatePosition();
            }
        }
    }

    public GameObject GetPooledObject()
    {
        for (int i = 0; i < numAI; i++)
        {
            if (!pooledObjects[i].activeInHierarchy)
            {
                return pooledObjects[i];
            }
        }

        return null;
    }

    [ContextMenu("Generate New World")]
    public void BuildWorld()
    {
        // Log execution time.
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

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

        // Create obstacles/geometry.
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float sample = 0.0f;
                // NOTE: Add a solid border and generate inside with perlin noise.
                if (x > 0 && y > 0 && x < width - 1 && y < height - 1)
                {
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
