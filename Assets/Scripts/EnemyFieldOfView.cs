using UnityEngine;

public class EnemyFieldOfView : MonoBehaviour
{
    public float viewRadius = 5f;
    public float viewAngle = 90f;
    public LayerMask obstacleMask;

    private Mesh mesh;
    private int stepCount = 10; // 视野分割段数
    private float stepAngleSize;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        stepAngleSize = viewAngle / stepCount;
    }

    void LateUpdate()
    {
        DrawFieldOfView();
    }

    void DrawFieldOfView()
    {
        mesh.Clear();
        Vector3[] vertices = new Vector3[stepCount + 2];
        int[] triangles = new int[stepCount * 3];

        vertices[0] = Vector3.zero;

        float currentAngle = -viewAngle / 2;
        for (int i = 0; i <= stepCount; i++)
        {
            Vector3 dir = Quaternion.Euler(0, 0, currentAngle) * Vector3.up; // 以 up 方向为基准旋转
            vertices[i + 1] = dir * viewRadius;

            if (i < stepCount)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
            currentAngle += stepAngleSize;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}
