using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainFragment
{
    public TerrainFragment(Coordinates coordinates, float elevation)
    {
        Coordinates = coordinates;
        Elevation = elevation;
    }

    public Coordinates Coordinates { get; }

    public float Longitude => Coordinates.Longitude;

    public float Latitude => Coordinates.Latitude;

    public float Elevation { get; }

    public override string ToString()
    {
        return $"({Longitude},{Elevation},{Latitude})";
    }
}
