using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;
using System.Data;

namespace Platoon
{
    public class PlatoonSDK
    {
        private bool _sendingActive = false;
        private MonoBehaviour _parent;
        private string _accessToken;
        private JSONArray _eventBuffer;
        private int _eventMaxToBuffer = 50;
        private string _userId;
        private Dictionary<string, object> _userPayload;
        private Dictionary<string, object> _sessionPayload;
        private IEnumerator _heartbeatCoroutine;
        private int _heartbeatFrequency = 20;

        // Public interface
        public string BaseUrl { get; set; }
        public PlatoonSDK(MonoBehaviour parent, string accessToken, bool active)
        {
            BaseUrl = "https://platoon.cc";
            this._parent = parent;
            this._accessToken = accessToken;
            Debug.Log("Platoon: Opening");
            this.ActivateSend(active);
        }

        public void Close()
        {
            Debug.Log("Platoon: Closing");
            AddEvent("$sessionEnd");
            SendEvents();
            this.ActivateSend(false);
        }

        public void SetUser(string _userId, Dictionary<string, object> payload)
        {
            this._userId = _userId;
            _userPayload = payload;
            AddEvent("$identify", _userPayload);
        }

        public void SetSession(Dictionary<string, object> payload)
        {
            _sessionPayload = payload;
            AddEvent("$sessionBegin", _sessionPayload);
        }

        public void AddEvent(string name)
        {
            if (_sendingActive)
            {
                _eventBuffer.Add(new JSONObject
                {
                    { "event", name },
                    { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
                    { "user_id", _userId },
                });
                if (_eventBuffer.Count >= _eventMaxToBuffer)
                {
                    SendEvents();
                }
            }
        }

        public void AddEvent(string name, Dictionary<string, object> payload)
        {
            if (_sendingActive)
            {
                _eventBuffer.Add(new JSONObject
                {
                    { "event", name },
                    { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
                    { "user_id", _userId },
                    { "payload", payload.ToJSONNode() }
                });
                if (_eventBuffer.Count >= _eventMaxToBuffer)
                {
                    SendEvents();
                }
            }
        }

        //////////////////////////
        /// Private Implementation
        //////////////////////////
        private void StartHeartbeat()
        {
            _heartbeatCoroutine = Heartbeat();
            _parent.StartCoroutine(_heartbeatCoroutine);
        }

        private void EndHeartbeat()
        {
            if (_heartbeatCoroutine != null)
            {
                _parent.StopCoroutine(_heartbeatCoroutine);
                _heartbeatCoroutine = null;
            }
        }

        private void ActivateSend(bool enable)
        {
            // Debug.LogFormat("ActivateSend {0} {1}", enable, _sendingActive);
            if (enable != _sendingActive)
            {
                if (enable)
                {
                    _eventBuffer = new JSONArray();
                    StartHeartbeat();
                }
                else
                {
                    _eventBuffer = null;
                    EndHeartbeat();
                }
                _sendingActive = enable;
            }
        }

        private void SendEvents()
        {
            if (_sendingActive)
            {
                if (_eventBuffer.Count > 0)
                {
                    var json = _eventBuffer.ToString();
                    Debug.LogFormat("Platoon: Sending {0} events", _eventBuffer.Count);
                    _parent.StartCoroutine(PostRequest("api/ingest", json));
                    // TODO : generally better approach to dealing with memory - freelists etc
                    // Probably some sort of double-buffering too, in case the send fails
                    _eventBuffer.Clear();
                }
            }
        }

        private IEnumerator Heartbeat()
        {
            yield return new WaitForSecondsRealtime(_heartbeatFrequency);
            Debug.Log("Platoon: Heartbeat");
            SendEvents();
        }

        private IEnumerator PostRequest(string uri, string data)
        {
            string endPoint = BaseUrl + "/" + uri;
            using (UnityWebRequest webRequest = CreatePost(endPoint, data, "application/json"))
            {
                webRequest.SetRequestHeader("X-API-KEY", _accessToken);
                yield return webRequest.SendWebRequest();

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError("Platoon: Error: " + webRequest.error);
                        Debug.Log("Disabling any further sending");
                        this.ActivateSend(false);
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

    }
}