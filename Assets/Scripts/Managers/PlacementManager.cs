/**
 *  Disabled the radius slider for the 23/9/19 prototype 
 *
 *
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.ProBuilder;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    private float height = 4;
    private GameObject groundPlane;
    private bool waterIsPlaced;
    private bool waterIsVisible;
    private bool wallPlacementEnabled;

    // Csv variables
    private CSVReader csvReader;

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

    // Raycasts
    private List<ARRaycastHit> hitsAR = new List<ARRaycastHit>();
    private RaycastHit hits;
    private bool HasSavedPoint;
    private Vector3 savedPoint;
    private List<GameObject> listOfPlacedObjects;

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
        multiplierText = multiplyElevationSlider.GetComponent<textOverlayMultSlider>();
        radiusText = adjustRadiusSlider.GetComponent<textOverlayRadiusSlider>();
        listOfPlacedObjects = new List<GameObject>();
        listOfWallMeshes = new List<GameObject>();

        // startPoint & endPoint
        startPoint = Instantiate(clickPointPrefab, Vector3.zero, Quaternion.identity);
        endPoint = Instantiate(clickPointPrefab, Vector3.zero, Quaternion.identity);
        startPoint.SetActive(false);
        endPoint.SetActive(false);
        measureLine = GetComponent<LineRenderer>();
        measureLine.enabled = false;

        // UI
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

                        if (Physics.Raycast(ray, out hitInfo))
                        {
                            startPoint.SetActive(true);
                            startPoint.transform.SetPositionAndRotation(hitInfo.point, Quaternion.identity);
                        }
                    }
                    //else if (planeIsPlaced)
                    //{
                    //    CreateClickPointObject(touch);
                    //}
                }

                else if (TouchPhase.Moved == touch.phase && wallPlacementEnabled)
                {
                    Ray ray = arCamera.ScreenPointToRay(touch.position);
                    RaycastHit hitInfo;

                    if (Physics.Raycast(ray, out hitInfo))
                    {
                        endPoint.SetActive(true);
                        endPoint.transform.SetPositionAndRotation(hitInfo.point, Quaternion.identity);
                    }
                }

                else if (TouchPhase.Ended == touch.phase && wallPlacementEnabled)
                {
                    // place wall
                    CreateQuadFromPoints(startPoint.transform.position, endPoint.transform.position);
                    startPoint.SetActive(false);
                    endPoint.SetActive(false);

                    if(!renderWaterButton.interactable)
                    {
                        renderWaterButton.interactable = true;
                    }
                }
            }
        }

        if (startPoint.activeSelf && endPoint.activeSelf)
        {
            measureLine.enabled = true;
            measureLine.SetPosition(0, startPoint.transform.position);
            measureLine.SetPosition(1, endPoint.transform.position);
        }
    }


    // Helper functions
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
        //var pos = new Vector3(arCamera.transform.position.x, groundPlane.transform.position.y + elevation - pipeHeight, arCamera.transform.position.z);
        var pos = new Vector3(arCamera.transform.position.x, groundPlane.transform.position.y - pipeHeight, arCamera.transform.position.z);
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

        // ge varje mesh ett material
        newMeshObject.GetComponent<Renderer>().materials = materialForWalls;
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

        // spara undan meshen i en lista
        listOfWallMeshes.Add(newMeshObject);
    }

    // UI logic
    public void ResetSession()
    {
        if (waterGameObject != null)
            Destroy(waterGameObject);
        // destroy the placed objects if any
        for (int i = 0; i < listOfPlacedObjects.Count; i++)
        {
            Destroy(listOfPlacedObjects[i].gameObject);
        }
        listOfPlacedObjects.Clear();

        // destroy the gameobjects holding the wall meshes
        for (int i = 0; i < listOfWallMeshes.Count; i++)
        {
            Destroy(listOfWallMeshes[i].gameObject);
        }
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
    }

    public void PlaceWalls()
    {
        // Logic
        wallPlacementEnabled = true;

        // Disable sliders
        sliderPanel.SetActive(false);

        Debug.Log("Placing walls is now possible");
    }


    public void MultiplyElevation()
    {
        // calculate elevation returns cm - dividing by 100 turns that into meters
        try
        {
            elevation = ((float)csvReader.CalculateElevationAtLocation().altitude) / 100.0f;

        }
        catch (System.Exception ex)
        {
            Debug.Log("csv stuff doesn't work");
        }
        var sliderValue = multiplyElevationSlider.value;

        var logarithmicChange = Helpers.ConvertToLog(sliderValue, elevation);
        elevation = logarithmicChange;

        multiplierText.SetText("Multiplier: \n" + System.Math.Round(elevation,2));
    }

    public void IncreaseRadius()
    {
        // adjustment of radius
        var sliderValue = adjustRadiusSlider.value;
        radius = sliderValue * maximumRaidus + minimumRadius;

        if (radius > maximumRaidus)
        {
            radius = maximumRaidus;
        }
    }
}
