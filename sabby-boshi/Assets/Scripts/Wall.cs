using UnityEngine;

public class Wall : MonoBehaviour
{

    public float wallSpeed;
    public float wallStartingPos;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform.position = new Vector3(0f, -2.7f, wallStartingPos);
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = transform.position + new Vector3(0f, 0f, -wallSpeed);
    }
 
}
