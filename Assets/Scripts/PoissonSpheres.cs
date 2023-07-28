using System.Collections.Generic;
using UnityEngine;

public class PoissonSpheres
{
    private Vector3 m_Size;
    private float m_MinSphereRadius;
    private float m_MaxSphereRadius;
    private float m_CellSize;
    private float m_SpacingLimit;

    private int[,,] m_Grid;
    private int m_GridSizeX, m_GridSizeY, m_GridSizeZ;

    private List<Point> m_Points = new List<Point>();
    private List<Point> m_SpawnPoints = new List<Point>();


    private int m_MinNearest = 9999999;
    private int m_MaxNearest = -9999999;


    public List<Point> Points { get => m_Points; }


    public class Point
    {
        public Vector3 pos;
        public float r;
        public List<Point> nextList;

        public Point(Vector3 pos, float r)
        {
            this.pos = pos;
            this.r = r;
            nextList = new List<Point>();
        }

        
    }

    public PoissonSpheres(Vector3 size, float minSphereRadius, float maxSphereRadius, float spacingLimit) 
    {
        m_Size = size;
        m_MinSphereRadius = minSphereRadius;
        m_MaxSphereRadius = maxSphereRadius;
        m_CellSize = 2 * minSphereRadius / Mathf.Sqrt(3);
        m_SpacingLimit = spacingLimit;

        m_GridSizeX = Mathf.CeilToInt(size.x / m_CellSize);
        m_GridSizeY = Mathf.CeilToInt(size.y / m_CellSize);
        m_GridSizeZ = Mathf.CeilToInt(size.z / m_CellSize);
    }

    public void GeneratePoints(int numSamplesBeforeRejection)
    {
        m_Points = new List<Point>();
        m_SpawnPoints = new List<Point>();
        m_Grid = new int[m_GridSizeX, m_GridSizeY, m_GridSizeZ];
        for (int z = 0; z < m_GridSizeZ; z++)
        {
            for (int y = 0; y < m_GridSizeY; y++)
            {
                for (int x = 0; x < m_GridSizeX; x++)
                {
                    m_Grid[x, y, z] = -1;
                }
            }
        }


        m_SpawnPoints.Add(new Point(m_Size / 2.0f, Random.Range(m_MinSphereRadius, m_MaxSphereRadius)));
        while (m_SpawnPoints.Count > 0)
        {
            int spawnIndex = Random.Range(0, m_SpawnPoints.Count);
            Point spawnerPoint = m_SpawnPoints[spawnIndex];
            bool candidateAccepted = false;

            for (int i = 0; i < numSamplesBeforeRejection; i++)
            {
                float candidateRadius = Random.Range(m_MinSphereRadius, m_MaxSphereRadius);
                float minDistFromSpawn = spawnerPoint.r + candidateRadius;
                float distFromSpawn = Random.Range(minDistFromSpawn, m_SpacingLimit * minDistFromSpawn);
                Vector3 candidatePos = spawnerPoint.pos + distFromSpawn * Random.onUnitSphere;
                Point candidatePoint = new Point(candidatePos, candidateRadius);

                if (!pointIsValid(candidatePoint)) continue;

                m_Grid[(int)(candidatePos.x / m_CellSize), (int)(candidatePos.y / m_CellSize), (int)(candidatePos.z / m_CellSize)] = m_Points.Count;
                m_Points.Add(candidatePoint);
                m_SpawnPoints.Add(candidatePoint);
                candidateAccepted = true;
                break;
            }
            if (!candidateAccepted)
            {
                m_SpawnPoints.RemoveAt(spawnIndex);
            }
        }
    }

    private bool pointIsValid(Point point)
    {
        if (point.pos.x < 0.0f || point.pos.x > m_Size.x || point.pos.y < 0.0f || point.pos.y > m_Size.y || point.pos.z < 0.0f || point.pos.z > m_Size.z) return false;

        float floatGridPosX = point.pos.x / m_CellSize;
        float floatGridPosY = point.pos.y / m_CellSize;
        float floatGridPosZ = point.pos.z / m_CellSize;

        float spawnRadiusSum = (point.r + m_MaxSphereRadius) / m_CellSize;
        int startX = Mathf.Max((int)(floatGridPosX - spawnRadiusSum), 0);
        int endX = Mathf.Min((int)(floatGridPosX + spawnRadiusSum), m_GridSizeX - 1);
        int startY = Mathf.Max((int)(floatGridPosY - spawnRadiusSum), 0);
        int endY = Mathf.Min((int)(floatGridPosY + spawnRadiusSum), m_GridSizeY - 1);
        int startZ = Mathf.Max((int)(floatGridPosZ - spawnRadiusSum), 0);
        int endZ = Mathf.Min((int)(floatGridPosZ + spawnRadiusSum), m_GridSizeZ - 1);

        for (int z = startZ; z <= endZ; z++)
        {
            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    int index = m_Grid[x, y, z];
                    if (index == -1) continue;

                    float sqrDist = (point.pos - m_Points[index].pos).sqrMagnitude;
                    float radiusSum = point.r + m_Points[index].r;
                    if (sqrDist < radiusSum * radiusSum) return false;
                }
            }
        }

        return true;
    }

    private class NearestPoint : IHeapItem<NearestPoint>
    {
        private int m_HeapIndex;

        private int m_PointIndex;
        private float m_SqrDist;

        public NearestPoint(int pointIndex, float sqrDist)
        {
            m_PointIndex = pointIndex;
            m_SqrDist = sqrDist;
        }

        public int HeapIndex { get => m_HeapIndex; set => m_HeapIndex = value; }
        public int PointIndex { get => m_PointIndex; }

        public int CompareTo(object obj)
        {
            NearestPoint other = obj as NearestPoint;
            return other.m_SqrDist.CompareTo(m_SqrDist);
        }
    }

    public void ConnectNearest(int searchDist, int idealNumOfNeighbours)
    {
        foreach (Point p in m_Points)
        {
            float floatGridPosX = p.pos.x / m_CellSize;
            float floatGridPosY = p.pos.y / m_CellSize;
            float floatGridPosZ = p.pos.z / m_CellSize;

            int gridPosX = (int)floatGridPosX;
            int gridPosY = (int)floatGridPosY;
            int gridPosZ = (int)floatGridPosZ;

            int startX = Mathf.Max(gridPosX - searchDist, 0);
            int endX = Mathf.Min(gridPosX + searchDist, m_GridSizeX - 1);
            int startY = Mathf.Max(gridPosY - searchDist, 0);
            int endY = Mathf.Min(gridPosY + searchDist, m_GridSizeY - 1);
            int startZ = Mathf.Max(gridPosZ - searchDist, 0);
            int endZ = Mathf.Min(gridPosZ + searchDist, m_GridSizeZ - 1);

            int searchWidth = (int)(2.0f * searchDist) + 1;
            Heap<NearestPoint> heap = new Heap<NearestPoint>(searchWidth * searchWidth * searchWidth);


            int tempGridPos = m_Grid[gridPosX, gridPosY, gridPosZ];
            m_Grid[gridPosX, gridPosY, gridPosZ] = -1;
            for (int z = startZ; z <= endZ; z++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    for (int x = startX; x <= endX; x++)
                    {
                        int index = m_Grid[x, y, z];
                        if (index == -1) continue;

                        heap.Add(new NearestPoint(index, (m_Points[index].pos - p.pos).sqrMagnitude));
                    }
                }
            }
            m_Grid[gridPosX, gridPosY, gridPosZ] = tempGridPos;


            while (heap.GetCount() > 0 && p.nextList.Count < idealNumOfNeighbours)
            {
                p.nextList.Add(m_Points[heap.Pop().PointIndex]);
            }

            m_MinNearest = Mathf.Min(p.nextList.Count, m_MinNearest);
            m_MaxNearest = Mathf.Max(p.nextList.Count, m_MaxNearest);
        }
        Debug.Log("min: " + m_MinNearest + ", max: " + m_MaxNearest);
    }
}
