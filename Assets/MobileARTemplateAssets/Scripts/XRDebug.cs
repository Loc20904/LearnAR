using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR;

public class XRDebug : MonoBehaviour
{
    void Start()
    {
        var loader = XRGeneralSettings.Instance.Manager.activeLoader;

        Debug.Log("Active Loader: " + (loader == null ? "NULL" : loader.name));

        var displays = new System.Collections.Generic.List<XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(displays);

        Debug.Log("Display Subsystems Count: " + displays.Count);

        foreach (var d in displays)
        {
            Debug.Log("Display running: " + d.running);
        }
    }
}