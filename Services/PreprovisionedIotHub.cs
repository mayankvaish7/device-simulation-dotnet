﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IPreprovisionedIotHub
    {
        // Ping the registry to see if the connection is healthy
        Task<Tuple<bool, string>> PingRegistryAsync();
    }

    public class PreprovisionedIotHub : IPreprovisionedIotHub
    {
        private readonly ILogger log;
        private readonly string connectionString;
        private readonly IInstance instance;

        private string ioTHubHostName;
        private RegistryManager registry;

        public PreprovisionedIotHub(
            IServicesConfig config,
            ILogger logger,
            IInstance instance)
        {
            this.log = logger;
            this.connectionString = config.IoTHubConnString;
            this.instance = instance;
        }

        // Ping the registry to see if the connection is healthy
        public async Task<Tuple<bool, string>> PingRegistryAsync()
        {
            var isHealthy = false;
            var message = "IoTHub check failed";
            try
            {
                await this.InitAsync();
                await this.registry.GetDeviceAsync("healthcheck");
                isHealthy = true;
                message = "Alive and Well!";
            }
            catch (Exception e)
            {
                this.log.Error("Device registry test failed", e);
            }

            return new Tuple<bool, string>(isHealthy, message);
        }

        // This call can throw an exception, which is fine when the exception happens during a method
        // call. We cannot allow the exception to occur in the constructor though, because it
        // would break DI.
        private async Task InitAsync()
        {
            if (this.instance.IsInitialized) return;

            this.registry = RegistryManager.CreateFromConnectionString(this.connectionString);
            await this.registry.OpenAsync();

            this.ioTHubHostName = IotHubConnectionStringBuilder.Create(this.connectionString).HostName;
            this.log.Info("Selected active IoT Hub for preprovisioned hub status check", () => new { this.ioTHubHostName });

            this.instance.InitComplete();
        }
    }
}
