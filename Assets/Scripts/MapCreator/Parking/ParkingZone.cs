﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ParkingZone : MonoBehaviour
{
    [SerializeField] PathNode pathNodePrefab;
    
    private Mesh mesh;
    private MeshFilter filter;
    private bool editActive;
    private int editVertexIndx;
    private Transform sphereHolder;
    private Transform nodeHolder;

    private List<Vector2> vertices2D;
    private List<GameObject> spheres;
    private List<PathNode> nodes;

    private void Awake()
    {
        vertices2D = new List<Vector2>();
       
        vertices2D.Add(new Vector2(0, 0));
        vertices2D.Add(new Vector2(0, 25));
        vertices2D.Add(new Vector2(25, 25));
        vertices2D.Add(new Vector2(25, 50));
        vertices2D.Add(new Vector2(0, 50));
        vertices2D.Add(new Vector2(0, 75));
        vertices2D.Add(new Vector2(75, 75));
        vertices2D.Add(new Vector2(75, 50));
        vertices2D.Add(new Vector2(50, 50));
        vertices2D.Add(new Vector2(50, 25));
        vertices2D.Add(new Vector2(75, 25));
        vertices2D.Add(new Vector2(75, 0));

        mesh = new Mesh();
        mesh.name = "parkingZoneMesh";
        filter = gameObject.AddComponent<MeshFilter>();
        filter.mesh = mesh;

        MeshUtil.ApplyMaterial(gameObject, new Color32(165, 165, 165, 255));

        transform.Rotate(Vector3.right, 90f);
        transform.localPosition = new Vector3(0, 0.1f, 0);

        spheres = new List<GameObject>();
        GameObject sphereHolderObj = new GameObject("SphereHolder");
        sphereHolderObj.transform.SetParent(transform, false);
        sphereHolder = sphereHolderObj.transform;

        nodes = new List<PathNode>();
        GameObject nodeHolderObj = new GameObject("NodeHolder");
        nodeHolderObj.transform.SetParent(transform, false);
        nodeHolder = nodeHolderObj.transform;
    }

    private void OnEnable()
    {
        MapCreatorLoader.Instance.CameraInstance.PointerPositionChanged += PointerPositionChanged;
    }

    private void OnDisable()
    {
        MapCreatorLoader.Instance.CameraInstance.PointerPositionChanged -= PointerPositionChanged;
    }

    private void PointerPositionChanged()
    {
        Vector2 pos2d = MapCreatorLoader.Pointer2d;

        if (editActive)
        {
            if (vertices2D.Count <= editVertexIndx || editVertexIndx < 0)
            {
                Debug.LogWarning("Invalid point", this);
                return;
            }

            vertices2D[editVertexIndx] = pos2d;
            ReDraw();
        }

        //bool inPoly = GeometryUtil.Pnpoly(vertices2D.ToArray(), pos2d);
    }

    private void ReDraw()
    {
        int[] indices = Triangulator.Triangulate(vertices2D);
        Vector3[] vertices = vertices2D.Select(c => new Vector3(c.x, c.y, 0)).ToArray();
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = indices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        foreach (var i in spheres)
            Destroy(i.gameObject);

        spheres.Clear();

        foreach (var i in vertices2D)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(sphereHolder, false);
            sphere.transform.localPosition = new Vector3(i.x, i.y, 0);
            sphere.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            sphere.GetComponent<MeshRenderer>().material.color = Color.blue;
            spheres.Add(sphere);
        }
    }

    private void Start()
    {
        ReDraw();
    }

    public void AddPoint()
    {
        List<Vector2> averages = new List<Vector2>();

        for (int i = 0; i < vertices2D.Count - 1; i++)
        {
            Vector2 v1 = vertices2D[i];
            Vector2 v2 = vertices2D[i + 1];
            Vector2 n = new Vector2((v1.x + v2.x) / 2, (v1.y + v2.y) / 2);
            averages.Add(n);
        }

        Vector2 vn1 = vertices2D[0];
        Vector2 vn2 = vertices2D[vertices2D.Count - 1];
        Vector2 vn = new Vector2((vn1.x + vn2.x) / 2, (vn1.y + vn2.y) / 2);
        averages.Add(vn);

        Vector2 p2d = MapCreatorLoader.Pointer2d;
        int closest = GeometryUtil.FindClosest(averages, p2d);

        int insertPoint = closest + 1;
        if (insertPoint >= vertices2D.Count)
            insertPoint = 0;

        vertices2D.Insert(insertPoint, p2d);
        ReDraw();
    }

    public void RemovePoint()
    {
        if(vertices2D.Count <= 4)
        {
            Debug.LogWarning("To small points count");
            return;
        }

        int indxMinDist = GeometryUtil.FindClosest(vertices2D, MapCreatorLoader.Pointer2d);
        vertices2D.RemoveAt(indxMinDist);
        ReDraw();
    }

    private void FindEditVertex()
    {
        int indxMinDist = GeometryUtil.FindClosest(vertices2D, MapCreatorLoader.Pointer2d);
        editVertexIndx = indxMinDist;
    }

    public void ToggleEdit(bool active)
    {
        if (active)
            FindEditVertex();

        editActive = active;
    }

    public void AddNode(bool connect)
    {
        float dist;
        int idClosest = GeometryUtil.FindClosest(nodes, MapCreatorLoader.Pointer2d, out dist);

        PathNode node = null;

        //if (connect && idClosest >= 0 && dist < 3)
        //{
        //    //node = 
        //    return;
        //}

        node = Instantiate(pathNodePrefab);
        node.transform.SetParent(nodeHolder, false);
        Vector2 p2d = MapCreatorLoader.Pointer2d;
        node.transform.localPosition = p2d;
        node.transform.Translate(new Vector3(0, 0, -0.5f));

        if (connect && idClosest >= 0)
        {
            PathNode closest = nodes[idClosest];
            closest.AddNode(node);
        }

        nodes.Add(node);
    }

    public void RemoveNode()
    {
        Vector2 p2d = MapCreatorLoader.Pointer2d;
        int id = GeometryUtil.FindClosest(nodes, p2d);
        if (id < 0)
            return;

        PathNode removingNode = nodes[id];

        foreach (var i in nodes)
            i.CheckNodeForRemove(removingNode);

        Destroy(removingNode.gameObject);
        nodes.Remove(removingNode);
    }
}