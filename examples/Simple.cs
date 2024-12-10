using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Simple : MonoBehaviour
{
    // Activates sending (default true)
    // turning this off allows an integration to leave the event-sending code
    // in place but avoid any sending, processing or memory overhead
    public bool active = true;
    public bool debugMode = false;
    public bool quietMode = false;
    public string debugUrl = "http://localhost:9998";
    public string accessToken = "";

    Platoon.PlatoonSDK s_instance;

    public void Start()
    {
        Debug.Log("Starting Analytics");

        s_instance = new Platoon.PlatoonSDK(this, accessToken, "steam#13", active);

        if (debugMode)
        {
            s_instance.BaseUrl = debugUrl;
        }

        if (quietMode)
        {
            s_instance.Quiet = quietMode;
        }

        // by default, the Application.version will be used for the session's version parameter
        // this can be overridden if required:
        //s_instance.SetCustomSessionData("version", "1.2.3");


        // then, set application-specific parameters
        s_instance.SetCustomSessionData("name", "bob");
        s_instance.SetCustomSessionData("payment_tier", "none");
        s_instance.SetCustomSessionData("branch", "developer");
        s_instance.SetCustomSessionData("vendor", "steam");

        // Let the system know we're interested in remote flags
        // If we're not, then it saves some server-side overhead
        s_instance.EnableFlags();

        // ... and once it's all set up, start the session
        // The provided callback will be called when the session is initialised and
        // ready to receive events. At this point, the feature flags can also be queried
        // Alternatively, IsReady() at any point.
        s_instance.StartSession(OnPlatoonReady);

        // ERROR - this will fail because the session is not yet ready
        // TODO - might want to catch these internally and process them once the 
        // server has replied with the begun session??
        // s_instance.AddEvent("failing event");
    }

    // TODO : Do we need to deal with OnApplicationFocus & OnApplicationPause?
    public void OnApplicationQuit()
    {
        s_instance.Close();
    }

    public void OnPlatoonReady()
    {
        Debug.Log("received init success callback - can now test flags!");
        Debug.LogFormat("Test active? {0}", s_instance.IsFlagActive("test"));
        Debug.LogFormat("Test payload? {0}", s_instance.GetFlagPayload("test"));
        Debug.LogFormat("Bob active? {0}", s_instance.IsFlagActive("test2"));
        Debug.LogFormat("Bob payload? {0}", s_instance.GetFlagPayload("test2"));

        s_instance.AddEvent("empty_event");
        s_instance.AddEvent("integer_event", new Dictionary<string, int> { { "fred", 123 } });
        s_instance.AddEvent("complex_event", new Dictionary<string, object> { { "fred", 123 }, { "fred2", "hello" } });
    }
}