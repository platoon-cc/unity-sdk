using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

using SimpleJSON;

namespace Platoon
{
    public class PlatoonSDK
    {
        private bool _active = false;
        private bool _ready = false;
        private MonoBehaviour _parent;
        private string _accessToken;
        private int _eventMaxToBuffer = 50;
        private JSONArray _eventBuffer = new() { };
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
        public bool Quiet { get; set; }

        public void _Debug(string logMsg) {
            if(!Quiet)
                Debug.Log(logMsg);
        }

        public void _DebugError(string logMsg) {
            if(!Quiet)
                Debug.LogError(logMsg);
        }

        public void _DebugFormat(string format, params object[] args) {
            if(!Quiet)
                Debug.LogFormat(format, args);
        }

        public PlatoonSDK(MonoBehaviour parent, string accessToken, string userId, bool active)
        {
            BaseUrl = "https://api.platoon.cc";
            this._parent = parent;
            this._accessToken = accessToken;
            _commonPayload.Add("user_id", userId);

            _initPayload.Add("version", Application.version);
            _initPayload.Add("platform", Application.platform.ToString());
            _initPayload.Add("device", SystemInfo.deviceModel);
            _initPayload.Add("os", SystemInfo.operatingSystem);
            _initPayload.Add("sdk", Version.SDK);

            this.Activate(active);
        }

        public void Close()
        {
            _Debug("Platoon: Closing");
            AddEvent("$sessionEnd");
            this.Activate(false);
            SendEvents(false);
            // #if UNITY_STANDALONE
            //             System.Threading.Thread.Sleep(1000);
            // #endif
            _initPayload = null;
            _commonPayload = null;
            _eventBuffer = null;
            _flags = null;
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
                _DebugFormat("Platoon: Sending init {0}", data);
                _parent.StartCoroutine(AsyncRequest("init", data, requestCallbackInit));
            }
        }

        void requestCallbackInit(string data)
        {
            _Debug("Callback received: " + data);
            var parsed = JSON.Parse(data);
            // var server_ts = parsed["server_ts"];
            // _Debug(server_ts);
            _commonPayload.Add("session_id", parsed["session_id"]);

            _flags = parsed["flags"].AsObject;
            _ready = true;
            _Debug(_flags);

            if (_readyCB != null)
            {
                _readyCB();
            }
        }

        public void AddEvent(string name)
        {
            AddEvent<string>(name, null);
        }

        public void AddEvent<T>(string name, Dictionary<string, T> payload)
        {
            if (_active)
            {
                if (!_ready)
                {
                    _DebugError("Adding an event before the session is ready");
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
                    StartHeartbeat();
                }
                else
                {
                    EndHeartbeat();
                }
            }
        }

        private void SendEvents(bool async = true)
        {
            if (_eventBuffer.Count > 0)
            {
                _DebugFormat("Platoon: Sending {0} events", _eventBuffer.Count);
                var data = _eventBuffer.ToString();
                if (async)
                    _parent.StartCoroutine(AsyncRequest("ingest", data));
                else
                    SyncRequest("ingest", data);
                // TODO : generally better approach to dealing with memory - freelists etc
                // Probably some sort of double-buffering too, in case the send fails
                _eventBuffer.Clear();
            }
        }

        private IEnumerator Heartbeat()
        {
            while (_active)
            {
                yield return new WaitForSecondsRealtime(_heartbeatFrequency);
                _Debug("Platoon: Heartbeat");
                SendEvents();
            }
        }

        private IEnumerator AsyncRequest(string uri, string data, requestCallback cb = null)
        {
            using (UnityWebRequest request = SetupRequest(uri, data))
            {
                yield return request.SendWebRequest();
                HandleRequestResponse(request, cb);
            }
        }
        private void SyncRequest(string uri, string data)
        {
            using (UnityWebRequest request = SetupRequest(uri, data))
            {
                request.SendWebRequest();
                while (!request.isDone) { }
                HandleRequestResponse(request);
            }
        }

        private UnityWebRequest SetupRequest(string uri, string data)
        {
            UnityWebRequest request = new UnityWebRequest(BaseUrl + "/" + uri, "POST");
            request.SetRequestHeader("X-API-KEY", _accessToken);
            request.disposeDownloadHandlerOnDispose = true;
            request.disposeUploadHandlerOnDispose = true;
            request.downloadHandler = new DownloadHandlerBuffer();
            if (!string.IsNullOrEmpty(data))
            {
                byte[] postRaw = Encoding.UTF8.GetBytes(data);
                request.uploadHandler = new UploadHandlerRaw(postRaw);
                request.uploadHandler.contentType = "application/json";
            }
            return request;
        }

        private void HandleRequestResponse(UnityWebRequest request, requestCallback cb = null)
        {

            switch (request.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    _DebugError("Platoon: Error: " + request.error);
                    _Debug("Disabling any further sending");
                    this.Activate(false);
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    _DebugError("Platoon: HTTP Error: " + request.error);
                    break;
                case UnityWebRequest.Result.Success:
                    cb?.Invoke(request.downloadHandler.text);
                    break;
            }
        }

    }
}
