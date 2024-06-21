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
        private bool _ready = false;
        private MonoBehaviour _parent;
        private string _accessToken;
        private JSONArray _eventBuffer;
        private int _eventMaxToBuffer = 50;
        private JSONObject _commonPayload = new() { };
        private JSONObject _initPayload = new() { };
        private JSONObject _flags;
        private bool _flagsRequired = false;
        private IEnumerator _heartbeatCoroutine;
        private int _heartbeatFrequency = 20;

        private delegate void requestCallback(string data);
        public delegate void readyCallback();
        private readyCallback _readyCB;

        // Public interface
        public string BaseUrl { get; set; }
        public PlatoonSDK(MonoBehaviour parent, string accessToken, string userId, bool active)
        {
            BaseUrl = "https://platoon.cc";
            this._parent = parent;
            this._accessToken = accessToken;
            _commonPayload.Add("user_id", userId);
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
            Debug.Log("Platoon: Closing");
            AddEvent("$sessionEnd");
            SendEvents();
            this.Activate(false);
            _initPayload = null;
            _commonPayload = null;
        }

        public void EnableFlags()
        {
            _flagsRequired = true;
        }

        public bool IsReady()
        {
            return _ready;
        }

        public bool IsFlagActive(string flag)
        {
            return _flags.ContainsKey(flag);
        }

        public object GetFlagPayload(string flag)
        {
            JSONNode val;
            if (_flags.TryGetValue(flag, out val))
            {
                return val["payload"];
            }
            return null;
        }

        public void SetCustomSessionData(string key, object value)
        {
            _initPayload.Add(key, JSON.ToJSONNode(value));
        }

        public void StartSession(readyCallback cb)
        {
            if (_active)
            {
                _readyCB = cb;
                _ready = false;

                var newEvent = new JSONObject{
                    { "user_id", _commonPayload["user_id"] },
                    { "payload", _initPayload },
                    { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}
                };

                if (_flagsRequired)
                {
                    newEvent.Add("process_flags", true);
                }

                var data = newEvent.ToString();
                Debug.LogFormat("Platoon: Sending init {0}", data);
                _parent.StartCoroutine(Post("api/init", data, requestCallbackInit));
            }
        }

        void requestCallbackInit(string data)
        {
            Debug.Log("Callback received: " + data);
            var parsed = JSON.Parse(data);
            // var server_ts = parsed["server_ts"];
            // Debug.Log(server_ts);
            _commonPayload.Add("session_id", parsed["session_id"]);

            _flags = parsed["flags"].AsObject;
            _ready = true;
            Debug.Log(_flags);

            if (_readyCB != null)
            {
                _readyCB();
            }
        }

        public void AddEvent(string name)
        {
            AddEvent(name, null);
        }

        public void AddEvent(string name, Dictionary<string, object> payload)
        {
            if (_active)
            {
                if (!_ready)
                {
                    Debug.LogError("Adding an event before the session is ready");
                    return;
                }
                var newEvent = _commonPayload.Clone();
                newEvent.Add("event", name);
                newEvent.Add("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                if (payload != null)
                    newEvent.Add("payload", payload.ToJSONNode());
                _eventBuffer.Add(newEvent);
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
                _active = enable;
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
            while (_active)
            {
                yield return new WaitForSecondsRealtime(_heartbeatFrequency);
                Debug.Log("Platoon: Heartbeat");
                SendEvents();
            }
        }

        private IEnumerator Post(string uri, string data, requestCallback cb = null)
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
