using System;
using System.Collections.Generic;
using UnityEngine;

namespace Platoon {
    [DisallowMultipleComponent]
    public class PlatoonManager : MonoBehaviour {
        public bool debugMode = false;
        public string debugUrl = "http://localhost:9998";
        public string accessToken = "";

        PlatoonSDK s_instance;

        public void Start() {
            Debug.Log("Starting PlatoonManager");
            s_instance = new PlatoonSDK(this, accessToken);
            if (debugMode) {
                s_instance.BaseUrl = debugUrl;
            }

            s_instance.SetUser("steam#40", new Dictionary<string, object> {
                {"name", "fred"},
                {"payment_tier", "none"}
            });

            s_instance.SetSession(new Dictionary<string, object> {
                {"branch", "developer"},
                {"vendor", "steam"},
                {"version", "0.1.6669"}
            });
        }

        // TODO : Do we ned to deal with OnAPplicationFocus & OnApplicationPause?
        public void OnApplicationQuit() {
            s_instance.Close();
        }
    }
}