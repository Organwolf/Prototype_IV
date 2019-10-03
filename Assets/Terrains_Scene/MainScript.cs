using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainScript : MonoBehaviour
{
    void Start()
    {
        Terrain currentTerrain = new Terrain("csv_small");
        List<TerrainFragment> fragments = currentTerrain.Fragments;
        foreach(TerrainFragment fragment in fragments)
        {
            Debug.Log(fragment.ToString());
        }
    }
}
