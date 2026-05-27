using UnityEngine;

public class cameraFollow : MonoBehaviour
{
    public Transform cat;
    public float distance = 5f;
    public float height = 2f;
    public float smoothSpeed = 5f;

    void LateUpdate()
{
    Quaternion targetRotation = Quaternion.LookRotation(cat.forward);
    transform.rotation = Quaternion.Slerp(
        transform.rotation,
        targetRotation,
        Time.deltaTime * smoothSpeed
    );


    Vector3 desiredPosition = cat.position 
        - transform.forward * distance 
        + Vector3.up * height;

    transform.position = Vector3.Lerp(
        transform.position,
        desiredPosition,
        Time.deltaTime * smoothSpeed
    );

    //transform.LookAt(cat.position + Vector3.up * 1f);
}
}