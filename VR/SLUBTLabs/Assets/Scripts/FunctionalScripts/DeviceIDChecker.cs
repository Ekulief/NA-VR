using UnityEngine;

public class DeviceIdExample : MonoBehaviour
{
    void Start()
    {
        string deviceId = SystemInfo.deviceUniqueIdentifier;
        Debug.Log("Device Unique ID: " + deviceId);
    }
}