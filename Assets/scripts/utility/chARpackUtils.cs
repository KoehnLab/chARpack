using UnityEngine;

public static class chARpackUtils
{

    public static void setObjectGrabbed(Transform obj, bool value)
    {
        var mol = obj.GetComponent<Molecule>();
        if (mol != null)
        {
            mol.isGrabbed = value;
            return;
        }
        var go = obj.GetComponent<GenericObject>();
        if (go != null)
        {
            go.isGrabbed = value;
        }
    }

    public static float distanceToHeadRay(Vector3 pos, Camera cam)
    {

        var intersection = getHeadRayIntersection(pos, cam);
        return Vector3.Distance(pos, intersection);
    }

    public static Vector3 getHeadRayIntersection(Vector3 pos, Camera cam)
    {
        var distance = 2f;
        var head_ray_startpoint = cam.transform.position;
        var head_ray_endpoint = head_ray_startpoint + (cam.transform.forward * distance);

        var t = ((pos.x - head_ray_endpoint.x) * (head_ray_startpoint.x - head_ray_endpoint.x) +
            (pos.y - head_ray_endpoint.y) * (head_ray_startpoint.y - head_ray_endpoint.y) +
            (pos.z - head_ray_endpoint.z) * (head_ray_startpoint.z - head_ray_endpoint.z)) / (distance * distance);

        var intersection = head_ray_endpoint + t * (head_ray_startpoint - head_ray_endpoint);

        return intersection;
    }
}
