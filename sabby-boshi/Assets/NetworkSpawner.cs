using Unity.Netcode;
using UnityEngine;

public class NetworkSpawner : MonoBehaviour
{
    public int landmarkNum;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < landmarkNum; i++)
        {
            var landmark = gameObject.transform.Find("Landmark 0");
            Debug.Log(landmark);
            var finalThing = landmark.gameObject;
            Debug.Log(finalThing);
            landmark.GetComponent<NetworkObject>().Spawn();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
