using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;

namespace Platoon {
    public class PlatoonSDK {
 
        public MonoBehaviour _parent;
        public string BaseUrl {get;set;} 
        public string accessToken {get;set;} 
        private readonly JSONArray jsonNode;

        public PlatoonSDK() {
            BaseUrl = "https://platoon.cc";
            jsonNode = new JSONArray();
        }

        public void AddEvent(string name, Dictionary<string, object> payload) {
            var node = new JSONObject
            {
                { "event", name },
                { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
                { "user_id", "fred" },
                { "payload", payload.ToJSONNode() }
            };

            //jsonNode.Add(node);
            // Debug.Log(jsonNode.ToString());
            _parent.StartCoroutine(PostRequest("api/ingest", node.ToString()));
            // _parent.StartCoroutine(GetRequest("api/test"));
            //jsonNode.Clear();
        }

        public IEnumerator PostRequest(string uri, string data) {
            string endPoint = BaseUrl + "/" + uri;
            using (UnityWebRequest webRequest = UnityWebRequest.Post(endPoint, data, "application/json")) {
                webRequest.SetRequestHeader("X-API-KEY", accessToken);
                yield return webRequest.SendWebRequest();
            }
        }
        public IEnumerator GetRequest(string uri)
        {
            string endPoint = BaseUrl + "/" + uri;
            Debug.Log("Calling:" + endPoint);
            using (UnityWebRequest webRequest = UnityWebRequest.Get(endPoint))
            {
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();

                string[] pages = uri.Split('/');
                int page = pages.Length - 1;

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError(pages[page] + ": Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.Success:
                        Debug.Log(pages[page] + ":\nReceived: " + webRequest.downloadHandler.text);
                        break;
                }
            }
        }
    }
}