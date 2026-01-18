using UnityEngine;

public class RotateParent : MonoBehaviour
{
    public float rotateSpeed = 90f; // độ/giây

    void Update()
    {
        transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
    }
}
