using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Terrain
{
    public Terrain(List<TerrainFragment> fragments)
    {
        Fragments = fragments;
    }

    public Terrain(string pathToCsvFile)
    {
        Fragments = new List<TerrainFragment>();
        TextAsset entireCSV = Resources.Load(pathToCsvFile) as TextAsset;
        var lines = entireCSV.text.Split('\n');

        foreach (var line in lines)
        {
            var locationString = line.Split(',');

            // If parse from string to double did not succeed set the value to zero
            var longitudeString = locationString[0].Trim();
            float longitude;

            if (float.TryParse(longitudeString, out longitude) == false)
            {
                longitude = 0;
            }

            var latitudeString = locationString[1].Trim();
            float latitude;
            if (float.TryParse(latitudeString, out latitude) == false)
            {
                latitude = 0;
            }

            var altitudeString = locationString[2].Trim();
            float altitude;
            if (float.TryParse(altitudeString, out altitude) == false)
            {
                altitude = 0;
            }

            var coordinates = new Coordinates(longitude, latitude);
            var locationToAdd = new TerrainFragment(coordinates, altitude);
            Fragments.Add(locationToAdd);
        }
    }

    public List<TerrainFragment> Fragments { get; }

    public TerrainFragment GetFragment(Coordinates coordinates, float tolerance = 0.01f)
    {
        return GetFragment(coordinates.Longitude, coordinates.Latitude, tolerance);
    }

    public TerrainFragment GetFragment(float longitude, float latitude, float tolerance = 0.01f)
    {
        var fragment = Fragments.FirstOrDefault(terrainFragment =>
            Mathf.Abs(terrainFragment.Coordinates.Longitude - longitude) < tolerance &&
            Mathf.Abs(terrainFragment.Coordinates.Latitude - latitude) < tolerance);

        return fragment;
    }
}

