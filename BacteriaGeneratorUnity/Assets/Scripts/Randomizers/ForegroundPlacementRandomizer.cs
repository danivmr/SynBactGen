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

        [Header("Tetrads Behaviour")]
        public int maxTetradSize = 4;
        public float probabilityOfTetrad = 0.3f;

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

            float scaleSample = scale.Sample();
            
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
                    // With a certain probability, create a tetrad instead of a chain
                    if (UnityEngine.Random.value < probabilityOfTetrad)
                    {
                        int tetradSize = UnityEngine.Random.Range(2, maxTetradSize + 1);
                        
                        // Calculate tetrad positions in a 2x2 grid pattern around the base position
                        // Spacing based on diameter so they're adjacent/touching
                        float spacing = scaleSample * 0.08f;
                        Vector3[] tetradOffsets = new Vector3[]
                        {
                            new Vector3(-spacing, -spacing, 0f),  // Bottom-left
                            new Vector3(spacing, -spacing, 0f),   // Bottom-right
                            new Vector3(-spacing, spacing, 0f),   // Top-left
                            new Vector3(spacing, spacing, 0f)     // Top-right
                        };
                        
                        for (int i = 0; i < tetradSize; i++)
                        {
                            GameObject prefab = firstPrefab;

                            var instance = m_GameObjectOneWayCache.GetOrInstantiate(prefab);

                            // Set the scale of the game object
                            instance.transform.localScale = Vector3.one * scaleSample;

                            // Calculate position relative to base position with geometric spacing
                            Vector3 tetradPos = startPos + tetradOffsets[i];
                            Vector3 worldPos = tetradPos + offset;
                            
                            // Check if position is within placement area bounds
                            if (worldPos.x - scaleSample * 0.5f < areaMin.x || worldPos.x + scaleSample * 0.5f > areaMax.x ||
                                worldPos.y - scaleSample * 0.5f < areaMin.y || worldPos.y + scaleSample * 0.5f > areaMax.y)
                            {
                                break; // Stop tetrad if object goes outside placement area
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

                for (int i = 0; i < chainLength; i++)
                {
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
