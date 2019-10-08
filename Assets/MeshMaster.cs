using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshMaster : MonoBehaviour
{

    // Start is called before the first frame update
    void Start()
    {
        DelaunayMesh mesh = GetComponent<DelaunayMesh>();
        mesh.Generate(-1);
    }
}
