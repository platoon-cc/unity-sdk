using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;

namespace Platoon {
    public class PlatoonSDK {
        public string BaseUrl {get;set;}
 
        private MonoBehaviour parent;
        private string accessToken;
        private readonly JSONArray eventBuffer;
        private int eventMaxToBuffer = 50;
        private string userId;
        private Dictionary<string, object> userPayload;
        private Dictionary<string, object> sessionPayload;
        private IEnumerator heartbeatCoroutine;
        private int heartbeatFrequency = 20;
        public PlatoonSDK(MonoBehaviour _parent, string _accessToken) {
            BaseUrl = "https://platoon.cc";
            parent = _parent;
            accessToken = _accessToken;
            eventBuffer = new JSONArray();
            Debug.Log("Platoon: Opening");
            StartHeartbeat();
        }

        public void Close() {
            Debug.Log("Platoon: Closing");
            EndHeartbeat();
            AddEvent("$sessionEnd");
            SendEvents();
        }

        public void SetUser(string _userId, Dictionary<string, object> payload) {
            userId = _userId;
            userPayload = payload;
            AddEvent("$identify", userPayload);
        }

        public void SetSession(Dictionary<string, object> payload) {
            sessionPayload = payload;
            AddEvent("$sessionBegin", sessionPayload);
        }

        public void AddEvent(string name) {
            eventBuffer.Add(new JSONObject
            {
                { "event", name },
                { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
                { "user_id", userId },
            });
            if (eventBuffer.Count >= eventMaxToBuffer) {
                SendEvents();
            }
        }

        public void AddEvent(string name, Dictionary<string, object> payload) {
            eventBuffer.Add(new JSONObject
            {
                { "event", name },
                { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
                { "user_id", userId },
                { "payload", payload.ToJSONNode() }
            });
            if (eventBuffer.Count >= eventMaxToBuffer) {
                SendEvents();
            }
        }

        private void SendEvents() {
            if (eventBuffer.Count > 0) {
                var json = eventBuffer.ToString();
                Debug.LogFormat("Platoon: Sending {0} events: {1}", eventBuffer.Count, json);
                parent.StartCoroutine(PostRequest("api/ingest", json));
                // TODO : generally better approach to dealing with memory - freelists etc
                // Probably some sort of double-buffering too, in case the send fails
                eventBuffer.Clear();
            }
        }

        private IEnumerator PostRequest(string uri, string data) {
            string endPoint = BaseUrl + "/" + uri;
            using (UnityWebRequest webRequest = CreatePost(endPoint, data, "application/json")) {
                webRequest.SetRequestHeader("X-API-KEY", accessToken);
                yield return webRequest.SendWebRequest();
                
                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError("Platoon: Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError("Platoon: HTTP Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.Success:
                        Debug.Log("Platoon: Received: " + webRequest.downloadHandler.text);
                        break;
                }
            }
        }

        private UnityWebRequest CreatePost(string url, string postData, string contentType)
        {
            var request = new UnityWebRequest(url, "POST");
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", contentType);
            
            byte[] postRaw = Encoding.UTF8.GetBytes(postData);
            request.uploadHandler = new UploadHandlerRaw(postRaw);
            request.uploadHandler.contentType = contentType;
            return request;
        }

        private IEnumerator Heartbeat()
        {
            yield return new WaitForSecondsRealtime(heartbeatFrequency);
            Debug.Log("Platoon: Heartbeat");
            SendEvents();
        }        

        private void StartHeartbeat() {
            heartbeatCoroutine = Heartbeat();
            parent.StartCoroutine(heartbeatCoroutine);
        }

        private void EndHeartbeat()
        {
            if (heartbeatCoroutine != null)
            {
                parent.StopCoroutine(heartbeatCoroutine);
                heartbeatCoroutine = null;
            }
        }

    }
}