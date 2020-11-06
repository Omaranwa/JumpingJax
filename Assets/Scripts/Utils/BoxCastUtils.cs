using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoxCastUtils
{
    //public class StandardBoxCastInput
    //{
    //    public Vector3 center;
    //    public Vector3 halfExtents;
    //    public Vector3 direction;
    //    public Quaternion orientation;
    //    public float maxDistance;
    //    public LayerMask layerMask;
    //    public Collider collider;

    //    public StandardBoxCastInput(Collider collider, Vector3 direction, float maxDistance, LayerMask layerMask)
    //    {
    //        center = collider.bounds.center;
    //        halfExtents = collider.bounds.extents;
    //        this.direction = direction;
    //        orientation = Quaternion.identity;
    //        this.maxDistance = maxDistance;
    //        this.layerMask = layerMask;
    //    }
    //}

    public class SourceBoxCastInput
    {
        public Vector3 center;
        public Vector3 halfExtents;
        public Vector3 direction;
        public Quaternion orientation;
        public float maxDistance;
        public LayerMask layerMask;
        public Collider collider;

        public SourceBoxCastInput(Vector3 start, Vector3 end, LayerMask layerMask, Collider collider)
        {
            center = start;
            halfExtents = collider.bounds.extents;
            direction = (end - start).normalized;
            orientation = Quaternion.identity;
            maxDistance = (end - start).magnitude;
            this.layerMask = layerMask;
            this.collider = collider;
        }
    }

    public class SourceBoxCastOutput
    {
        public float fraction;

        public Vector3 startPosition;
        public Vector3 endPosition;

        public Vector3 normal;
    }

    public static void SourceBoxCast(SourceBoxCastInput input, out SourceBoxCastOutput output)
    {
        RaycastHit[] hits = Physics.BoxCastAll(
            center: input.center,
            halfExtents: input.halfExtents,
            direction: input.direction,
            orientation: input.orientation,
            maxDistance: input.maxDistance,
            layerMask: input.layerMask);

        List<RaycastHit> validHits = hits
            .ToList()
            .OrderBy(hit => hit.distance)
            .Where(hit => !hit.collider.isTrigger)
            .Where(hit => !Physics.GetIgnoreCollision(hit.collider, input.collider))
            //.Where(hit => hit.point != Vector3.zero)
            .ToList();

        float fraction = 0;
        Vector3 endPosition = Vector3.zero;
        Vector3 normal = Vector3.zero;

        if (validHits.Count > 0)
        {
            RaycastHit closestHit = validHits.First();
            fraction = closestHit.distance / input.maxDistance;
            endPosition = input.center + (input.direction * closestHit.distance);
            normal = closestHit.normal;
        }
        else
        {
            fraction = 1;
            endPosition = input.center + (input.direction * input.maxDistance);
            normal = Vector3.zero;
        }


        output = new SourceBoxCastOutput()
        {
            fraction = fraction,
            startPosition = input.center,
            endPosition = endPosition,
            normal = normal
        };
    }


}