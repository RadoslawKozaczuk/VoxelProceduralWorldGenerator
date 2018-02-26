// -------------------------------------
//  Domain		: IBT / Realtime.co
//  Author		: Nicholas Ventimiglia
//  Product		: Messaging and Storage
//  Published	: 2014
//  -------------------------------------
using System;
using System.Collections;
using Foundation.Tasks;
using Foundation.Terminal;
using Realtime.Messaging;
using UnityEngine;

namespace Realtime.Demos
{
    /// <summary>
    /// Demo Client using the Realtime Client
    /// </summary>
    [AddComponentMenu("Realtime/Demos/RealtimeTest")]
    public class RealtimeTest : MonoBehaviour
    {
        /// <summary>
        /// Identifies the client.
        /// </summary>
        public string ClientMetaData = "UnityClient1";

        /// <summary>
        /// Send / Subscribe channel
        /// </summary>
        public string Channel = "myChannel";

        /// <summary>
        /// Message Content
        /// </summary>
        public string Message = "This is my message";

        // Note : this section should really be handled on a webserver you control. It is here only as education.

        /// <summary>
        /// The token that the client uses to access the Pub/Sub network
        /// </summary>
        public string AuthToken = "UnityClient1";

        /// <summary>
        /// Partition announcements
        /// </summary>
        public string AnnouncementSubChannel = "myChannel";

        /// <summary>
        /// Automatic reconnection
        /// </summary>
        public bool EnableReconnect = true;

        /// <summary>
        /// Permissions used if authenticating
        /// </summary>
        public RealtimePermission[] Permissions =
        {
            new RealtimePermission("myChannel", ChannelPermissions.Read), 
            new RealtimePermission("myChannel", ChannelPermissions.Write), 
            new RealtimePermission("myChannel", ChannelPermissions.Presence), 

        };

        private RealtimeMessenger Messenger { get; set; }

        protected void Start()
        {
            // Bug in Windows Store does not recognize Startup
            TaskManager.ConfirmInit();
            MessengerSettings.Init();

            Debug.Log("Starting Realtime Tests.");
            Message = string.Format("{0} : {1}", ClientMetaData, "Hello World");

            Messenger = new RealtimeMessenger();
            Messenger.OnMessage += OnMessage;
            Messenger.OnConnectionChanged += Messenger_OnConnectionChanged;
            Messenger.OnException += Messenger_OnException;
            Messenger.ConnectionMetadata = ClientMetaData = Application.platform + "-" + UnityEngine.Random.Range(0, 20);

            LoadCommands();

            Debug.Log("Current Id : " + ClientMetaData);
            Debug.Log("Realtime Ready");
        }

        void Messenger_OnConnectionChanged(ConnectionState state)
        {
            TerminalModel.LogImportant(state.ToString());
        }

        void Messenger_OnException(Exception ex)
        {
            Debug.LogException(ex);
        }

        void OnMessage(string channel, string message)
        {
            TerminalModel.Log(String.Format("[{0}] > {1}", channel, message));
        }

        void ReadText(string text)
        {
            if (text.StartsWith("."))
            {
                Messenger.ConnectionMetadata = ClientMetaData = text.Replace(".", "");
                TerminalModel.LogImportant("Name set to " + ClientMetaData);
            }
            else
            {
                if (!Messenger.IsConnected)
                {
                    Debug.LogError("Not Connected");
                }
                else
                {
                    Message = string.Format("{0} : {1}", ClientMetaData, text);
                    StartCoroutine(Send());
                }
            }
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
                Method = () => StartCoroutine(Disconnect())
            });

            //

            TerminalModel.Add(new TerminalCommand
            {
                Label = "Subscribe",
                Method = () => StartCoroutine(Subscribe())
            });

            TerminalModel.Add(new TerminalCommand
            {
                Label = "Unsubscribe",
                Method = () => StartCoroutine(Unsubscribe())
            });
            TerminalModel.Add(new TerminalCommand
            {
                Label = "Send",
                Method = () => StartCoroutine(Send())
            });

            //

            TerminalModel.Add(new TerminalCommand
            {
                Label = "Sub Announcements",
                Method = () => StartCoroutine(SubscribeAnnouncments())
            });

            TerminalModel.Add(new TerminalCommand
            {
                Label = "Un Announcments",
                Method = () => StartCoroutine(UnsubscribeAnnouncments())
            });

            //
            TerminalModel.Add(new TerminalCommand
            {
                Label = "Pause",
                Method = () => StartCoroutine(Pause())
            });

            TerminalModel.Add(new TerminalCommand
            {
                Label = "Resume",
                Method = () => StartCoroutine(Resume())
            });

            //

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

        protected void OnApplicationPause(bool isPaused)
        {
            if (Messenger == null)
                return;

            if (isPaused)
            {
                if (Messenger.IsConnected)
                    Messenger.Pause();
            }
            else
            {
                if (Messenger.IsPaused)
                    Messenger.Resume();
            }
        }


        #region methods

        IEnumerator Auth()
        {
            TerminalModel.Log("Posting Authentication Token");

            Messenger.AuthenticationToken = AuthToken;
            var task = Messenger.PostAuthentication(Permissions);

            yield return StartCoroutine(task.WaitRoutine());

            if (task.IsFaulted)
                TerminalModel.LogError(task.Exception);
            else
            {
                TerminalModel.LogSuccess("Authentication Posted");
            }

        }

        IEnumerator RequestPresence()
        {
            // Authenticate
            TerminalModel.Log("Getting Presence : " + Channel);

            var task = Messenger.GetPresence(AuthToken, Channel);

            yield return StartCoroutine(task.WaitRoutine());

            if (task.IsFaulted)
                TerminalModel.LogError(task.Exception.Message);
            else
            {
                TerminalModel.LogSuccess("Presence Got");

                TerminalModel.Log(String.Format("Subscriptions {0}", task.Result.Subscriptions));
                TerminalModel.Log(String.Format("Metadata {0}", task.Result.Metadata.Count));

                if (task.Result.Metadata != null)
                {
                    foreach (var metadata in task.Result.Metadata)
                    {
                        TerminalModel.Log(metadata.Key + " - " + metadata.Value);
                    }
                }
            }
        }

        IEnumerator EnablePresence()
        {
            TerminalModel.Log("Enabling Presence : " + Channel);

            var task = Messenger.EnablePresence(Channel, true);

            yield return StartCoroutine(task.WaitRoutine());

            if (task.IsFaulted)
                TerminalModel.LogError(task.Exception);
            else
            {
                TerminalModel.LogSuccess("Presence Enabled");
                TerminalModel.Log(task.Result);
            }
        }

        IEnumerator DisablePresence()
        {
            TerminalModel.Log("Disabling Presence : " + Channel);

            var task = Messenger.DisabledPresence(Channel);

            yield return StartCoroutine(task.WaitRoutine());

            if (task.IsFaulted)
                TerminalModel.LogError(task.Exception);
            else
            {
                TerminalModel.LogSuccess("Presence Disabled");
                TerminalModel.Log(task.Result);
            }

        }

        IEnumerator Connect()
        {
            yield return 1;
            TerminalModel.Log("Connect...");

            Messenger.ConnectionMetadata = ClientMetaData;
            Messenger.AuthenticationToken = AuthToken;
            Messenger.AnnouncementSubChannel = AnnouncementSubChannel;
            Messenger.EnableReconnect = EnableReconnect;
            var task = Messenger.Connect();

            yield return StartCoroutine(task.WaitRoutine());

            if (task.IsFaulted)
                TerminalModel.LogError(task.Exception.Message);
            else
                TerminalModel.LogSuccess("Connected !");
        }

        IEnumerator Disconnect()
        {
            yield return 1;
            TerminalModel.Log("Disconnect...");

            var task = Messenger.Disconnect();

            yield return StartCoroutine(task.WaitRoutine());

            if (task.IsFaulted)
                TerminalModel.LogError(task.Exception.Message);
            else
                TerminalModel.LogSuccess("Disconnected !");
        }


        IEnumerator Resume()
        {
            yield return 1;
            TerminalModel.Log("Resume...");

            var task = Messenger.Resume();

            yield return StartCoroutine(task.WaitRoutine());

            if (task.IsFaulted)
                TerminalModel.LogError(task.Exception.Message);
            else
                TerminalModel.LogSuccess("Resumed !");
        }

        IEnumerator Pause()
        {
            yield return 1;
            TerminalModel.Log("Pause...");

            var task = Messenger.Pause();

            yield return StartCoroutine(task.WaitRoutine());

            if (task.IsFaulted)
                TerminalModel.LogError(task.Exception.Message);
            else
                TerminalModel.LogSuccess("Paused !");
        }

        IEnumerator Subscribe()
        {
            yield return 1;
            TerminalModel.Log(String.Format("Subscribe to: {0}...", Channel));

            var task = Messenger.Subscribe(Channel);

            yield return StartCoroutine(task.WaitRoutine());

            if (task.IsFaulted)
                TerminalModel.LogError(task.Exception.Message);
            else
                TerminalModel.LogSuccess("Subscribed !");
        }

        IEnumerator Unsubscribe()
        {
            yield return 1;
            TerminalModel.Log(String.Format("Unsubscribe from: {0}...", Channel));

            var task = Messenger.Unsubscribe(Channel);

            yield return StartCoroutine(task.WaitRoutine());

            if (task.IsFaulted)
                TerminalModel.LogError(task.Exception.Message);
            else
                TerminalModel.LogSuccess("Unsubscribed !");
        }

        IEnumerator Send()
        {
            yield return 1;
            // Parallel Task: Send
            //TerminalModel.Log(string.Format(">> [{0}] {1}",Channel, Message));

            var task = Messenger.Send(Channel, Message);

            yield return StartCoroutine(task.WaitRoutine());

            if (task.IsFaulted)
                TerminalModel.LogError(task.Exception.Message);
            //else
            //    TerminalModel.LogSuccess("Sent !");
        }

        IEnumerator SubscribeAnnouncments()
        {
            if (string.IsNullOrEmpty(AnnouncementSubChannel))
            {
                yield return StartCoroutine(Subscribe("ortcClientConnected"));
                yield return StartCoroutine(Subscribe("ortcClientDisconnected"));
                yield return StartCoroutine(Subscribe("ortcClientSubscribed"));
                yield return StartCoroutine(Subscribe("ortcClientUnsubscribed"));
            }
            else
            {
                yield return StartCoroutine(Subscribe(string.Format("{0}:{1}", "ortcClientConnected", AnnouncementSubChannel)));
                yield return StartCoroutine(Subscribe(string.Format("{0}:{1}", "ortcClientDisconnected", AnnouncementSubChannel)));
                yield return StartCoroutine(Subscribe(string.Format("{0}:{1}", "ortcClientSubscribed", AnnouncementSubChannel)));
                yield return StartCoroutine(Subscribe(string.Format("{0}:{1}", "ortcClientUnsubscribed", AnnouncementSubChannel)));
            }
        }

        IEnumerator Subscribe(string channel)
        {
            yield return 1;
            TerminalModel.Log(String.Format("Subscribe to: {0}...", channel));

            var task = Messenger.Subscribe(channel);

            yield return StartCoroutine(task.WaitRoutine());

            if (task.IsFaulted)
                TerminalModel.LogError(task.Exception.Message);
            else
                TerminalModel.LogSuccess("Subscribed !");

        }

        IEnumerator UnsubscribeAnnouncments()
        {
            if (string.IsNullOrEmpty(AnnouncementSubChannel))
            {
                yield return StartCoroutine(Unsubscribe("ortcClientConnected"));
                yield return StartCoroutine(Unsubscribe("ortcClientDisconnected"));
                yield return StartCoroutine(Unsubscribe("ortcClientSubscribed"));
                yield return StartCoroutine(Unsubscribe("ortcClientUnsubscribed"));
            }
            else
            {
                yield return StartCoroutine(Unsubscribe(string.Format("{0}:{1}", "ortcClientConnected", AnnouncementSubChannel)));
                yield return StartCoroutine(Unsubscribe(string.Format("{0}:{1}", "ortcClientDisconnected", AnnouncementSubChannel)));
                yield return StartCoroutine(Unsubscribe(string.Format("{0}:{1}", "ortcClientSubscribed", AnnouncementSubChannel)));
                yield return StartCoroutine(Unsubscribe(string.Format("{0}:{1}", "ortcClientUnsubscribed", AnnouncementSubChannel)));
            }
        }

        IEnumerator Unsubscribe(string channel)
        {
            yield return 1;
            TerminalModel.Log(String.Format("Unsubscribe from: {0}...", channel));

            var task = Messenger.Unsubscribe(channel);

            yield return StartCoroutine(task.WaitRoutine());

            if (task.IsFaulted)
                TerminalModel.LogError(task.Exception.Message);
            else
                TerminalModel.LogSuccess("Unsubscribed !");
        }
        #endregion
    }
}
