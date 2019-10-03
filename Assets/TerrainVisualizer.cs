using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainVisualizer : MonoBehaviour
{
    [SerializeField] private string csvPath;
    [SerializeField] private float elevationMultiplier = 1.0f;
    [SerializeField] private GameObject fragmentPrefab;

    private Terrain terrain;

    // Start is called before the first frame update
    void Start()
    {
        terrain = new Terrain(csvPath);
        List<TerrainFragment> fragments = terrain.Fragments;
        foreach (TerrainFragment fragment in fragments)
        {
            Instantiate(fragmentPrefab, new Vector3(fragment.Longitude, fragment.Elevation * elevationMultiplier, fragment.Latitude), Quaternion.identity);
        }
    }
}
