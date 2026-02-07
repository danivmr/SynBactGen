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

        [Header("Transform Samplers")]
        public UniformSampler scale = new UniformSampler(0.2f, 0.8f);
        public UniformSampler rotationSampler = new UniformSampler(0f, 360f);

        [Header("Color random Settings")]
        public bool enableColorHeritage = true;
        public UniformSampler hueSampler = new UniformSampler(0f, 1f);
        public UniformSampler saturationSampler = new UniformSampler(0f, 1f);
        public UniformSampler valueSampler = new UniformSampler(0f, 1f);

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

                // If no compatible prefabs found, skip this sample
                if (!m_PrefabsByLabel.TryGetValue(chainLabel, out var compatiblePrefabs) ||
                    compatiblePrefabs.Count == 0)
                    continue;

                // Sample color once per chain if color heritage is enabled
                Color chainColor = Color.white;
                if (enableColorHeritage)
                {
                    float hue = hueSampler.Sample();
                    float saturation = saturationSampler.Sample();
                    float value = valueSampler.Sample();
                    chainColor = Color.HSVToRGB(hue, saturation, value);
                    chainColor.a = 1f;
                }

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
                    if (renderer != null)
                    {
                        if (enableColorHeritage)
                        {
                            renderer.material.color = chainColor;
                        }
                        else
                        {
                            // Sample a unique color for each object when heritage is disabled
                            float hue = hueSampler.Sample();
                            float saturation = saturationSampler.Sample();
                            float value = valueSampler.Sample();
                            Color objectColor = Color.HSVToRGB(hue, saturation, value);
                            objectColor.a = 1f;
                            renderer.material.color = objectColor;
                        }
                    }

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
