using System;
using System.Collections.Generic;
using UnityEngine;

public class AronsLocation
{
    public double longitude;
    public double latitude;
    public double altitude;

    public AronsLocation()
    {
    }

    public AronsLocation(double longitude, double latitude, double altitude)
    {
        this.longitude = longitude;
        this.latitude = latitude;
        this.altitude = altitude;
    }

    public static double Distance(AronsLocation location1, AronsLocation location2)
    {
        return Distance(location1.longitude, location2.longitude, location1.latitude, location2.latitude);
    }

    public static double Distance(double long1, double long2, double lat1, double lat2)
    {
        // Calculate distance with Haversine formula
        var R = 6371000; // earths diameter metres
        var latRadian1 = DegreeToRadian(lat1);
        var latRadian2 = DegreeToRadian(lat2);
        var deltaLat = DegreeToRadian(lat2 - lat1);
        var deltaLong = DegreeToRadian(long2 - long1);

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) *
                Math.Sin(deltaLong / 2) * Math.Sin(deltaLong / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        var distance = R * c;

        return distance;
    }

    private static double DegreeToRadian(double angle)
    {
        return Math.PI * angle / 180.0;
    }

    public static AronsLocation ClosestCellToCurrentLocation(AronsLocation currentLocation, List<AronsLocation> locationsToCheck)
    {
        //Debug.Log("locations to check length: " + locationsToCheck.Count);
        var shortestDistans = double.MaxValue;
        var currentlyClosestPoint = new AronsLocation();
        int debugCounter = 0;
        foreach (var point in locationsToCheck)
        {
            var distanceToCurrentPoint = AronsLocation.Distance(currentLocation.longitude, point.longitude,
                                                  currentLocation.latitude, point.latitude);
            //Debug.Log("dist to current point: " + distanceToCurrentPoint);

            if (distanceToCurrentPoint < shortestDistans)
            {
                debugCounter++;
                shortestDistans = distanceToCurrentPoint;
                //Debug.Log("dist to current point: " + shortestDistans);
                currentlyClosestPoint = point;
            }
        }
        //Debug.Log("closest alt: " + currentLocation.altitude);
        //Debug.Log("Debug counter: " + debugCounter);
        return currentlyClosestPoint;
        
    }

    // vad är locationInfo?
    public static AronsLocation FromUnityLocationToAronLocation(LocationInfo location)
    {
        // Just a safety check
        if (Input.location.status != LocationServiceStatus.Running)
        {
            return new AronsLocation();
        }

        return new AronsLocation(location.longitude, location.latitude, location.altitude);
    }
}
