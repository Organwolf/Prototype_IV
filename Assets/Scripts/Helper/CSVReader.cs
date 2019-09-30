using System.Collections.Generic;
using UnityEngine;
using System.Collections;

// this code is no longer compatable with ReadFromCSV
// check older git commits if needed.

public class CSVReader : MonoBehaviour
{
    [SerializeField] private List<AronsLocation> listOfPositions = new List<AronsLocation>();

    private AronsLocation currentLocation = new AronsLocation(13.200226, 55.708675, 0);
    private AronsLocation currentlyClosestPoint;

    void Start()
    {
        listOfPositions = ReadAndParseCSV();
        StartCoroutine(StartGPS());

    }

    public void DebugingCSV()
    {
        Debug.Log("Do I reach this method?");
    }

    private void Update()
    {
        CalculateStuff();
    }

    public AronsLocation CalculateElevationAtLocation()
    {
        if(Input.location.status == LocationServiceStatus.Running)
        {
            currentLocation.longitude = Input.location.lastData.longitude;
            currentLocation.latitude = Input.location.lastData.latitude;
            var currentlyClosestPoint = AronsLocation.ClosestCellToCurrentLocation(currentLocation, listOfPositions);
            Debug.Log("Currently closest point altitude: " + currentlyClosestPoint.altitude);
            return currentlyClosestPoint;
        }
        else
        {
            return null;
        }

    }

    public AronsLocation CurrentlyClosestPoint
    {
        get
        {
            return currentlyClosestPoint;
        }
    }

    void CalculateStuff()
    {
        if (Input.location.status == LocationServiceStatus.Running)
        {
            currentLocation.longitude = Input.location.lastData.longitude;
            currentLocation.latitude = Input.location.lastData.latitude;

            currentlyClosestPoint = AronsLocation.ClosestCellToCurrentLocation(currentLocation, listOfPositions);

            // Debug information of Elevation, Longitude & Latitude
            //Debug.Log("Elevation: " + currentlyClosestPoint.altitude);
            //Debug.Log("Current long: " + currentLocation.longitude);
            //Debug.Log("Current lat:  " + currentLocation.latitude);

            var shortestDistans = AronsLocation.Distance(currentLocation, currentlyClosestPoint);
            //Debug.Log("Distance to cell:  " + shortestDistans);
        }
        else
        {
            //Debug.Log("LocationServiceStatus != Running");
        }
    }

    public List<AronsLocation> ListOfPositions
    {
        get
        {
            return listOfPositions;
        }
    }

    public List<AronsLocation> ReadAndParseCSV()
    {
        var locations = new List<AronsLocation>();
        TextAsset entireCSV = Resources.Load("csv_altitude") as TextAsset;
        var lines = entireCSV.text.Split('\n');

        foreach(var line in lines)
        {
            var locationString = line.Split(',');

            // If parse from string to double did not succeed set the value to zero
            var longitudeString = locationString[0].Trim();
            double longitude;

            if(double.TryParse(longitudeString, out longitude) == false)
            {
                longitude = 0;
            }

            var latitudeString = locationString[1].Trim();
            double latitude;
            if (double.TryParse(latitudeString, out latitude) == false)
            {
                latitude = 0;
            }

            var altitudeString = locationString[2].Trim();
            double altitude;
            if(double.TryParse(altitudeString, out altitude) == false)
            {
                altitude = 0;
            }

            var locationToAdd = new AronsLocation(longitude, latitude, altitude);
            locations.Add(locationToAdd);
        }
        return locations;
    }

    IEnumerator StartGPS()
    {
        // First, check if user has location service enabled
        if (!Input.location.isEnabledByUser)
            yield break;

        // Start service before querying location
        Input.location.Start(2f,1f);

        // Wait until service initializes
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // Service didn't initialize in 20 seconds
        if (maxWait < 1)
        {
            print("Timed out");
            yield break;
        }

        // Connection has failed
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            print("Unable to determine device location");
            yield break;
        }
        else
        {
            // Access granted and location value could be retrieved
            Debug.Log("GPS up and running");
            print("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);
            yield break;
        } 
    }
}

