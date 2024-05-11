using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

using SimpleJSON;
using UnityEditorInternal;

namespace Platoon
{
    public class PlatoonSDK
    {
        private bool _active = false;
        private MonoBehaviour _parent;
        private string _accessToken;
        private JSONArray _eventBuffer;
        private int _eventMaxToBuffer = 50;
        private JSONObject _commonPayload = new() { };
        private JSONObject _initPayload = new() { };
        private IEnumerator _heartbeatCoroutine;
        private int _heartbeatFrequency = 20;

        private delegate void Callback(string data);

        // Public interface
        public string BaseUrl { get; set; }
        public PlatoonSDK(MonoBehaviour parent, string accessToken, bool active)
        {
            BaseUrl = "https://platoon.cc";
            this._parent = parent;
            this._accessToken = accessToken;
            Debug.Log("Platoon: Opening");

            _initPayload.Add("version", Application.version);
            _initPayload.Add("platform", Application.platform.ToString());
            _initPayload.Add("device", SystemInfo.deviceModel);
            _initPayload.Add("os", SystemInfo.operatingSystem);
            _initPayload.Add("sdk", Version.SDK);

            this.Activate(active);
        }

        public void Close()
        {
            _initPayload = null;
            Debug.Log("Platoon: Closing");
            AddEvent("$sessionEnd");
            SendEvents();
            this.Activate(false);
        }

        public void SetUserID(string _userId)
        {
            _commonPayload.Add("user_id", _userId);
        }

        public void SetCustomSessionData(string key, object value)
        {
            _initPayload.Add(key, JSON.ToJSONNode(value));
        }

        public void StartSession()
        {
            Debug.Log(_initPayload.ToString());

            var newEvent = new JSONObject
            {
                { "user_id", _commonPayload["user_id"] },
                { "payload", _initPayload }
            };
            var data = newEvent.ToString();
            Debug.LogFormat("Platoon: Sending init {0}", data);
            _parent.StartCoroutine(Post("api/init", data, CallbackInit));
        }

        void CallbackInit(string data)
        {
            Debug.Log("Callback received: " + data);
            var parsed = JSON.Parse(data);
            // var server_ts = parsed["server_ts"];
            // Debug.Log(server_ts);
            _commonPayload.Add("session_id", parsed["session_id"]);
        }

        public void AddEvent(string name)
        {
            if (_active)
            {
                var newEvent = _commonPayload.Clone();
                newEvent.Add("event", name);
                newEvent.Add("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                _eventBuffer.Add(newEvent);
                if (_eventBuffer.Count >= _eventMaxToBuffer)
                {
                    SendEvents();
                }
            }
        }

        public void AddEvent(string name, Dictionary<string, object> payload)
        {
            if (_active)
            {
                var newEvent = _commonPayload.Clone();
                newEvent.Add("event", name);
                newEvent.Add("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                newEvent.Add("payload", payload.ToJSONNode());
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

        private void Activate(bool enable)
        {
            if (enable != _active)
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
                _active = enable;
            }
        }

        private void SendEvents()
        {
            if (_active)
            {
                if (_eventBuffer.Count > 0)
                {
                    Debug.LogFormat("Platoon: Sending {0} events", _eventBuffer.Count);
                    var data = _eventBuffer.ToString();
                    _parent.StartCoroutine(Post("api/ingest", data));

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

        private IEnumerator Post(string uri, string data, Callback cb = null)
        {
            using UnityWebRequest webRequest = CreatePost(uri, data);
            yield return webRequest.SendWebRequest();

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError("Platoon: Error: " + webRequest.error);
                    Debug.Log("Disabling any further sending");
                    this.Activate(false);
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError("Platoon: HTTP Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.Success:
                    cb?.Invoke(webRequest.downloadHandler.text);
                    break;
            }
        }

        private UnityWebRequest CreatePost(string uri, string data)
        {
            string url = BaseUrl + "/" + uri;
            byte[] postRaw = Encoding.UTF8.GetBytes(data);
            var request = new UnityWebRequest(url, "POST")
            {
                downloadHandler = new DownloadHandlerBuffer(),
                uploadHandler = new UploadHandlerRaw(postRaw)
                {
                    contentType = "application/json"
                }
            };
            request.SetRequestHeader("X-API-KEY", _accessToken);
            return request;
        }

    }
}
