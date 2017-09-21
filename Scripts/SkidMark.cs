using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkidMark : MonoBehaviour 
{
    public Material skidMarkMat;
    public float skidMarkWidth = 0.25f;
    public float skidThrehold = 0.4f;

    private WheelCollider wc;
    private bool isSkidding = false;

    private bool existLastVertex = false;
    private Vector3[] lastVertices = new Vector3[2];
    private Vector3 lastHitPos;

    void Start()
    {
        wc = GetComponent<WheelCollider>();
    }

	void Update () 
	{
        WheelHit hit;
        if(wc.GetGroundHit(out hit))
        {
            Vector3 normal = hit.normal;
            Vector3 pos = hit.point;
            float fraction = Mathf.Abs(hit.forwardSlip);
            print(fraction);
            if(fraction  > skidThrehold)
            {
                if(isSkidding)
                {
                    DrawSkidMark(lastHitPos, pos, normal);
                }
                else
                {
                    lastHitPos = pos;
                }

                isSkidding = true;
            }
            else
            {
                isSkidding = false;
            }
        }
	}

    private void DrawSkidMark(Vector3 lastHitPos, Vector3 pos, Vector3 normal)
    {
        Vector3 zDir = (pos - lastHitPos).normalized;
        Vector3 xDir = Vector3.Cross(normal, zDir).normalized;

        if(existLastVertex)
        {
            Vector3[] vertices = new Vector3[4];
            vertices[0] = lastVertices[0];
            vertices[1] = lastVertices[1];

            float halfSkidMarkWidth = skidMarkWidth * 0.5f;
            vertices[2] = pos + -halfSkidMarkWidth * xDir;
            vertices[3] = pos + halfSkidMarkWidth * xDir;

            lastVertices[0] = vertices[3];
            lastVertices[1] = vertices[2];

            int[] triangles = { 0, 1, 2, 0, 2, 3 };

            Vector2[] uvs = new Vector2[4];
            uvs[0] = new Vector2(0, 0);
            uvs[1] = new Vector2(1, 0);
            uvs[2] = new Vector2(1, 1);
            uvs[3] = new Vector2(0, 1);

            GameObject mark = new GameObject("Mark");
            mark.AddComponent<AutoDestroy>();
            MeshFilter filter = mark.AddComponent<MeshFilter>();
            MeshRenderer meshRender = mark.AddComponent<MeshRenderer>();
            Mesh mesh = new Mesh();

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            filter.mesh = mesh;

            meshRender.sharedMaterial = skidMarkMat;
        }
        else
        {
            float halfSkidMarkWidth = skidMarkWidth * 0.5f;
            lastVertices[0] = pos + halfSkidMarkWidth * xDir;
            lastVertices[1] = pos + -halfSkidMarkWidth * xDir;
        }

        existLastVertex = true;
    }
}
