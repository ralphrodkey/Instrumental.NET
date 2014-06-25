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
using System.Text.RegularExpressions;
using Common.Logging;

namespace Instrumental.NET
{
    public class Agent
    {
        public bool Enabled { get; set; }
        public bool Synchronous { get; set; }

        private readonly Collector _collector;
        private static readonly ILog _log = LogManager.GetCurrentClassLogger();

        public Agent(String apiKey)
        {
            Enabled = true;
            Synchronous = false;
            _collector = new Collector(apiKey);
        }

        public void Gauge(String metricName, float value, DateTime? time = null)
        {
            try
            {
                if (!Enabled || !ValidateMetricName(metricName)) return;
                var t = time == null ? DateTime.Now : (DateTime)time;
                _collector.SendMessage(String.Format("gauge {0} {1} {2}", metricName, value, t.ToEpoch()), Synchronous);
            }
            catch (Exception e)
            {
                ReportException(e);
            }            
        }

        public void Time(String metricName, Action action, float durationMultiplier = 1)
        {
            var start = DateTime.Now;
            try
            {
                action();
            }
            finally
            {
                var end = DateTime.Now;
                var duration = end - start;
                Gauge(metricName, (float)duration.TotalSeconds * durationMultiplier);
            }
        }

        public void TimeMs(String metricName, Action action)
        {
            Time(metricName, action, 1000);
        }

        public void Increment(String metricName, float value = 1, DateTime? time = null)
        {
            try
            {
                if (!Enabled || !ValidateMetricName(metricName)) return;
                var t = time == null ? DateTime.Now : (DateTime)time;
                _collector.SendMessage(String.Format("increment {0} {1} {2}", metricName, value, t.ToEpoch()), Synchronous);
            }
            catch (Exception e)
            {
                ReportException(e);
            }
        }

        public void Notice(String message, float duration = 0, DateTime? time = null)
        {
            try
            {
                if (!Enabled || !ValidateNote(message)) return;
                var t = time == null ? DateTime.Now : (DateTime)time;
                _collector.SendMessage(String.Format("notice {0} {1} {2}", t.ToEpoch(), duration, message), Synchronous);
            }
            catch (Exception e)
            {
                ReportException(e);
            }
        }

        private static bool ValidateNote(String message)
        {
            var valid = message.IndexOf("\r") == -1 && message.IndexOf("\n") == -1;
            if(!valid) _log.WarnFormat("Invalid notice message: {0}", message);
            return valid;
        }

        private bool ValidateMetricName(String metricName)
        {
            var validMetric = Regex.IsMatch(metricName, @"^[\d\w\-_]+(\.[\d\w\-_]+)+$", RegexOptions.IgnoreCase);

            if (validMetric) return true;

            Increment("agent.invalid_metric");
            _log.WarnFormat("Invalid metric name: {0}", metricName);

            return false;
        }

        private void ReportException(Exception e)
        {
            _log.Error("An exception occurred", e);
        }

    }
}
