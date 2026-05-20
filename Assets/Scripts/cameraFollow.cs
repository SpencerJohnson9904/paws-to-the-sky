using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 3, -7);

    void LateUpdate()
    {
        transform.position = target.position + offset;
        transform.LookAt(target); 
    }

}