using System;
using System.Linq;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Perception.Randomization.Utilities;
using UnityEngine.Scripting.APIUpdating;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers
{
    /// <summary>
    /// Creates a 2D layer of of evenly spaced GameObjects from a given list of prefabs
    /// </summary>
    [Serializable]
    [AddRandomizerMenu("Bacterias/Foreground Arrangement Placement Randomizer")]
    [MovedFrom("UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers")]
    public class ForegroundArrangementPlacementRandomizer : Randomizer
    {
        [Tooltip("The Z offset applied to positions of all placed objects.")]
        public float depth;

        [Tooltip("The minimum distance between the centers of the placed objects.")]
        public float separationDistance = 2f;

        [Tooltip("The width and height of the area in which objects will be placed. These should be positive numbers and sufficiently large in relation with the Separation Distance specified.")]
        public Vector2 placementArea;

        [Tooltip("The list of Prefabs to be placed by this Randomizer.")]
        public CategoricalParameter<GameObject> prefabs;

        [Tooltip("The probability for the division of bacteria.")]
        public float ArrangementProb = 50;

        [Tooltip("Reduction of probability division after a division.")]
        public float RedArrangementProb = 20;

        [Tooltip("The range of scale to assign to target objects.")]
        public UniformSampler scale = new UniformSampler(0, 1);

        GameObject m_Container;
        GameObjectOneWayCache m_GameObjectOneWayCache;

        /// <inheritdoc/>
        protected override void OnAwake()
        {
            m_Container = new GameObject("Foreground Objects");
            m_Container.transform.parent = scenario.transform;
            m_GameObjectOneWayCache = new GameObjectOneWayCache(
                m_Container.transform, prefabs.categories.Select(element => element.Item1).ToArray(), this);
        }

        /// <summary>
        /// Generates a foreground layer of objects at the start of each scenario iteration
        /// </summary>

        protected override void OnIterationStart()
        {
            var seed = SamplerState.NextRandomState();
            var placementSamples = PoissonDiskSampling.GenerateSamples(
                placementArea.x, placementArea.y, separationDistance, seed);
            var offset = new Vector3(placementArea.x, placementArea.y, 0f) * -0.5f;
            bool division = false;
            var divisionCounter = 0f;
            var prevPlacementSample = new Vector3(placementSamples[0].x, placementSamples[0].y, depth); 
            var prevAngleDivision = -1f;
            bool makeDivision = false;
            var probOfDivision = new UniformSampler(0, 100);
            var angleSample = new UniformSampler(0, 180);
            var angle = 1000f;
            var newPost = new Vector3(0, 0, 0);
            const float PI = (float)Math.PI;

            List<Vector3> listSamples = new List<Vector3>();
            List<float> listAngles = new List<float>();
            var s = scale.Sample();
            foreach (var sample in placementSamples)
            {
                var instance = m_GameObjectOneWayCache.GetOrInstantiate(prefabs.Sample());
                instance.transform.localScale = new Vector3(s, s, s);
                if (division && (probOfDivision.Sample() < ArrangementProb - (20 * divisionCounter)))
                {
                    makeDivision = true;
                    divisionCounter++;
                }
                else if (listSamples.Count > 0 && probOfDivision.Sample() < 30)
                {
                    makeDivision = true;
                    prevPlacementSample = listSamples[0];
                    listSamples.RemoveAt(0);
                    prevAngleDivision = listAngles[0];
                    listAngles.RemoveAt(0); 
                    divisionCounter = 1;
                }
                if (makeDivision)
                {
                    angle = angleSample.Sample();
                    
                    listAngles.Add(angle);
                  
                    if ((angle < prevAngleDivision - 70 && angle > prevAngleDivision + 70))
                    {
                        angle = -angle;
                    }
                    var width = instance.GetComponentsInChildren<Renderer>()[0].bounds.size.x;
                    prevAngleDivision = Math.Abs(angle);
                    var angleRad = PI * (angle / 180f);

                    newPost = new Vector3(prevPlacementSample.x + width * Mathf.Cos(angleRad), prevPlacementSample.y + width * Mathf.Sin(angleRad), depth);

                    instance.transform.position = newPost + offset;
                    listSamples.Add(newPost);
                    
                    makeDivision = false;
                }

                else
                {
                    prevAngleDivision = 1000f;
                    instance.transform.position = new Vector3(sample.x, sample.y, depth) + offset;
                    prevPlacementSample = new Vector3(sample.x, sample.y, depth);

                    division = true;
                    divisionCounter = 0f;
                }

            }
            listSamples.Clear();
            listAngles.Clear();
            placementSamples.Dispose();

        }
        /// <summary>
        /// Deletes generated foreground objects after each scenario iteration is complete
        /// </summary>
        protected override void OnIterationEnd()
        {
            m_GameObjectOneWayCache.ResetAllObjects();
        }
    }
}

