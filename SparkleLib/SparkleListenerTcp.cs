//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.


using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SparkleLib {

    public class SparkleListenerTcp : SparkleListenerBase {

        private Thread thread;
        
        // these are shared
        private readonly Object mutex = new Object();
        private Socket socket;
        private bool connected;

        public SparkleListenerTcp (Uri server, string folder_identifier) :
            base (server, folder_identifier)
        {
            base.channels.Add (folder_identifier);
            this.socket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.connected = false;
        }


        public override bool IsConnected {
            get {
                //return this.client.IsConnected;
                bool result = false;
                lock (this.mutex) {
                  result = this.connected;
                }
                return result;
            }
        }

        private void SendCommand(TcpMessagePacket pkt)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(TcpMessagePacket));

            XmlSerializerNamespaces emptyNamespace = new XmlSerializerNamespaces();
            emptyNamespace.Add(String.Empty, String.Empty);

            StringBuilder output = new StringBuilder();

            XmlWriter writer = XmlWriter.Create(output,
                new XmlWriterSettings { OmitXmlDeclaration = true });
            serializer.Serialize(writer, pkt, emptyNamespace);
            
            lock (this.mutex) {
                this.socket.Send(Encoding.UTF8.GetBytes(output.ToString()));
            }
        }

        private TcpMessagePacket ReceiveCommand(byte[] input)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(TcpMessagePacket));
            
            MemoryStream ms = new MemoryStream(input);
            
            TcpMessagePacket ret = (TcpMessagePacket) serializer.Deserialize(ms);
            return ret;
        }

        // Starts a new thread and listens to the channel
        public override void Connect ()
        {
            SparkleHelpers.DebugInfo ("ListenerTcp", "Connecting to " + Server.Host);

            base.is_connecting = true;

            this.thread = new Thread (
                new ThreadStart (delegate {
                    try {
                        // Connect and subscribe to the channel
                        int port = Server.Port;
                        if (port < 0) port = 9999;
                        this.socket.Connect (Server.Host, port);
                        lock (this.mutex) {
                            base.is_connecting = false;
                            this.connected = true;
                        }

                        foreach (string channel in base.channels) {
                            SparkleHelpers.DebugInfo ("ListenerTcp", "Subscribing to channel " + channel);
                            this.SendCommand(new TcpMessagePacket(channel, "register"));
                        }

                        byte [] bytes = new byte [4096];

                        // List to the channels, this blocks the thread
                        while (this.socket.Connected) {
                            int bytes_read = this.socket.Receive (bytes);
                            if (bytes_read > 0) {
                                TcpMessagePacket message = this.ReceiveCommand(bytes);
                                SparkleHelpers.DebugInfo ("ListenerTcp", "Update for folder " + message.repo);
                                OnAnnouncement (new SparkleAnnouncement (message.repo, message.readable));
                            } else {
                                SparkleHelpers.DebugInfo ("ListenerTcp", "Error on socket");
                                lock (this.mutex) {
                                    this.socket.Close();
                                    this.connected = false;
                                }
                            }
                        }
                        
                        SparkleHelpers.DebugInfo ("ListenerTcp", "Disconnected from " + Server.Host);
                        
                        // TODO: attempt to reconnect..?
                    } catch (SocketException e) {
                        SparkleHelpers.DebugInfo ("ListenerTcp", "Could not connect to " + Server + ": " + e.Message);
                    }
                })
            );

            this.thread.Start ();
        }


        public override void AlsoListenTo (string folder_identifier)
        {
            string channel = folder_identifier;
            if (!base.channels.Contains (channel)) {
                base.channels.Add (channel);

                if (IsConnected) {
                    SparkleHelpers.DebugInfo ("ListenerTcp", "Subscribing to channel " + channel);

                    this.SendCommand(new TcpMessagePacket(channel, "register"));
                }
            }
        }


        public override void Announce (SparkleAnnouncement announcement)
        {
            this.SendCommand(new TcpMessagePacket(announcement.FolderIdentifier, "new_version"));

            // Also announce to ourselves for debugging purposes
            // base.OnAnnouncement (announcement);
        }


        public override void Dispose ()
        {
            this.thread.Abort ();
            this.thread.Join ();
            base.Dispose ();
        }
    }
    
    [Serializable,XmlRoot("packet")]
    public class TcpMessagePacket 
    {
        public string repo { get; set; }
        public string command { get; set; }
        public string readable { get; set; }
        
        public TcpMessagePacket(string repo, string command) {
            this.repo = repo;
            this.command = command;
        }
        
        public TcpMessagePacket() {
            this.repo = "none";
            this.command = "invalid";
        }
    }
}
