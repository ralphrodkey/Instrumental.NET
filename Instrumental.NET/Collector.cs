//  Copyright 2014 Bloomerang
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Net.Sockets;
using System.Threading;
using Common.Logging;

namespace Instrumental.NET
{
    class Collector
    {
        private const int MaxBuffer = 5000;
        private const int Backoff = 2;
        private const int MaxReconnectDelay = 15;

        private readonly string _apiKey;
        private readonly BlockingCollection<Tuple<String, AutoResetEvent>> _messages = new BlockingCollection<Tuple<String, AutoResetEvent>>();
        private Tuple<String, AutoResetEvent> _currentCommand;
        private BackgroundWorker _worker;
        private bool _queueFullWarned;
        private static readonly ILog _log = LogManager.GetCurrentClassLogger();

        public Collector(String apiKey)
        {
            _apiKey = apiKey;
        }

        public void SendMessage(String message, bool synchronous)
        {
            if (_worker == null) StartBackgroundWorker();

            if (_messages.Count < MaxBuffer)
            {
                _queueFullWarned = false;
                _log.DebugFormat("Queueing message: {0}", message);
                QueueMessage(message, synchronous);
            }
            else
            {
                if (!_queueFullWarned)
                {
                    _queueFullWarned = true;
                    _log.Warn("Queue full. Dropping messages until there's room.");
                }
                _log.DebugFormat(String.Format("Dropping message: {0}", message));
            }
        }

        private void QueueMessage(string message, bool synchronous)
        {
            if(!synchronous)
                _messages.Add(new Tuple<string, AutoResetEvent>(message, null));
            else
            {
                var handle = new AutoResetEvent(false);
                _messages.Add(new Tuple<string, AutoResetEvent>(message, handle));
                handle.WaitOne();
            }
        }

        private void StartBackgroundWorker()
        {
            _worker = new BackgroundWorker();
            _worker.DoWork += WorkerOnDoWork;
            _worker.RunWorkerAsync();
        }

        private void WorkerOnDoWork(object sender, DoWorkEventArgs doWorkEventArgs)
        {
            while (!_worker.CancellationPending)
            {
                Socket socket = null;
                var failures = 0;

                try
                {
                    socket = Connect();
                    Authenticate(socket);
                    failures = 0;
                    SendQueuedMessages(socket);
                }
                catch (Exception e)
                {
                    _log.Error("An exception occurred", e);
                    if (socket != null)
                    {
                        socket.Disconnect(false);
                        socket = null;
                    }
                    var delay = (int) Math.Min(MaxReconnectDelay, Math.Pow(failures++, Backoff));
                    _log.ErrorFormat("Disconnected. {0} failures in a row. Reconnect in {1} seconds.", failures, delay);
                    Thread.Sleep(delay*1000);
                }
            }
        }

        private void SendQueuedMessages(Socket socket)
        {
            while (!_worker.CancellationPending)
            {
                if (_currentCommand == null) _currentCommand = _messages.Take();
                var message = _currentCommand.Item1;
                var syncHandle = _currentCommand.Item2;

                if(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0)
                    throw new Exception("Disconnected");

                _log.DebugFormat("Sending: {0}", message);
                var data = System.Text.Encoding.ASCII.GetBytes(message + "\n");
                socket.Send(data);
                if (syncHandle != null) syncHandle.Set();
                _currentCommand = null;
            }
        }

        private void Authenticate(Socket socket)
        {
            var buf = new byte[3];
            var data = System.Text.Encoding.ASCII.GetBytes("hello version 1.0\n");
            socket.Send(data);
            data = System.Text.Encoding.ASCII.GetBytes(String.Format("authenticate {0}\n", _apiKey));
            socket.Send(data);
        }

        private static Socket Connect()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _log.Info("Connecting to collector.");
            socket.Connect("collector.instrumentalapp.com", 8000);
            _log.Info("Connected to collector.");
            return socket;
        }
    }
}
