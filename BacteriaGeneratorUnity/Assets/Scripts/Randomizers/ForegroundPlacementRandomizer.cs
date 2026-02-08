using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Perception.Randomization.Utilities;
using UnityEngine.Scripting.APIUpdating;
using System.Collections.Generic;
using UnityEngine.Perception.GroundTruth.LabelManagement;

namespace UnityEngine.Perception.Randomization.Randomizers.SynBactGen
{
    [Serializable]
    [AddRandomizerMenu("SynBactGen/Foreground Placement Randomizer")]
    [MovedFrom("UnityEngine.Perception.Randomization.Randomizers.SynBactGen")]
    public class ForegroundPlacementRandomizer : Randomizer
    {
        [Header("Placement Settings")]
        public float depth;
        public float separationDistance = 0.4f;
        public Vector2 placementArea;
        public CategoricalParameter<GameObject> prefabs;

        [Header("Chain Behaviour")]
        public int maxChainLength = 6;
        public float maxBendAngle = 25f;
        public float probabilityOfChain = 0.7f;

        [Header("Grape Cluster Behaviour")]
        public int maxClusterSize = 4;
        public float probabilityOfCluster = 0.3f;

        [Header("Transform Samplers")]
        public UniformSampler scale = new UniformSampler(0.2f, 0.8f);
        public UniformSampler rotationSampler = new UniformSampler(0f, 360f);

        GameObject m_Container;
        GameObjectOneWayCache m_GameObjectOneWayCache;

        Dictionary<string, List<GameObject>> m_PrefabsByLabel;

        protected override void OnAwake()
        {
            m_Container = new GameObject("Foreground Objects");
            m_Container.transform.parent = scenario.transform;

            var prefabArray = prefabs.categories.Select(e => e.Item1).ToArray();

            m_GameObjectOneWayCache = new GameObjectOneWayCache(
                m_Container.transform,
                prefabArray,
                this);

            // Make a dictionary of prefabs by their labels for chain compatibility
            m_PrefabsByLabel = new Dictionary<string, List<GameObject>>();

            foreach (var prefab in prefabArray)
            {
                if (prefab.TryGetComponent<Labeling>(out var labeling))
                {
                    foreach (var label in labeling.labels)
                    {
                        if (!m_PrefabsByLabel.TryGetValue(label, out var list))
                        {
                            list = new List<GameObject>();
                            m_PrefabsByLabel[label] = list;
                        }
                        list.Add(prefab);
                    }
                }
            }
        }

        protected override void OnIterationStart()
        {
            var seed = SamplerState.NextRandomState();
            var samples = PoissonDiskSampling.GenerateSamples(
                placementArea.x, placementArea.y, separationDistance, seed);

            // Center the placement area around the origin
            var offset = new Vector3(placementArea.x, placementArea.y, 0f) * -0.5f;
            var bendSampler = new UniformSampler(-maxBendAngle, maxBendAngle);

            float chainProbabilitySample = UnityEngine.Random.value;
            // Calculate placement area bounds
            Vector3 areaMin = offset;
            Vector3 areaMax = offset + new Vector3(placementArea.x, placementArea.y, 0f);

            foreach (var sample in samples)
            {
                Vector3 startPos = new Vector3(sample.x, sample.y, depth);
                Quaternion rotation = Quaternion.Euler(0f, 0f, rotationSampler.Sample());

                int chainLength = UnityEngine.Random.Range(1, maxChainLength + 1);
                Vector3 currentPos = startPos;

                GameObject firstPrefab = prefabs.Sample();

                // Ensure the first prefab has labeling and compatible prefabs exist
                if (!firstPrefab.TryGetComponent<Labeling>(out var firstLabeling) ||
                    firstLabeling.labels.Count == 0)
                    continue;

                // Use the first label to determine compatible prefabs for the chain
                string chainLabel = firstLabeling.labels[0];

                if(chainLabel == "sphere")
                {
                    // With a certain probability, create a grape cluster instead of a chain
                    if (UnityEngine.Random.value < probabilityOfCluster)
                    {
                        int clusterSize = UnityEngine.Random.Range(2, maxClusterSize + 1);
                        float scaleSample = scale.Sample();
                        
                        // Get the actual bounds of the sphere at the given scale
                        var testInstance = m_GameObjectOneWayCache.GetOrInstantiate(firstPrefab);
                        testInstance.transform.localScale = Vector3.one * scaleSample;
                        float sphereDiameter = testInstance.GetComponentInChildren<Renderer>().bounds.size.x;
                        m_GameObjectOneWayCache.ResetObject(testInstance);
                        
                        float maxRadius = sphereDiameter * 3.0f; // Maximum radius for cluster spread
                        
                        List<Vector3> clusterPositions = new List<Vector3>();
                        clusterPositions.Add(startPos); // Add center sphere first
                        
                        for (int i = 1; i < clusterSize; i++)
                        {
                            Vector3 newPos = Vector3.zero;
                            bool validPosition = false;
                            int attempts = 0;
                            
                            // Try to find a position that doesn't overlap with existing spheres
                            while (!validPosition && attempts < 20)
                            {
                                float randomRadius = UnityEngine.Random.Range(sphereDiameter * 0.8f, maxRadius);
                                float randomAngle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                                
                                Vector3 clusterOffset = new Vector3(
                                    Mathf.Cos(randomAngle) * randomRadius,
                                    Mathf.Sin(randomAngle) * randomRadius,
                                    0f
                                );
                                
                                newPos = startPos + clusterOffset;
                                
                                // Check if this position doesn't overlap significantly with existing positions (allow touching)
                                validPosition = true;
                                foreach (var existingPos in clusterPositions)
                                {
                                    float distance = Vector3.Distance(newPos, existingPos);
                                    if (distance < sphereDiameter * 0.95f) // Allow touching (95% diameter apart)
                                    {
                                        validPosition = false;
                                        break;
                                    }
                                }
                                
                                attempts++;
                            }
                            
                            if (validPosition)
                            {
                                clusterPositions.Add(newPos);
                            }
                        }
                        
                        // Now place all the spheres
                        foreach (var clusterPos in clusterPositions)
                        {
                            GameObject prefab = firstPrefab;
                            var instance = m_GameObjectOneWayCache.GetOrInstantiate(prefab);
                            instance.transform.localScale = Vector3.one * scaleSample;
                            
                            Vector3 worldPos = clusterPos + offset;
                            
                            // Check if position is within placement area bounds
                            if (worldPos.x - sphereDiameter * 0.5f < areaMin.x || worldPos.x + sphereDiameter * 0.5f > areaMax.x ||
                                worldPos.y - sphereDiameter * 0.5f < areaMin.y || worldPos.y + sphereDiameter * 0.5f > areaMax.y)
                            {
                                continue;
                            }
                            
                            instance.transform.position = worldPos;
                            instance.transform.rotation = rotation;
                        }
                        continue; // Skip chain generation for this sample
                    }
                   
                }
                else if(chainLabel == "spiral")
                {
                    // Spirals are placed individually without arrangement
                    float scaleSample = scale.Sample();
                    GameObject prefab = firstPrefab;
                    var instance = m_GameObjectOneWayCache.GetOrInstantiate(prefab);
                    instance.transform.localScale = Vector3.one * scaleSample;
                    
                    Vector3 worldPos = startPos + offset;
                    
                    // Check if position is within placement area bounds
                    if (worldPos.x - scaleSample * 0.5f >= areaMin.x && worldPos.x + scaleSample * 0.5f <= areaMax.x &&
                        worldPos.y - scaleSample * 0.5f >= areaMin.y && worldPos.y + scaleSample * 0.5f <= areaMax.y)
                    {
                        instance.transform.position = worldPos;
                        instance.transform.rotation = rotation;
                    }
                    continue; // Skip chain generation for this sample
                }

                // If no compatible prefabs found, skip this sample
                if (!m_PrefabsByLabel.TryGetValue(chainLabel, out var compatiblePrefabs) ||
                    compatiblePrefabs.Count == 0)
                    continue;

                // Check chain probability
                if (chainProbabilitySample >= probabilityOfChain)
                {
                    // Place a single object instead of skipping
                    float scaleSample = scale.Sample();
                    GameObject prefab = compatiblePrefabs[
                        UnityEngine.Random.Range(0, compatiblePrefabs.Count)
                    ];

                    var instance = m_GameObjectOneWayCache.GetOrInstantiate(prefab);
                    instance.transform.localScale = Vector3.one * scaleSample;

                    Vector3 worldPos = startPos + offset;

                    // Check if position is within placement area bounds
                    if (worldPos.x - scaleSample * 0.5f >= areaMin.x && worldPos.x + scaleSample * 0.5f <= areaMax.x &&
                        worldPos.y - scaleSample * 0.5f >= areaMin.y && worldPos.y + scaleSample * 0.5f <= areaMax.y)
                    {
                        instance.transform.position = worldPos;
                        instance.transform.rotation = rotation;
                    }
                    continue;
                }

                for (int i = 0; i < chainLength; i++)
                {
                    float scaleSample = scale.Sample();
                    // Randomly select a prefab from the compatible prefabs
                    GameObject prefab = compatiblePrefabs[
                        UnityEngine.Random.Range(0, compatiblePrefabs.Count)
                    ];

                    var instance = m_GameObjectOneWayCache.GetOrInstantiate(prefab);

                    // Set the scale of the game object
                    instance.transform.localScale = Vector3.one * scaleSample;

                    // Apply randomized color
                    var renderer = instance.GetComponentInChildren<Renderer>();

                    // Set the position and rotation of the game object
                    Vector3 worldPos = currentPos + offset;
                    
                    // Check if position is within placement area bounds
                    if (worldPos.x - scaleSample * 0.5f < areaMin.x || worldPos.x + scaleSample * 0.5f > areaMax.x ||
                        worldPos.y - scaleSample * 0.5f < areaMin.y || worldPos.y + scaleSample * 0.5f > areaMax.y)
                    {
                        break; // Stop chain if object goes outside placement area
                    }
                    
                    instance.transform.position = worldPos;
                    instance.transform.rotation = rotation;

                    // Obtain the width of the game object
                    float width = instance.GetComponentInChildren<Renderer>().bounds.size.x;

                    // Step forward in the direction the object is facing
                    Vector3 forward = rotation * Vector3.right;
                    Vector3 nextPos = currentPos + forward * width;

                    currentPos = nextPos;
                    // Apply a slight bend for the next object in the chain
                    rotation *= Quaternion.Euler(0f, 0f, bendSampler.Sample());
                }
            }

            samples.Dispose();
        }

        protected override void OnIterationEnd()
        {
            m_GameObjectOneWayCache.ResetAllObjects();
        }
    }
}
