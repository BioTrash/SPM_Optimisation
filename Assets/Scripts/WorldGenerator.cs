using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class WorldGenerator : MonoBehaviour
{
    [Header("World")]
    public int width = 200;
    public int height = 200;
    public float percentageBlocks = 0.35f;
    public float noiseScale = 15.0f;

    [Header("Materials")]
    public Material planeMaterial;
    public Material obstacleMaterial;

    [Header("AI")]
    public GameObject pawnDirectoryInstance;
    public GameObject AIPrefab;
    public int numAI = 500;

    private Vector3 v3pos;

    
    void Awake()
    {
        PlaceAI();
    }

    private void FixedUpdate()
    {
        /*
         *Spawn Enemies only without player view port + a certain distance
         *  - Spawn enemies based on placementChance variable
         *Destroy enemies that are without player view port + a certain distance
         */
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

        int placed = 0;

        float placementChance = numAI / ((1.0f - percentageBlocks) * ((width - 2) * (height - 2)));

        while (placed < numAI)
        {
            float x = Random.Range(1, width);
            float y = Random.Range(0, height);

            Vector3 samplePos = new Vector3(x, AIPrefab.GetComponent<CapsuleCollider>().height * 0.5f, y);
            Vector3 point1 = samplePos + AIPrefab.GetComponent<CapsuleCollider>().height * 0.5f * Vector3.up;
            Vector3 point2 = samplePos - AIPrefab.GetComponent<CapsuleCollider>().height * 0.5f * Vector3.up;
            float radius = AIPrefab.GetComponent<CapsuleCollider>().radius;

            Collider[] colliders = Physics.OverlapCapsule(point1, point1, radius);
            if (colliders.Length == 0)
            {
                Quaternion rotation = Quaternion.AngleAxis(Random.value * 360.0f, Vector3.up);
                GameObject pawn = GameObject.Instantiate(AIPrefab, samplePos, rotation);
                pawn.transform.SetParent(pawnDirectoryInstance.transform);
                placed++;
            }
        }
        
        // Logging 
        stopwatch.Stop();
        Debug.LogFormat("[WorldGenerator::PlaceAI] Execution time: {0}ms", stopwatch.ElapsedMilliseconds);
    }
}
