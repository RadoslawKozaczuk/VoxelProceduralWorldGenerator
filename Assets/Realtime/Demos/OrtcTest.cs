// -------------------------------------
//  Domain		: IBT / Realtime.co
//  Author		: Nicholas Ventimiglia
//  Product		: Messaging and Storage
//  Published	: 2014
//  -------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Foundation.Tasks;
using Foundation.Terminal;
using Realtime.Messaging;
using UnityEngine;

namespace Realtime.Demos
{
    /// <summary>
    /// Demo Client using the Ortc CLient
    /// </summary>
    [AddComponentMenu("Realtime/Demos/OrtcTest")]
    public class OrtcTest : MonoBehaviour
    {
        /// <summary>
        /// 
        /// </summary>
        public string ClientURL = "http://ortc-developers.realtime.co/server/2.1";

        /// <summary>
        /// Identifies the client.
        /// </summary>
        public string ClientMetaData = "UnityClient1";

        /// <summary>
        /// Idk
        /// </summary>
        public string ClientAnnouncmentSubChannel = string.Empty;

        /// <summary>
        /// Identities your channel group
        /// </summary>
        public string ApplicationKey = "BsnG6J";

        /// <summary>
        /// Send / Subscribe channel
        /// </summary>
        public string Channel = "myChannel";

        /// <summary>
        /// Message Content
        /// </summary>
        public string Message = "This is my message";

        /// <summary>
        /// 15 second inactivity check
        /// </summary>
        public bool Heartbeat = false;

        /// <summary>
        /// For dedicated servers
        /// </summary>
        public bool ClientIsCluster = true;

        /// <summary>
        /// Automatic reconnection
        /// </summary>
        public bool EnableReconnect = true;

        #region auth settings

        // Note : this section should really be handled on a webserver you control. It is here only as education.

        /// <summary>
        /// Important
        /// Dont publish your app with this.
        /// This will allow users to authenticate themselves.
        /// Authentication should take place on your authentication server
        /// </summary>
        public string PrivateKey = "eH4nshYKQMYh";

        /// <summary>
        /// Approved channels
        /// </summary>
        public string[] AuthChannels = { "myChannel", "myChannel:sub" };

        /// <summary>
        /// Permission
        /// </summary>
        public ChannelPermissions[] AuthPermission = { ChannelPermissions.Presence, ChannelPermissions.Read, ChannelPermissions.Write };

        /// <summary>
        /// The token that the client uses to access the Pub/Sub network
        /// </summary>
        public string AuthToken = "UnityClient1";

        /// <summary> 
        /// Only one connection can use this token since it's private for each user
        /// </summary>
        public bool AuthTokenIsPrivate = false;

        /// <summary>
        /// Time to live. Expiration of the authentication token.
        /// </summary>
        public int AuthTTL = 1400;
        #endregion

        private OrtcClient _ortc;

        void Awake()
        {
            TaskManager.ConfirmInit();
        }

        protected void Start()
        {
            LoadFactory();
            LoadCommands();
        }

        void ReadText(string text)
        {
            if (text.StartsWith("."))
            {
                ClientMetaData = text.Replace(".", "");
                _ortc.ConnectionMetadata = ClientMetaData;
                TerminalModel.LogImportant("Name set to " + ClientMetaData);
            }
            else
            {
                if (!_ortc.IsConnected)
                {
                    Debug.LogError("Not Connected");
                }
                else
                {
                    Message = string.Format("{0} : {1}", ClientMetaData, text);
                    Send();
                }
            }
        }
        protected void OnApplicationPause(bool isPaused)
        {
            if (_ortc != null && _ortc.IsConnected)
                _ortc.Disconnect();
        }

        void LoadCommands()
        {
            TerminalModel.Add(new TerminalInterpreter
            {
                Label = "Chat",
                Method = ReadText
            });
            TerminalModel.Add(new TerminalCommand
            {
                Label = "Connect",
                Method = () => StartCoroutine(Connect())
            });

            TerminalModel.Add(new TerminalCommand
            {
                Label = "Disconnect",
                Method = Disconnect
            });

            //

            TerminalModel.Add(new TerminalCommand
            {
                Label = "Subscribe",
                Method = Subscribe
            });

            TerminalModel.Add(new TerminalCommand
            {
                Label = "Unsubscribe",
                Method = Unsubscribe
            });
            TerminalModel.Add(new TerminalCommand
            {
                Label = "Send",
                Method = Send
            });


            TerminalModel.Add(new TerminalCommand
            {
                Label = "Auth",
                Method = () => StartCoroutine(Auth())
            });

            //

            TerminalModel.Add(new TerminalCommand
            {
                Label = "EnablePresence",
                Method = () => StartCoroutine(EnablePresence())
            });


            TerminalModel.Add(new TerminalCommand
            {
                Label = "DisablePresense",
                Method = () => StartCoroutine(DisablePresence())
            });

            TerminalModel.Add(new TerminalCommand
            {
                Label = "Presence",
                Method = () => StartCoroutine(RequestPresence())
            });

            //


        }

        private void LoadFactory()
        {
            try
            {
                // Construct object
                _ortc = new UnityOrtcClient();

                if (_ortc != null)
                {
                    //_ortc.ConnectionTimeout = 10000;

                    // Handlers
                    _ortc.OnConnected += ortc_OnConnected;
                    _ortc.OnDisconnected += ortc_OnDisconnected;
                    _ortc.OnReconnecting += ortc_OnReconnecting;
                    _ortc.OnReconnected += ortc_OnReconnected;
                    _ortc.OnSubscribed += ortc_OnSubscribed;
                    _ortc.OnUnsubscribed += ortc_OnUnsubscribed;
                    _ortc.OnException += ortc_OnException;
                    TerminalModel.LogInput("Ortc Ready");
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogException(ex.InnerException);
            }

            if (_ortc == null)
            {

                TerminalModel.LogError("ORTC object is null");
            }
        }

        #region methods
        private void Log(string text)
        {
            var dt = DateTime.Now;
            const string datePatt = @"HH:mm:ss";

            TerminalModel.Log(String.Format("{0}: {1}", dt.ToString(datePatt), text));
        }

        IEnumerator Auth()
        {

            Log("Posting permissions...");

            var perms = new Dictionary<string, List<ChannelPermissions>>();

            foreach (var authChannel in AuthChannels)
            {
                perms.Add(authChannel, AuthPermission.ToList());
            }

            var task = AuthenticationClient.PostAuthentication(ClientURL,
                ClientIsCluster, AuthToken, AuthTokenIsPrivate, ApplicationKey,
                AuthTTL, PrivateKey, perms);

            yield return StartCoroutine(task.WaitRoutine());

            if (task.IsFaulted)
                TerminalModel.LogError("Unable to post permissions");
            else
            {
                Log("Permissions posted");
            }

        }

        IEnumerator Connect()
        {
            yield return 1;
            // Update URL with user entered text
            if (ClientIsCluster)
            {
                _ortc.ClusterUrl = ClientURL;
            }
            else
            {
                _ortc.Url = ClientURL;
            }

            _ortc.ConnectionMetadata = ClientMetaData;
            _ortc.AnnouncementSubChannel = ClientAnnouncmentSubChannel;
            _ortc.HeartbeatActive = Heartbeat;
            _ortc.EnableReconnect = EnableReconnect;

            Log(String.Format("Connecting to: {0}...", ClientURL));
            _ortc.Connect(ApplicationKey, AuthToken);
        }

        void Disconnect()
        {
            Log("Disconnecting...");
            _ortc.Disconnect();
        }

        void Subscribe()
        {
            Log(String.Format("Subscribing to: {0}...", Channel));

            _ortc.Subscribe(Channel, OnMessageCallback);
        }

        void Unsubscribe()
        {
            Log(String.Format("Unsubscribing from: {0}...", Channel));

            _ortc.Unsubscribe(Channel);

        }

        IEnumerator RequestPresence()
        {
            var task = PresenceClient.GetPresence(ClientURL, ClientIsCluster, AuthToken, ApplicationKey, Channel);

            yield return StartCoroutine(task.WaitRoutine());

            if (task.IsFaulted)
            {
                TerminalModel.LogError(String.Format("Error: {0}", task.Exception.Message));
            }
            else
            {
                var result = task.Result;

                Log(String.Format("Subscriptions {0}", result.Subscriptions));

                if (result.Metadata != null)
                {
                    foreach (var metadata in result.Metadata)
                    {
                        Log(metadata.Key + " - " + metadata.Value);
                    }
                }
            }
        }

        IEnumerator EnablePresence()
        {
            var task = PresenceClient.EnablePresence(ClientURL, ClientIsCluster, ApplicationKey, PrivateKey, Channel, true);

            yield return StartCoroutine(task.WaitRoutine());

            if (task.IsFaulted)
                TerminalModel.LogError(String.Format("Error: {0}", task.Exception.Message));
            else
                Log(task.Result);
        }

        IEnumerator DisablePresence()
        {
            var task = PresenceClient.DisablePresence(ClientURL, ClientIsCluster, ApplicationKey, PrivateKey, Channel);

            yield return StartCoroutine(task.WaitRoutine());

            if (task.IsFaulted)
                TerminalModel.LogError(String.Format("Error: {0}", task.Exception.Message));
            else
                Log(task.Result);

        }


        void Send()
        {  // Parallel Task: Send
            Log("Send:" + Message + " to " + Channel);

            _ortc.Send(Channel, Message);
        }
        #endregion

        #region Events

        private void OnMessageCallback(string channel, string message)
        {
            switch (channel)
            {
                case "ortcClientConnected":
                    Log(String.Format("A client connected: {0}", message));
                    break;
                case "ortcClientDisconnected":
                    Log(String.Format("A client disconnected: {0}", message));
                    break;
                case "ortcClientSubscribed":
                    Log(String.Format("A client subscribed: {0}", message));
                    break;
                case "ortcClientUnsubscribed":
                    Log(String.Format("A client unsubscribed: {0}", message));
                    break;
                default:
                    //Log(String.Format("Received at {0}: {1}", channel, message.Substring(message.Length - 5)));
                    Log(String.Format("[{0}] {1}", channel, message));
                    break;
            }
        }

        private void ortc_OnConnected()
        {
            Log(String.Format("Connected to: {0}", _ortc.Url));
            Log(String.Format("Connection metadata: {0}", _ortc.ConnectionMetadata));
            Log(String.Format("Session ID: {0}", _ortc.SessionId));
            Log(String.Format("Heartbeat: {0}", _ortc.HeartbeatActive ? "active" : "inactive"));
            if (_ortc.HeartbeatActive)
            {
                Log(String.Format("Heartbeat time: {0}    Heartbeat fails: {1}", _ortc.HeartbeatTime, _ortc.HeartbeatFails));
            }

            //  btnDrawCircle.Enabled = true;
            //    btnSendBugilion.Enabled = true;
        }

        private void ortc_OnDisconnected()
        {
            Log("Disconnected");

            //    btnDrawCircle.Enabled = false;
            //    btnSendBugilion.Enabled = false;
        }

        private void ortc_OnReconnecting()
        {
            // Update URL with user entered text
            if (ClientIsCluster)
            {
                Log(String.Format("Reconnecting to: {0}", _ortc.ClusterUrl));
            }
            else
            {
                Log(String.Format("Reconnecting to: {0}", _ortc.Url));
            }
        }

        private void ortc_OnReconnected()
        {
            Log(String.Format("Reconnected to: {0}", _ortc.Url));

            //   btnDrawCircle.Enabled = true;
            //   btnSendBugilion.Enabled = true;
        }

        private void ortc_OnSubscribed(string channel)
        {
            Log(String.Format("Subscribed to: {0}", channel));
        }

        private void ortc_OnUnsubscribed(string channel)
        {
            Log(String.Format("Unsubscribed from: {0}", channel));
        }

        private void ortc_OnException(Exception ex)
        {
            Log(String.Format("Error: {0}", ex.Message));
        }

        #endregion

    }


}
