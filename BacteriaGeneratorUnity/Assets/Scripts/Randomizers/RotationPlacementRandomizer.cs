using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Perception.Randomization.Utilities;
using UnityEngine.Scripting.APIUpdating;
using System.Collections.Generic;

namespace UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers
{
    [Serializable]
    [AddRandomizerMenu("Bacterias/Rotation Placement Randomizer")]
    [MovedFrom("UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers")]
    public class RotationPlacementRandomizer : Randomizer
    {
        [Header("Placement Settings")]
        public float depth;
        public float separationDistance = 0.4f;
        public Vector2 placementArea;
        public CategoricalParameter<GameObject> prefabs;

        [Header("Chain Behaviour")]
        public float ArrangementProb = 50f;
        public int maxChainLength = 6;
        public float maxBendAngle = 25f;

        [Header("Transform Samplers")]
        public UniformSampler scale = new UniformSampler(0.2f, 0.8f);
        public UniformSampler rotationSampler = new UniformSampler(0f, 360f);

        GameObject m_Container;
        GameObjectOneWayCache m_GameObjectOneWayCache;

        protected override void OnAwake()
        {
            m_Container = new GameObject("Foreground Objects");
            m_Container.transform.parent = scenario.transform;

            m_GameObjectOneWayCache = new GameObjectOneWayCache(
                m_Container.transform,
                prefabs.categories.Select(e => e.Item1).ToArray(),
                this);
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

            float halfX = placementArea.x * 0.5f;
            float halfY = placementArea.y * 0.5f;

            foreach (var sample in samples)
            {
                Vector3 startPos = new Vector3(sample.x, sample.y, depth);
                Quaternion rotation = Quaternion.Euler(0f, 0f, rotationSampler.Sample());

                int chainLength = UnityEngine.Random.Range(1, maxChainLength + 1);
                Vector3 currentPos = startPos;

                for (int i = 0; i < chainLength; i++)
                {
                    // Obtain a game object instance
                    var instance = m_GameObjectOneWayCache.GetOrInstantiate(prefabs.Sample());

                    // Set the scale of the game object
                    instance.transform.localScale = Vector3.one * scaleSample;

                    // Set the position and rotation of the game object
                    instance.transform.position = currentPos + offset;
                    instance.transform.rotation = rotation;

                    // Obtain the width of the game object
                    float width = instance.GetComponentInChildren<Renderer>().bounds.size.x;

                    // Step forward in the direction the object is facing
                    Vector3 forward = rotation * Vector3.right;
                    Vector3 nextPos = currentPos + forward * width;

                    // Convert to world space BEFORE checking bounds
                    Vector3 nextWorldPos = nextPos + offset;

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
