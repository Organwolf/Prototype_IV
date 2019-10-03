using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coordinates
{
    public Coordinates(float longitude, float latitude)
    {
        Longitude = longitude;
        Latitude = latitude;
    }

    public float Longitude { get; }

    public float Latitude { get; }

    public bool IsSame(Coordinates coordinate, float tolerance)
    {
        return IsSame(this, coordinate, tolerance);
    }

    public static bool IsSame(Coordinates firstCoordinate, Coordinates secondCoordinate, float tolerance)
    {
        return Mathf.Abs(firstCoordinate.Longitude - secondCoordinate.Longitude) < tolerance && Mathf.Abs(firstCoordinate.Latitude - secondCoordinate.Latitude) < tolerance;
    }

    public const float GenericTolerance = 0.01f;

    public override bool Equals(object obj)
    {
        if (obj is Coordinates coordinate)
        {
            return IsSame(this, coordinate, GenericTolerance);
        }
        return false;
    }

    protected bool Equals(Coordinates other)
    {
        return Longitude.Equals(other.Longitude) && Latitude.Equals(other.Latitude);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Longitude.GetHashCode() * 397) ^ Latitude.GetHashCode();
        }
    }

    public override string ToString()
    {
        return $"({Longitude},{Latitude})";
    }
}
