using System;
using System.Collections.Generic;
using UnityEngine;

namespace Platoon {
    public class PlatoonManager : MonoBehaviour {
        public bool debugMode = false;
        public string debugUrl = "http://localhost:9999";
        public string accessToken = "";

        PlatoonSDK s_instance;

        public void Start() {
            Debug.Log("Starting PlatoonManager");
            s_instance = new PlatoonSDK
            {
                accessToken = accessToken,
                _parent = this
            };
            if (debugMode) {
                s_instance.BaseUrl = debugUrl;
            }

            s_instance.AddEvent("test", new Dictionary<string, object> {
                {"name", "fred"},
                {"score", new List<int>{1,2}}
            });

            s_instance.AddEvent("test2", new Dictionary<string, object> {
                {"name", "fred"},
                {"score", new List<int>{1,2}}
            });   
        }
    }
}