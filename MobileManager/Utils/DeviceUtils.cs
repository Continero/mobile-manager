﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MobileManager.Http.Clients;
using MobileManager.Http.Clients.Interfaces;
using MobileManager.Logging.Logger;
using MobileManager.Models.Devices;
using MobileManager.Models.Devices.Enums;
using MobileManager.Models.Devices.Interfaces;
using MobileManager.Models.Reservations.enums;
using MobileManager.Models.Reservations.Interfaces;
using MobileManager.Services;
using MobileManager.Services.Interfaces;
using Newtonsoft.Json;

namespace MobileManager.Utils
{
    /// <inheritdoc />
    /// <summary>
    /// Device utils.
    /// </summary>
    public class DeviceUtils : IDeviceUtils
    {
        private readonly IManagerLogger _logger;
        private readonly IExternalProcesses _externalProcesses;


        /// <summary>
        ///
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="externalProcesses"></param>
        public DeviceUtils(IManagerLogger logger, IExternalProcesses externalProcesses)
        {
            _logger = logger;
            _externalProcesses = externalProcesses;
        }

        /// <inheritdoc />
        /// <summary>
        /// Locks the device.
        /// </summary>
        /// <returns>The device.</returns>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="restClient">Rest client.</param>
        /// <param name="appiumService">Appium service.</param>
        public async Task<Device> LockDeviceAppium(string deviceId, IRestClient restClient,
            IAppiumService appiumService)
        {
            var device = await SetDeviceAvailabilityToFalse(deviceId, restClient);

            _logger.Debug($"{nameof(LockDeviceAppium)}: device id [{deviceId}] start Appium.");
            try
            {
                device.AppiumEndpoint = Task.Run(() => appiumService.StartAppiumForDeviceId(deviceId)).Result;
            }
            catch (Exception e)
            {
                _logger.Error($"{nameof(LockDeviceAppium)} failed with exception.", e);
                await UnlockDeviceAppium(device.Id, restClient, appiumService);
                throw;
            }

            return await SetDeviceStatusToLocked(deviceId, restClient, device);
        }

        /// <inheritdoc />
        /// <summary>
        /// Locks the device.
        /// </summary>
        /// <returns>The device.</returns>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="restClient">Rest client.</param>
        public async Task<Device> LockDeviceXcuiTest(string deviceId, IRestClient restClient)
        {
            var device = await SetDeviceAvailabilityToFalse(deviceId, restClient);

            _logger.Debug($"{nameof(LockDeviceAppium)}: device id [{deviceId}] for XCUITest.");

            return await SetDeviceStatusToLocked(deviceId, restClient, device);
        }

        /// <inheritdoc />
        /// <summary>
        /// Locks the device.
        /// </summary>
        /// <returns>The device.</returns>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="restClient">Rest client.</param>
        public async Task<Device> LockDeviceManual(string deviceId, IRestClient restClient)
        {
            var device = await SetDeviceAvailabilityToFalse(deviceId, restClient);

            _logger.Debug($"{nameof(LockDeviceAppium)}: device id [{deviceId}] for Manual.");

            return await SetDeviceStatusToLocked(deviceId, restClient, device);
        }

        private async Task<Device> SetDeviceStatusToLocked(string deviceId, IRestClient restClient, Device device)
        {
            _logger.Debug($"{nameof(LockDeviceAppium)}: device id [{deviceId}] set status from [{device.Status}] to LOCKED");

            device.Status = DeviceStatus.Locked;

            var updatedDevice = await restClient.UpdateDevice(device);
            _logger.Debug($"{nameof(LockDeviceAppium)}: updated device [{JsonConvert.SerializeObject(updatedDevice)}]");

            return updatedDevice;
        }

        private async Task<Device> SetDeviceAvailabilityToFalse(string deviceId, IRestClient restClient)
        {
            _logger.Debug($"{nameof(LockDeviceAppium)}: device id [{deviceId}].");

            var device = await restClient.GetDevice(deviceId);

            if (device == null)
            {
                throw new KeyNotFoundException("Failed to find device with id: " + deviceId);
            }

            _logger.Debug(
                $"{nameof(LockDeviceAppium)}: set device id [{deviceId}] available from [{device.Available}] to false.");
            device.Available = false;
            return device;
        }

        /// <inheritdoc />
        /// <summary>
        /// Unlocks the device.
        /// </summary>
        /// <returns>The device.</returns>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="restClient">Rest client.</param>
        /// <param name="appiumService">Appium service.</param>
        public async Task<Device> UnlockDeviceAppium(string deviceId, IRestClient restClient,
            IAppiumService appiumService)
        {
            var device = await SetDeviceAvailabilityToTrue(deviceId, restClient);

            _logger.Debug($"{nameof(UnlockDeviceAppium)}: device id [{deviceId}] stop running Appium");
            if (!await appiumService.StopAppiumForDeviceIdAsync(deviceId))
            {
                return device;
            }

            device.AppiumEndpoint = "";

            return await SetDeviceStatusToOffline(deviceId, restClient, device);
        }

        /// <inheritdoc />
        /// <summary>
        /// Unlocks the device.
        /// </summary>
        /// <returns>The device.</returns>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="restClient">Rest client.</param>
        public async Task<Device> UnlockDeviceXcuiTest(string deviceId, IRestClient restClient)
        {
            var device = await SetDeviceAvailabilityToTrue(deviceId, restClient);

            _logger.Debug($"{nameof(UnlockDeviceAppium)}: device id [{deviceId}] stop XcuiTest.");

            return await SetDeviceStatusToOffline(deviceId, restClient, device);
        }

        /// <inheritdoc />
        /// <summary>
        /// Unlocks the device.
        /// </summary>
        /// <returns>The device.</returns>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="restClient">Rest client.</param>
        public async Task<Device> UnlockDeviceManual(string deviceId, IRestClient restClient)
        {
            var device = await SetDeviceAvailabilityToTrue(deviceId, restClient);

            _logger.Debug($"{nameof(UnlockDeviceAppium)}: device id [{deviceId}] stop Manual.");

            return await SetDeviceStatusToOffline(deviceId, restClient, device);
        }

        private async Task<Device> SetDeviceStatusToOffline(string deviceId, IRestClient restClient, Device device)
        {
            _logger.Debug(
                $"{nameof(UnlockDeviceAppium)}: device id [{deviceId}] set status from [{device.Status}] to OFFLINE");
            device.Status = DeviceStatus.Offline;

            var updatedDevice = await restClient.UpdateDevice(device);

            _logger.Debug($"{nameof(UnlockDeviceAppium)}: updated device [{JsonConvert.SerializeObject(updatedDevice)}]");
            return updatedDevice;
        }

        private async Task<Device> SetDeviceAvailabilityToTrue(string deviceId, IRestClient restClient)
        {
            _logger.Debug($"{nameof(UnlockDeviceAppium)}: device id [{deviceId}].");

            var device = await restClient.GetDevice(deviceId);

            if (device == null)
            {
                throw new KeyNotFoundException("Failed to find device with id: " + deviceId);
            }

            _logger.Debug(
                $"{nameof(UnlockDeviceAppium)}: set device id [{deviceId}] available from [{device.Available}] to true.");
            device.Available = true;
            return device;
        }

        /// <inheritdoc />
        /// <summary>
        /// Finds the matching device.
        /// </summary>
        /// <returns>The matching device.</returns>
        /// <param name="requestedDevice">Requested device.</param>
        /// <param name="restClient">Rest client.</param>
        public async Task<Device> FindMatchingDevice(RequestedDevices requestedDevice, IRestClient restClient)
        {
            if (requestedDevice.DeviceId != null)
            {
                return await restClient.GetDevice(requestedDevice.DeviceId);
            }

            if (requestedDevice.DeviceType != DeviceType.Unspecified)
            {
                var allDevices = await restClient.GetDevices();
                var matchingDevices =
                    allDevices.Where((dev) => dev.Type == requestedDevice.DeviceType).ToList();
                return SelectRandomDevice(matchingDevices);
            }

            if (!string.IsNullOrEmpty(requestedDevice.DeviceName))
            {
                var allDevices = await restClient.GetDevices();
                var matchingDevices =
                    allDevices.Where((dev) => dev.Name == requestedDevice.DeviceName).ToList();
                return SelectRandomDevice(matchingDevices);
            }

            if (requestedDevice.Properties.Any())
            {
                var allDevices = await restClient.GetDevices();
                var matchingDevices = new List<Device>();

                foreach (var device in allDevices)
                {
                    var match = false;
                    foreach (var requestedDeviceProperty in requestedDevice.Properties)
                    {
                        if (requestedDeviceProperty.Value.Contains('*') || requestedDeviceProperty.Value.Contains('?'))
                        {
                            match = false;
                            foreach (var prop in device.Properties)
                            {
                                if (prop.Key == requestedDeviceProperty.Key)
                                {
                                    var regex = WildCardToRegular(requestedDeviceProperty.Value);
                                    if (Regex.IsMatch(prop.Value, regex))
                                    {
                                        match = true;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            match = device.Properties.Any(prop =>
                                prop.Key == requestedDeviceProperty.Key && prop.Value == requestedDeviceProperty.Value);
                        }
                    }

                    if (match)
                    {
                        matchingDevices.Add(device);
                    }
                }

                return SelectRandomDevice(matchingDevices);
            }

            return null;
        }

        /// <summary>
        /// Selects the random device.
        /// </summary>
        /// <returns>The random device.</returns>
        /// <param name="devices">Devices.</param>
        private static Device SelectRandomDevice(IEnumerable<Device> devices)
        {
            return devices.OrderBy(x => Guid.NewGuid()).FirstOrDefault();
        }

        /// <inheritdoc />
        /// <summary>
        /// Restarts the device.
        /// </summary>
        /// <returns>The device.</returns>
        /// <param name="device">Device.</param>
        public string RestartDevice(IDevice device)
        {
            var deviceRestartOutput = string.Empty;

            switch (device.Type)
            {
                case DeviceType.IOS:
                {
                    deviceRestartOutput =
                        _externalProcesses.RunProcessAndReadOutput("idevicediagnostics",
                            "-u " + device.Id + " restart");

                    if (string.Equals(deviceRestartOutput, "Restarting device.\n"))
                    {
                        return string.Empty;
                    }

                    break;
                }
                case DeviceType.Android:
                {
                    deviceRestartOutput =
                        _externalProcesses.RunProcessAndReadOutput("adb", "-s " + device.Id + " reboot");

                    //todo: check output for android
                    if (string.Equals(deviceRestartOutput, "Restarting device."))
                    {
                        return string.Empty;
                    }

                    break;
                }
                case DeviceType.Unspecified:
                    break;
                default:
                    throw new NotImplementedException(string.Format("Restart for device type [{0}] not implemented.",
                        device.Type));
            }

            return deviceRestartOutput;
        }

        /// <summary>
        /// https://stackoverflow.com/questions/30299671/matching-strings-with-wildcard
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string WildCardToRegular(string value)
        {
            return $@"^{Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*")}$";
        }


        /// <inheritdoc />
        /// <summary>
        /// Checks status of connected device ids with stored devices in database - changes status accordingly.
        /// </summary>
        /// <param name="checkedDeviceIds">Ids of devices which are currently connected.</param>
        /// <param name="deviceType">Type of devices for which we have ids.</param>
        /// <param name="restClient"><see cref="T:MobileManager.Http.Clients.Interfaces.IRestClient" />.</param>
        /// <returns><see cref="T:System.Threading.Tasks.Task" />.</returns>
        public async Task CheckAllDevicesInDevicePoolAreOnline(IReadOnlyCollection<string> checkedDeviceIds,
            DeviceType deviceType, IRestClient restClient)
        {
            var devicesInPool = await restClient.GetDevices();
            _logger.Debug(
                $"{nameof(CheckAllDevicesInDevicePoolAreOnline)}: devicesInPool [{JsonConvert.SerializeObject(devicesInPool)}]");

            var checkedDevices = devicesInPool.Where(dev => dev.Type == deviceType);
            _logger.Debug(
                $"{nameof(CheckAllDevicesInDevicePoolAreOnline)}: checkedDevices [{JsonConvert.SerializeObject(checkedDevices)}]");


            foreach (var device in checkedDevices)
            {
                if (checkedDeviceIds.All(id => id != device.Id) && device.Status != DeviceStatus.Locked &&
                    device.Status != DeviceStatus.LockedOffline)
                {
                    _logger.Debug(
                        $"{nameof(CheckAllDevicesInDevicePoolAreOnline)}: change device status from [{device.Status}] to [{nameof(DeviceStatus.Offline)}].");
                    device.Status = DeviceStatus.Offline;
                    device.Available = false;
                    await restClient.UpdateDevice(device);
                }
                else if (checkedDeviceIds.All(id => id != device.Id) &&
                         device.Status == DeviceStatus.Locked)
                {
                    _logger.Debug(
                        $"{nameof(CheckAllDevicesInDevicePoolAreOnline)}: change device status from [{device.Status}] to [{nameof(DeviceStatus.Offline)}].");
                    device.Status = DeviceStatus.LockedOffline;
                    await restClient.UpdateDevice(device);
                }
                else if (checkedDeviceIds.Any(id => id == device.Id) && device.Status == DeviceStatus.LockedOffline)
                {
                    _logger.Debug(
                        $"{nameof(CheckAllDevicesInDevicePoolAreOnline)}: change device status from [{device.Status}] to [{nameof(DeviceStatus.Locked)}].");
                    device.Status = DeviceStatus.Locked;
                    device.Available = false;
                    await restClient.UpdateDevice(device);
                }
                else if (checkedDeviceIds.Any(id => id == device.Id) && device.Status == DeviceStatus.Offline)
                {
                    _logger.Debug(
                        $"{nameof(CheckAllDevicesInDevicePoolAreOnline)}: change device status from [{device.Status}] to [{nameof(DeviceStatus.Online)}].");
                    device.Status = DeviceStatus.Online;
                    device.Available = true;
                    await restClient.UpdateDevice(device);
                }
            }
        }


        /// <inheritdoc />
        /// <exception cref="T:System.Exception">Failure during unlocking the device.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Invalid ReservationType.</exception>
        public async Task UnlockDeviceByReservationType(IReservation reservation, ReservedDevice reserveDevice, IRestClient restClient, IAppiumService appiumService)
        {
            switch (reservation.ReservationType)
            {
                case ReservationType.Appium:
                    _logger.Debug(
                        $"{nameof(UnlockDeviceByReservationType)}: reservation [{reservation.Id}] [{ReservationType.Appium}].");
                    if (!(await UnlockDeviceAppium(reserveDevice.DeviceId, restClient, appiumService))
                        .Available)
                    {
                        throw new Exception(
                            "Applying reservation failed to lock all. ReservationService failed to unlock device " +
                            reserveDevice.DeviceId);
                    }

                    break;
                case ReservationType.XcuiTest:
                    _logger.Debug(
                        $"{nameof(UnlockDeviceByReservationType)}: reservation [{reservation.Id}] [{ReservationType.XcuiTest}].");
                    if (!(await UnlockDeviceXcuiTest(reserveDevice.DeviceId, restClient)).Available)
                    {
                        throw new Exception(
                            "Applying reservation failed to lock all. ReservationService failed to unlock device " +
                            reserveDevice.DeviceId);
                    }

                    break;
                case ReservationType.Manual:
                    _logger.Debug(
                        $"{nameof(UnlockDeviceByReservationType)}: reservation [{reservation.Id}] [{ReservationType.Manual}].");
                    if (!(await UnlockDeviceManual(reserveDevice.DeviceId, restClient)).Available)
                    {
                        throw new Exception(
                            "Applying reservation failed to lock all. ReservationService failed to unlock device " +
                            reserveDevice.DeviceId);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
