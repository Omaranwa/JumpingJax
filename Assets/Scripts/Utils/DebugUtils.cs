using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugUtils
{
    public static void DrawCube(Vector3 center, Vector3 extents)
    {
        // fl - fr
        //  |    |
        // bl - br
        Vector3 bottomFrontLeft = new Vector3(center.x - extents.x, center.y - extents.y, center.z + extents.z);
        Vector3 bottomFrontRight = new Vector3(center.x + extents.x, center.y - extents.y, center.z + extents.z);
        Vector3 bottomBackLeft = new Vector3(center.x - extents.x, center.y - extents.y, center.z - extents.z);
        Vector3 bottomBackRight = new Vector3(center.x + extents.x, center.y - extents.y, center.z - extents.z);

        Vector3 topFrontLeft = new Vector3(center.x - extents.x, center.y + extents.y, center.z + extents.z);
        Vector3 topFrontRight = new Vector3(center.x + extents.x, center.y + extents.y, center.z + extents.z);
        Vector3 topBackLeft = new Vector3(center.x - extents.x, center.y + extents.y, center.z - extents.z);
        Vector3 topBackRight = new Vector3(center.x + extents.x, center.y + extents.y, center.z - extents.z);

        Debug.DrawLine(bottomFrontLeft, bottomFrontRight);
        Debug.DrawLine(bottomFrontRight, bottomBackRight);
        Debug.DrawLine(bottomBackRight, bottomBackLeft);
        Debug.DrawLine(bottomBackLeft, bottomFrontLeft);

        Debug.DrawLine(topFrontLeft, topFrontRight);
        Debug.DrawLine(topFrontRight, topBackRight);
        Debug.DrawLine(topBackRight, topBackLeft);
        Debug.DrawLine(topBackLeft, topFrontLeft);

        Debug.DrawLine(bottomFrontLeft, topFrontLeft);
        Debug.DrawLine(bottomFrontRight, topFrontRight);
        Debug.DrawLine(bottomBackRight, topBackRight);
        Debug.DrawLine(bottomBackLeft, topBackLeft);
    }
}