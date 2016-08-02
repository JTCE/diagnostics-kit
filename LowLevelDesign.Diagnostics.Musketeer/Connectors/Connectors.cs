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
using System;
using System.Collections.Generic;
using System.Linq;

namespace LowLevelDesign.Diagnostics.Musketeer.Connectors
{
    public interface IMusketeerConnectorFactory
    {
        IMusketeerConnector GetConnector();
    }

    public interface IMusketeerConnector : IDisposable
    {

        bool IsEnabled { get; }

        void SendLogRecord(LogRecord logrec);

        void SendLogRecords(IEnumerable<LogRecord> logrecs);

        bool SupportsApplicationConfigs { get; }

        string[] SendApplicationConfigs(IEnumerable<ApplicationServerConfig> configs);
    }

    public class MusketeerConnectorFactory : IMusketeerConnectorFactory
    {
        private sealed class MultiMusketeerConnector : IMusketeerConnector
        {
            private readonly IMusketeerConnector[] connectors;
            private readonly bool isEnabled;
            private readonly IMusketeerConnector connectorSupportingAppConfigs;

            public MultiMusketeerConnector(IMusketeerConnector[] connectors)
            {
                this.connectors = connectors;
                foreach (var c in connectors) {
                    isEnabled = c.IsEnabled || isEnabled;
                }
                connectorSupportingAppConfigs = connectors.FirstOrDefault(c => c.SupportsApplicationConfigs);
            }

            public bool IsEnabled { get { return true; } }

            public void Dispose()
            {
                foreach (var c in connectors) {
                    c.Dispose();
                }
            }

            public bool SupportsApplicationConfigs { get { return connectorSupportingAppConfigs != null; } }

            public string[] SendApplicationConfigs(IEnumerable<ApplicationServerConfig> configs)
            {
                if (!isEnabled || !SupportsApplicationConfigs) {
                    throw new InvalidOperationException();
                }
                return connectorSupportingAppConfigs.SendApplicationConfigs(configs);
            }

            public void SendLogRecord(LogRecord logrec)
            {
                if (!isEnabled) {
                    throw new InvalidOperationException();
                }
                foreach (var c in connectors) {
                    c.SendLogRecord(logrec);
                }
            }

            public void SendLogRecords(IEnumerable<LogRecord> logrecs)
            {
                if (!isEnabled) {
                    throw new InvalidOperationException();
                }
                foreach (var c in connectors) {
                    c.SendLogRecords(logrecs);
                }
            }
        }

        private readonly IMusketeerConnector connector;

        public MusketeerConnectorFactory()
        {
            var castleConnector = new MusketeerHttpCastleConnector();
            var logstashConnector = new LogStashConnector();

            if (castleConnector.IsEnabled && !logstashConnector.IsEnabled) {
                connector = castleConnector;
            } else if (!castleConnector.IsEnabled && logstashConnector.IsEnabled) {
                connector = logstashConnector;
            } else {
                connector = new MultiMusketeerConnector(new IMusketeerConnector[] { castleConnector, logstashConnector });
            }
        }

        public IMusketeerConnector GetConnector()
        {
            return connector;
        }
    }
}