using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(CapsuleCollider))]
public class EnemyPawn : MonoBehaviour
{
    public float movementSpeed = 5.0f;
    public float rotationSpeed = 4.0f;
    public float sight = 400.0f;
    [Range(3, 21)]
    public int traces = 7;
    public float visionAngle = 70.0f;

    public bool debugDraw = false;
    public Vector3 movementVectorUpdated;
    public WorldGenerator sharedInstance;
    public float areaSize = 1f;

    private void Start()
    {

    }

    void FixedUpdate()
    {
        UpdatePosition();
    }

    public void UpdatePosition()
    {
        movementVectorUpdated = CalculateMovementVector();

        // Apply movement
        transform.forward = Vector3.Lerp(transform.forward, movementVectorUpdated.normalized, Time.deltaTime * rotationSpeed);
        transform.position += transform.forward * movementSpeed * Time.deltaTime;

        //ResolveCollisions();
    }

    public Vector3 CalculateMovementVector()
    {
        Vector3 movementVector = Vector3.zero;

        //Create movement vector based on lidar sight.
        for (int i = 0; i < traces; i++)
        {


            float stepAngle = (visionAngle * 2.0f) / (traces - 1);
            float angle = (90.0f + visionAngle - (i * stepAngle)) * Mathf.Deg2Rad;
            Vector3 direction = transform.TransformDirection(new Vector3(Mathf.Cos(angle), 0.0f, Mathf.Sin(angle)));

            Vector2 enemyPosition = new Vector2(Mathf.Round(transform.position.x), Mathf.Round(transform.position.z));
            
            int startX = Mathf.Max(0, Mathf.FloorToInt(enemyPosition.x - areaSize / 2));
            int startY = Mathf.Max(0, Mathf.FloorToInt(enemyPosition.y - areaSize / 2));
            int endX = Mathf.Min(sharedInstance.width, Mathf.CeilToInt(enemyPosition.x + areaSize / 2));
            int endY = Mathf.Min(sharedInstance.height, Mathf.CeilToInt(enemyPosition.y + areaSize / 2));

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    float sample = Mathf.PerlinNoise((float)x / sharedInstance.width * sharedInstance.noiseScale, (float)y / sharedInstance.height * sharedInstance.noiseScale);

                    if (sample < sharedInstance.percentageBlocks)
                    {
                        //Debug.Log("Direction should change right about now");
                        movementVector = -direction * sight;
                    }
                    else
                    {
                        movementVector += direction * sight;
                    }
                }
            }

            // if (sharedInstance.noiseList.Contains(enemyPosition))
            // {
            //     movementVector = -direction * sight;
            //     Debug.Log("Direction should change right about now");
            // }
            // else
            // {
            //     movementVector += direction * sight;
            //
            // }




            // if (Physics.Raycast(transform.position, direction, out RaycastHit hitInfo, sight))
            // {
            //     movementVector += direction * hitInfo.distance;
            //
            //     if (debugDraw)
            //     {
            //         Debug.DrawLine(transform.position, transform.position + direction * hitInfo.distance);
            //
            //         Vector3 Perp = Vector3.Cross(direction, Vector3.up);
            //         Debug.DrawLine(hitInfo.point + Perp, hitInfo.point - Perp, Color.red);
            //     }
            //
            //     if (hitInfo.transform.GetComponent<Player>())
            //     {
            //         movementVector = direction;
            //         break;
            //     }
            // }
            // else
            // {
            //     movementVector += direction * sight;
            //
            //     if (debugDraw)
            //     {
            //         Debug.DrawLine(transform.position, transform.position + direction * sight);
            //     }
            // }
        }

        return movementVector;
    }

    public void ResolveCollisions()
    {
        List<Collider> colliders = Physics.OverlapCapsule(
            transform.position + GetComponent<CapsuleCollider>().height * 0.5f * Vector3.up,
            transform.position - GetComponent<CapsuleCollider>().height * 0.5f * Vector3.up,
            GetComponent<CapsuleCollider>().radius)
            .Where(c => c.transform != transform)
            .ToList();

        if (colliders.Count > 0)
        {
            if (Physics.ComputePenetration(
                GetComponent<CapsuleCollider>(),
                transform.position,
                transform.rotation,
                colliders[0],
                colliders[0].transform.position,
                colliders[0].transform.rotation,
                out Vector3 direction,
                out float distance))
            {
                transform.position += direction * distance;
            }
        }
    }
}
