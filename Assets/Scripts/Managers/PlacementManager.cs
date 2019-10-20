/**
 *  Snap functionality for start and end points 2/10 -19
 *
 *
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.ProBuilder;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif

public class PlacementManager : MonoBehaviour
{
    // Prefabs & materials
    public GameObject groundPlanePrefab;
    public GameObject clickPointPrefab;
    public Material[] materialForWalls;
    public Material waterMaterial;

    // startPoint & endPoint
    private GameObject startPoint;
    private GameObject endPoint;
    private LineRenderer measureLine;

    // Plane, water & wall variables
    private bool planeIsPlaced;
    private float height = 4.0f;    // changed from 4 to 0.5 for debugging
    private GameObject groundPlane;
    private bool waterIsPlaced;
    private bool waterIsVisible;
    private bool wallPlacementEnabled;
    private bool toggleVisibilityOfWalls;
    private List<GameObject> listOfLinerenderers;

    // Csv variables
    private CSVReader csvReader;

    // Line renderer
    [SerializeField] GameObject lineRendererPrefab;

    // AR
    [SerializeField] ARSession arSession;
    [SerializeField] ARRaycastManager arRaycastManager;
    [SerializeField] ARPlaneManager arPlaneManager;
    [SerializeField] Camera arCamera;

    // UI
    [SerializeField] Slider multiplyElevationSlider;
    [SerializeField] Slider adjustRadiusSlider;
    [SerializeField] Button resetSessionButton;
    [SerializeField] Button renderWaterButton;
    [SerializeField] Button placeWallsButton;
    [SerializeField] GameObject sliderPanel;
    [SerializeField] GameObject radiusTextVisibility;
    [SerializeField] GameObject multiplierTextVisibility;

    // Raycasts
    private List<ARRaycastHit> hitsAR = new List<ARRaycastHit>();
    private RaycastHit hits;
    private bool HasSavedPoint;
    private Vector3 savedPoint;
    private List<GameObject> listOfPlacedObjects;
    private int groundLayerMask = 1 << 8;

    // ProBuilder variables
    private GameObject waterGameObject;
    private int pipeSubdivAxis = 10;
    private int pipeSubdivHeight = 4;
    private float holeSize = 0.5f;
    private float pipeHeight = 0.1f;
    private List<GameObject> listOfWallMeshes;

    // ProBuilder - UI slider values
    private float minimumRadius = 2.0f;
    private float maximumRaidus = 50f;
    private int maxMultiplyElevation = 10;
    private float radius = 2.0f;
    private textOverlayMultSlider multiplierText;
    private textOverlayRadiusSlider radiusText;

    // Elevation variables
    private float elevation = 0.001f;

    private void Awake()
    {
        // Lists for wall objects
        listOfPlacedObjects = new List<GameObject>();
        listOfWallMeshes = new List<GameObject>();
        listOfLinerenderers = new List<GameObject>();

        // startPoint & endPoint
        startPoint = Instantiate(clickPointPrefab, Vector3.zero, Quaternion.identity);
        endPoint = Instantiate(clickPointPrefab, Vector3.zero, Quaternion.identity);
        startPoint.SetActive(false);
        endPoint.SetActive(false);
        measureLine = GetComponent<LineRenderer>();
        measureLine.enabled = false;

        // UI
        multiplierText = multiplyElevationSlider.GetComponent<textOverlayMultSlider>();
        radiusText = adjustRadiusSlider.GetComponent<textOverlayRadiusSlider>();
        renderWaterButton.interactable = false;
        placeWallsButton.interactable = false;
    }

    private void Start()
    {
        #if PLATFORM_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
        }
        #endif

        // Assuming that the GPS is active
        csvReader = GetComponent<CSVReader>();
    }

    private void Update()
    {
        if(planeIsPlaced && waterIsVisible)
            GenerateWater();

        if (!EventSystem.current.IsPointerOverGameObject(0))
        {
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);

                if (TouchPhase.Began == touch.phase)
                {
                    // Placement of the transparent plane used to raycast against
                    if (!planeIsPlaced)
                    {
                        if (arRaycastManager.Raycast(touch.position, hitsAR, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon))
                        {
                            Debug.Log("Placed the plane");
                            var hitPose = hitsAR[0].pose;
                            groundPlane = Instantiate(groundPlanePrefab, hitPose.position, hitPose.rotation);
                            planeIsPlaced = true;
                            placeWallsButton.interactable = true;
                            TogglePlaneDetection();
                        }
                    }

                    else if (planeIsPlaced && wallPlacementEnabled)
                    {
                        Ray ray = arCamera.ScreenPointToRay(touch.position);
                        RaycastHit hitInfo;

                        if (Physics.Raycast(ray, out hitInfo, groundLayerMask))
                        {
                            startPoint.SetActive(true);
                            startPoint.transform.SetPositionAndRotation(hitInfo.point, Quaternion.identity);

                            // Snapping startPoint
                            if (listOfPlacedObjects != null)
                            {
                                foreach (var point in listOfPlacedObjects)
                                {
                                    if (point.transform.position != startPoint.transform.position)
                                    {
                                        float dist = Vector3.Distance(point.transform.position, startPoint.transform.position);
                                        if (dist < 0.1)
                                        {
                                            startPoint.transform.position = point.transform.position;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                else if (TouchPhase.Moved == touch.phase && wallPlacementEnabled)
                {
                    Ray ray = arCamera.ScreenPointToRay(touch.position);
                    RaycastHit hitInfo;

                    if (Physics.Raycast(ray, out hitInfo, groundLayerMask))
                    {
                        endPoint.SetActive(true);
                        endPoint.transform.SetPositionAndRotation(hitInfo.point, Quaternion.identity);
                    }
                }

                else if (TouchPhase.Ended == touch.phase && wallPlacementEnabled && startPoint.activeSelf && endPoint.activeSelf)
                {
                    // Snapping the endPoint
                    if (listOfPlacedObjects != null)
                    {
                        foreach (var point in listOfPlacedObjects)
                        {
                            if (point.transform.position != endPoint.transform.position)
                            {
                                float dist = Vector3.Distance(point.transform.position, endPoint.transform.position);
                                if (dist < 0.1)
                                {
                                    endPoint.transform.position = point.transform.position;
                                }
                            }
                        }
                    }

                    // De-activates objects/lines smaller than 10 cm
                    if(Vector3.Distance(startPoint.transform.position,endPoint.transform.position) < 0.1f)
                    {
                        startPoint.SetActive(false);
                        endPoint.SetActive(false);
                        measureLine.enabled = false;
                        return;
                    }

					// Create the start and endpoint
					var startPointObject = Instantiate(clickPointPrefab, startPoint.transform.position, Quaternion.identity);
                    var endPointObject = Instantiate(clickPointPrefab, endPoint.transform.position, Quaternion.identity);
                    listOfPlacedObjects.Add(startPointObject);
                    listOfPlacedObjects.Add(endPointObject);
                    
                    // Disable temporary line renderer and create a new one
                    measureLine.enabled = false;
                    DrawLineBetweenTwoPoints(startPoint, endPoint);

                    // Then disable the startPoint and endPoint
                    startPoint.SetActive(false);
                    endPoint.SetActive(false);

                    // Create a wall with the startpoint and endpoint as corner vertices
                    CreateQuadFromPoints(startPointObject.transform.position, endPointObject.transform.position);

                    // A wall has been placed. Now the water can be rendered
                    if (!renderWaterButton.interactable)
                    {
                        renderWaterButton.interactable = true;
                    }
                }
            }
        }

        // Draws a line while placing the endpoint
        if (startPoint.activeSelf && endPoint.activeSelf)
        {
            measureLine.enabled = true;
            measureLine.SetPosition(0, startPoint.transform.position);
            measureLine.SetPosition(1, endPoint.transform.position);
        }
    }

    // Helper functions
    private void DrawLineBetweenTwoPoints(GameObject startPoint, GameObject endPoint)
    {
        var lineRendererGameObject = Instantiate(lineRendererPrefab);
        var lineRenderer = lineRendererGameObject.GetComponent<LineRenderer>();
        lineRenderer.SetPosition(0, startPoint.transform.position);
        lineRenderer.SetPosition(1, endPoint.transform.position);
        listOfLinerenderers.Add(lineRendererGameObject);
    }

    private void GenerateWater()
    {
		if (radius > maximumRaidus)
		{
			radius = maximumRaidus;
		}

		if (waterGameObject != null)
		{
			Destroy(waterGameObject);
		}

		var thickness = radius - holeSize;
		var mesh = ShapeGenerator.GeneratePipe(PivotLocation.Center, radius, pipeHeight, thickness, pipeSubdivAxis, pipeSubdivHeight);
		var meshRenderer = mesh.GetComponent<MeshRenderer>();

        meshRenderer.sharedMaterial = waterMaterial;

		mesh.transform.SetParent(transform, false);
		waterGameObject = mesh.gameObject;

        // adjust the y-value of the gameobject
        // by removing the pipeHeight from the y-axis the wall cutoff looks right from the get go
        // DEBUG: remove elevation
        var pos = new Vector3(arCamera.transform.position.x, groundPlane.transform.position.y + elevation - pipeHeight, arCamera.transform.position.z);
        //var pos = new Vector3(arCamera.transform.position.x, groundPlane.transform.position.y - pipeHeight, arCamera.transform.position.z);
        waterGameObject.transform.SetPositionAndRotation(pos, Quaternion.identity);
	}

    private void TogglePlaneDetection()
    {
        arPlaneManager.enabled = !arPlaneManager.enabled;

        // Go though each plane
        foreach (ARPlane plane in arPlaneManager.trackables)
        {
            plane.gameObject.SetActive(arPlaneManager.enabled);
        }
    }

    // Currently NOT in use
    private void CreateClickPointObject(Touch touch)
    {
        Ray ray = arCamera.ScreenPointToRay(touch.position);
        RaycastHit hitInfo;

        if (Physics.Raycast(ray, out hitInfo))
        {
            // Visualizes the click point on the surface
            var clickPointObject = Instantiate(clickPointPrefab, hitInfo.point, Quaternion.identity);
            listOfPlacedObjects.Add(clickPointObject);

            // input the two vectors/points
            if (HasSavedPoint)
            {
                CreateQuadFromPoints(savedPoint, hitInfo.point);
                HasSavedPoint = false;
            }
            else
            {
                savedPoint = hitInfo.point;
                HasSavedPoint = true;
            }
        }
    }

    private void CreateQuadFromPoints(Vector3 firstPoint, Vector3 secondPoint)
    {
        GameObject newMeshObject = new GameObject("wall");
        MeshFilter newMeshFilter = newMeshObject.AddComponent<MeshFilter>();
        newMeshObject.AddComponent<MeshRenderer>();

        // ge varje mesh ett material - 0: Occlusion
        newMeshObject.GetComponent<Renderer>().material = materialForWalls[0];
        Mesh newMesh = new Mesh();

        Vector3 heightVector = new Vector3(0, height, 0);

        newMesh.vertices = new Vector3[]
        {
            firstPoint,
            secondPoint,
            firstPoint + heightVector,
            secondPoint + heightVector
        };

        newMesh.triangles = new int[]
        {
            0,2,1,1,2,3,
        };

        newMesh.RecalculateNormals();
        newMesh.RecalculateTangents();
        newMesh.RecalculateBounds();

        newMeshFilter.mesh = newMesh;

        // At first the meshes aren't visible
        newMeshObject.SetActive(false);

        // Add the mesh to the list
        listOfWallMeshes.Add(newMeshObject);
    }

    private void renderWallMeshes(bool isVisible)
    {
        foreach (GameObject wallMesh in listOfWallMeshes)
        {
            wallMesh.SetActive(isVisible);
        }
    }

    private void renderClickPoints(bool isVisible)
    {
        foreach (GameObject point in listOfPlacedObjects)
        {
            point.SetActive(isVisible);
        }
    }

    private void renderLineRenderers(bool isVisible)
    {
        foreach (GameObject line in listOfLinerenderers)
        {
            line.SetActive(isVisible);
        }
    }

    private void DrawLinesBetweenObjects()
    {
        int lengthOfList = listOfPlacedObjects.Count;
        if (lengthOfList > 1)
        {
            for (int i = 0; i < lengthOfList - 1; i++)
            {
                try
                {
                    var lineRendererGameObject = Instantiate(lineRendererPrefab);
                    var lineRenderer = lineRendererGameObject.GetComponent<LineRenderer>();
                    lineRenderer.SetPosition(0, listOfPlacedObjects[i].transform.position);
                    lineRenderer.SetPosition(1, listOfPlacedObjects[i + 1].transform.position);
                    listOfLinerenderers.Add(lineRendererGameObject);
                }
                catch (Exception)
                {
                    Debug.LogError("Exceptions baby!");
                    throw;
                }
            }
        }
    }

    // UI logic
    public void ResetSession()
    {
        if (waterGameObject != null)
            Destroy(waterGameObject);

        // Destroy the placed objects if any
        for (int i = 0; i < listOfPlacedObjects.Count; i++)
        {
            Destroy(listOfPlacedObjects[i].gameObject);
        }
        listOfPlacedObjects.Clear();

        // Destroy the gameobjects holding the wall meshes
        for (int i = 0; i < listOfWallMeshes.Count; i++)
        {
            Destroy(listOfWallMeshes[i].gameObject);
        }
        listOfWallMeshes.Clear();

        for (int i = 0; i < listOfLinerenderers.Count; i++)
        {
            Destroy(listOfLinerenderers[i].gameObject);
        }
        listOfLinerenderers.Clear();

        // Reset variables
        waterIsVisible = false;
        adjustRadiusSlider.value = 0;
        multiplyElevationSlider.value = 0;
        HasSavedPoint = false;
        elevation = 0.0f;
        arSession.Reset();
        Debug.Log("Session reset");
    }

    public void RenderWater()
    {
        waterIsVisible = true;
        Debug.Log("Pressed water render button -> water is now: " + waterIsVisible);
        wallPlacementEnabled = false;
        sliderPanel.SetActive(true);
        renderWallMeshes(true);
        renderClickPoints(false);
        renderLineRenderers(false);
        multiplierTextVisibility.SetActive(true);
        radiusTextVisibility.SetActive(true);
    }

    public void PlaceWalls()
    {
        wallPlacementEnabled = true;
        waterIsVisible = false;
        sliderPanel.SetActive(false);
        renderWallMeshes(false);
        renderClickPoints(true);
        renderLineRenderers(true);
        multiplierTextVisibility.SetActive(false);
        radiusTextVisibility.SetActive(false);
    }


    public void MultiplyElevation()
    {
        // Calculate elevation returns cm - dividing by 100 turns that into meters
        try
        {
            elevation = ((float)csvReader.CalculateElevationAtLocation().altitude) / 100.0f;

        }
        catch (System.Exception ex)
        {
            Debug.Log("csv stuff doesn't work");
        }
        var sliderValue = multiplyElevationSlider.value;

        Debug.Log($"Elevation before log value applied: {elevation} Slider value: {sliderValue}");

        var logarithmicChange = Helpers.ConvertToLog(sliderValue, elevation);
        elevation = logarithmicChange;

        Debug.Log($"Logarithmic change: {logarithmicChange}");

        multiplierText.SetText("Multiplier: \n" + System.Math.Round(elevation,2));
    }

    public void IncreaseRadius()
    {
        // Adjustment of radius
        var sliderValue = adjustRadiusSlider.value;
        radius = sliderValue * maximumRaidus + minimumRadius;

        if (radius > maximumRaidus)
        {
            radius = maximumRaidus;
        }
    }
}
