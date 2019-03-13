using System.Collections.Generic;
using System.Threading.Tasks;
using MobileManager.Http.Clients.Interfaces;
using MobileManager.Models.Devices;
using MobileManager.Models.Devices.Enums;
using MobileManager.Models.Devices.Interfaces;
using MobileManager.Models.Reservations.Interfaces;
using MobileManager.Services.Interfaces;

namespace MobileManager.Utils
{
    /// <summary>
    /// Device utils.
    /// </summary>
    public interface IDeviceUtils
    {
        /// <summary>
        /// Locks the device for Appium.
        /// </summary>
        /// <returns>The device.</returns>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="restClient">Rest client.</param>
        /// <param name="appiumService">Appium service.</param>
        /// <returns>Updated device.</returns>
        Task<Device> LockDeviceAppium(string deviceId, IRestClient restClient,
            IAppiumService appiumService);

        /// <summary>
        /// Locks the device for XcuiTests.
        /// </summary>
        /// <returns>The device.</returns>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="restClient">Rest client.</param>
        /// <returns>Updated device.</returns>
        Task<Device> LockDeviceXcuiTest(string deviceId, IRestClient restClient);

        /// <summary>
        /// Locks the device for Manual.
        /// </summary>
        /// <returns>The device.</returns>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="restClient">Rest client.</param>
        /// <returns>Updated device.</returns>
        Task<Device> LockDeviceManual(string deviceId, IRestClient restClient);

        /// <summary>
        /// Unlocks the device.
        /// </summary>
        /// <returns>The device.</returns>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="restClient">Rest client.</param>
        /// <param name="appiumService">Appium service.</param>
        /// <returns>Updated device.</returns>
        Task<Device> UnlockDeviceAppium(string deviceId, IRestClient restClient,
            IAppiumService appiumService);

        /// <summary>
        /// Unlocks the device from XcuiTest.
        /// </summary>
        /// <returns>The device.</returns>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="restClient">Rest client.</param>
        /// <returns>Updated device.</returns>
        Task<Device> UnlockDeviceXcuiTest(string deviceId, IRestClient restClient);

        /// <summary>
        /// Unlocks the device from Manual.
        /// </summary>
        /// <returns>The device.</returns>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="restClient">Rest client.</param>
        /// <returns>Updated device.</returns>
        Task<Device> UnlockDeviceManual(string deviceId, IRestClient restClient);

        /// <summary>
        /// Finds the matching device.
        /// </summary>
        /// <returns>The matching device.</returns>
        /// <param name="requestedDevice">Requested device.</param>
        /// <param name="restClient">Rest client.</param>
        Task<Device> FindMatchingDevice(RequestedDevices requestedDevice, IRestClient restClient);

        /// <summary>
        /// Restarts the device.
        /// </summary>
        /// <returns>The device.</returns>
        /// <param name="device">Device.</param>
        string RestartDevice(IDevice device);

        /// <summary>
        /// Checks status of connected device ids with stored devices in database - changes status accordingly.
        /// </summary>
        /// <param name="checkedDeviceIds">Ids of devices which are currently connected.</param>
        /// <param name="deviceType">Type of devices for which we have ids.</param>
        /// <param name="restClient"><see cref="IRestClient"/>.</param>
        /// <returns><see cref="Task"/>.</returns>
        Task CheckAllDevicesInDevicePoolAreOnline(IReadOnlyCollection<string> checkedDeviceIds,
            DeviceType deviceType, IRestClient restClient);

        /// <summary>
        /// Unlocks the device by reservation ReservationType.
        /// </summary>
        /// <param name="reservation">reservation.</param>
        /// <param name="reserveDevice">reserved device.</param>
        /// <param name="restClient"></param>
        /// <param name="appiumService"></param>
        /// <returns></returns>
        Task UnlockDeviceByReservationType(IReservation reservation, ReservedDevice reserveDevice,
            IRestClient restClient, IAppiumService appiumService);
    }
}
