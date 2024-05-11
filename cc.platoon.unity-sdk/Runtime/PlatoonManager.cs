using System.Collections.Generic;
using UnityEngine;

namespace Platoon
{
    [DisallowMultipleComponent]
    public class PlatoonManager : MonoBehaviour
    {
        // Activates sending (default true)
        // turning this off allows an integration to leave the event-sending code
        // in place but avoid any sending, processing or memory overhead
        public bool active = true;

        public bool debugMode = false;
        public string debugUrl = "http://localhost:9998";
        public string accessToken = "";

        PlatoonSDK s_instance;

        public void Start()
        {
            Debug.Log("Starting PlatoonManager");
            s_instance = new PlatoonSDK(this, accessToken, active);

            if (debugMode)
            {
                s_instance.BaseUrl = debugUrl;
            }

            s_instance.SetUserID("steam#43");
            s_instance.SetCustomSessionData("name", "bob");
            s_instance.SetCustomSessionData("payment_tier", "none");
            s_instance.SetCustomSessionData("branch", "developer");
            s_instance.SetCustomSessionData("vendor", "steam");
            s_instance.StartSession();
        }

        // TODO : Do we need to deal with OnAPplicationFocus & OnApplicationPause?
        public void OnApplicationQuit()
        {
            s_instance.Close();
        }
    }
}