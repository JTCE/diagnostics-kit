﻿/**
 *  Part of the Diagnostics Kit
 *
 *  Copyright (C) 2016  Sebastian Solnica
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.

 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 */


using LowLevelDesign.Diagnostics.Commons.Models;
using LowLevelDesign.Diagnostics.LogStash;
using LowLevelDesign.Diagnostics.Musketeer.Config;
using System;
using System.Collections.Generic;
using System.IO;

namespace LowLevelDesign.Diagnostics.Musketeer.Connectors
{
    public sealed class LogStashConnector : IMusketeerConnector
    {
        private readonly Beats beatsClient;

        public LogStashConnector()
        {
            if (MusketeerConfiguration.LogStashUrl != null) {
                beatsClient = new Beats(MusketeerConfiguration.LogStashUrl.Host, MusketeerConfiguration.LogStashUrl.Port,
                    MusketeerConfiguration.LogStashUseSsl, MusketeerConfiguration.LogStashCertThumb);
            } else {
                beatsClient = null;
            }
        }

        public bool IsEnabled { get { return beatsClient != null; } }

        public bool SupportsApplicationConfigs { get { return false; } }

        public string[] SendApplicationConfigs(IEnumerable<ApplicationServerConfig> configs)
        {
            throw new InvalidOperationException();
        }

        public void SendLogRecord(LogRecord logrec)
        {
            var data = new Dictionary<string, object> {
                { "Server", logrec.Server },
                { "ProcessId", logrec.ProcessId },
                { "ThreadId", logrec.ThreadId },
                { "Path", logrec.ApplicationPath },
                { "LogLevel", logrec.LogLevel.ToString() },
                { "Logger", logrec.LoggerName }
            };

            if (logrec.PerformanceData != null) {
                data.Add("PerfData", logrec.PerformanceData);
            }
            if (!string.IsNullOrEmpty(logrec.Message)) {
                data.Add("Message", logrec.Message);
            }

            beatsClient.SendEvent("musketeer", string.Format("Musketeer.{0}", logrec.LoggerName), logrec.TimeUtc, data);
        }

        public void SendLogRecords(IEnumerable<LogRecord> logrecs)
        {
            foreach (var log in logrecs) {
                SendLogRecord(log);
            }
        }

        public void Dispose()
        {
            beatsClient.Dispose();
        }
    }
}
