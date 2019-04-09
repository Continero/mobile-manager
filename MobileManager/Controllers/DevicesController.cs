using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MobileManager.Configuration.Interfaces;
using MobileManager.Controllers.Interfaces;
using MobileManager.Database.Repositories.Interfaces;
using MobileManager.Logging.Logger;
using MobileManager.Models.Devices;
using MobileManager.Models.Devices.Enums;
using MobileManager.Models.Devices.Interfaces;
using MobileManager.Services;
using MobileManager.Utils;
using Newtonsoft.Json;

namespace MobileManager.Controllers
{
    /// <inheritdoc cref="IDeviceController" />
    /// <summary>
    /// Devices controller.
    /// </summary>
    [Route("api/v1/device")]
    [EnableCors("AllowAllHeaders")]
    public class DevicesController : ControllerExtensions, IDeviceController
    {
        private readonly IRepository<Device> _devicesRepository;
        private readonly IManagerLogger _logger;
        private readonly IManagerConfiguration _configuration;
        private readonly IScreenshotService _screenshotService;
        private readonly IDeviceUtils _deviceUtils;
        private readonly IExternalProcesses _externalProcesses;


        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:MobileManager.Controllers.DevicesController" /> class.
        /// </summary>
        /// <param name="devicesRepository"><see cref="IRepository{T}"/> Device.</param>
        /// <param name="logger"><see cref="IManagerLogger"/></param>
        /// <param name="configuration"><see cref="IManagerConfiguration"/></param>
        /// <param name="screenshotService"><see cref="IScreenshotService"/></param>
        /// <param name="externalProcesses"><see cref="IExternalProcesses"/></param>
        /// <param name="deviceUtils"><see cref="IDeviceUtils"/></param>
        public DevicesController(IRepository<Device> devicesRepository, IManagerLogger logger,
            IManagerConfiguration configuration, IDeviceUtils deviceUtils,
            IScreenshotService screenshotService, IExternalProcesses externalProcesses) : base(logger)
        {
            _devicesRepository = devicesRepository;
            _logger = logger;
            _configuration = configuration;
            _deviceUtils = deviceUtils ?? new DeviceUtils(_logger, _externalProcesses);
            _screenshotService = screenshotService ?? new ScreenshotService(_logger, _externalProcesses);
            _externalProcesses = externalProcesses ?? new ExternalProcesses(_logger);
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets all active devices.
        /// </summary>
        /// <returns>Active devices.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Device>), StatusCodes.Status200OK)]
        public IEnumerable<Device> GetAll()
        {
            var devices = _devicesRepository.GetAll();
            _logger.Debug(string.Format("GetAll devices: [{0}]", JsonConvert.SerializeObject(devices)));
            return devices;
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets device the by identifier.
        /// </summary>
        /// <returns>The device identifier.</returns>
        /// <param name="id">Identifier.</param>
        [HttpGet("{id}", Name = "getDevice")]
        [ProducesResponseType(typeof(Device), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public IActionResult GetById(string id)
        {
            var device = _devicesRepository.Find(id);
            if (device == null)
            {
                return NotFoundExtension("Device not found in database.");
            }

            return JsonExtension(device);
        }

        /// <summary>
        /// Gets device properties the by identifier.
        /// </summary>
        /// <returns>The device properties.</returns>
        /// <param name="id">Identifier.</param>
        [HttpGet("properties/{id}", Name = "getDeviceProperties")]
        [ProducesResponseType(typeof(List<DeviceProperties>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public IActionResult GetPropertiesById(string id)
        {
            var device = _devicesRepository.Find(id);
            if (device == null)
            {
                return NotFoundExtension("Device not found in database.");
            }

            return JsonExtension(device.Properties);
        }

        /// <summary>
        /// Gets device properties keys.
        /// </summary>
        /// <returns>The device property keys.</returns>
        /// <response code="200">Property keys returned successfully.</response>
        [HttpGet("properties", Name = "getDevicePropertiesKeys")]
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        public IActionResult GetAllPropertiesKeys()
        {
            var devices = _devicesRepository.GetAll();
            var properties = new List<DeviceProperties>();

            foreach (var device in devices)
            {
                properties.AddRange(device.Properties);
            }

            var keys = properties.Select(p => p.Key).Distinct();

            return JsonExtension(keys);
        }

        /// <summary>
        /// Gets device properties the by identifier.
        /// </summary>
        /// <returns>The device properties.</returns>
        /// <param name="id">Identifier.</param>
        [HttpGet("seleniumConfig/{id}", Name = "getDeviceSeleniumConfig")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public IActionResult GetSeleniumConfigById(string id)
        {
            var device = _devicesRepository.Find(id);
            if (device == null)
            {
                return NotFoundExtension("Device not found in database.");
            }

            switch (device.Type)
            {
                case DeviceType.IOS:
                    return JsonExtension(CreateIosSeleniumConfig(device));
                case DeviceType.Android:
                    return JsonExtension(CreateAndroidSeleniumConfig(device));
                case DeviceType.Unspecified:
                    return BadRequestExtension("Unsupported device type");
                default:
                    return BadRequestExtension("Unsupported device type");
            }
        }

        private string CreateIosSeleniumConfig(IDevice device)
        {
            var config = new StringBuilder();
            config.AppendLine($"[mobile-manager-{device.Id}]");
            config.AppendLine(device.AppiumEndpoint != string.Empty ? $"url = {device.AppiumEndpoint}" : $"url = ");
            config.AppendLine($"browserName = safari_mobile");
            config.AppendLine($"platformName = iOS");
            config.AppendLine($"udid = {device.Id}");
            config.AppendLine($"deviceName = \"{device.Name}\"");
            config.AppendLine($"deviceVersion = {device.Properties.First(x => x.Key == "ProductVersion").Value}");
            config.AppendLine($"automationName = XCUITest");
            config.AppendLine($"teamId = {_configuration.IosDeveloperCertificateTeamId}");
            config.AppendLine($"signingId = \"iPhone Developer\"");
            config.AppendLine($"showXcodeLog: true");
            config.AppendLine($"realDeviceLogger = /usr/local/lib/node_modules/deviceconsole/deviceconsole");
            config.AppendLine(
                $"bootstrapPath = /usr/local/lib/node_modules/appium/node_modules/appium-xcuitest-driver/WebDriverAgent");
            config.AppendLine(
                $"agentPath = /usr/local/lib/node_modules/appium/node_modules/appium-xcuitest-driver/WebDriverAgent/WebDriverAgent.xcodeproj");
            config.AppendLine("sessionTimeout = 6000");
            config.AppendLine($"startIWDP: true");

            return config.ToString();
        }

        private static string CreateAndroidSeleniumConfig(IDevice device)
        {
            var config = new StringBuilder();
            config.AppendLine($"[mobile-manager-{device.Id}]");
            config.AppendLine(device.AppiumEndpoint != string.Empty ? $"url = {device.AppiumEndpoint}" : $"url = ");
            config.AppendLine($"browserName = chrome_mobile");
            config.AppendLine($"platformName = android");
            config.AppendLine($"udid = {device.Id}");
            config.AppendLine($"deviceName = \"{device.Name}\" ");
            config.AppendLine($"sessionTimeout = 6000");

            return config.ToString();
        }

        /// <inheritdoc />
        /// <summary>
        /// Create the specified device.
        /// </summary>
        /// <returns>Created device.</returns>
        /// <param name="device">Device.</param>
        [HttpPost]
        [ProducesResponseType(typeof(Device), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult Create([FromBody] Device device)
        {
            if (device == null)
            {
                return BadRequestExtension("Empty device in request");
            }

            if (_devicesRepository.Find(device.Id) != null)
            {
                // 409 = Conflict
                return StatusCodeExtension(409, "Device ID already stored in database.");
            }

            if (string.IsNullOrEmpty(device.Id) || string.IsNullOrEmpty(device.Name))
            {
                return BadRequestExtension("Device Id and Name has to be specified.");
            }

            try
            {
                _devicesRepository.Add(device);
            }
            catch (Exception ex)
            {
                return StatusCodeExtension(500, "Failed to Add device in database. " + ex.Message);
            }

            _logger.Debug(string.Format("Created new device: [{0}]", JsonConvert.SerializeObject(device)));

            return CreatedAtRoute("getDevice", new {id = device.Id}, device);
        }

        /// <inheritdoc />
        /// <summary>
        /// Update the specified device.
        /// </summary>
        /// <returns>The update.</returns>
        /// <param name="id">Identifier.</param>
        /// <param name="deviceUpdated">Device updated.</param>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Device), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult Update(string id, [FromBody] Device deviceUpdated)
        {
            if (deviceUpdated == null || deviceUpdated.Id != id)
            {
                return BadRequestExtension("Empty device in request");
            }

            var device = _devicesRepository.Find(id);
            if (device == null)
            {
                return NotFoundExtension("Device not found in database.");
            }

            //todo: why is this commented out
            /*
            if (!device.Available)
            {
                return StatusCodeExtension(423, "Device is locked.");
            }
            */

            try
            {
                _devicesRepository.Update(deviceUpdated);
            }
            catch (Exception ex)
            {
                return StatusCodeExtension(500, "Failed to Update device in database. " + ex.Message);
            }

            _logger.Debug(
                $"Updated device: [{JsonConvert.SerializeObject(device)}] to [{JsonConvert.SerializeObject(deviceUpdated)}]");


            return JsonExtension(deviceUpdated);
        }

        /// <inheritdoc />
        /// <summary>
        /// Delete the specified device by id.
        /// </summary>
        /// <returns>null</returns>
        /// <param name="id">Device Identifier.</param>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status423Locked)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult Delete(string id)
        {
            var device = _devicesRepository.Find(id);
            if (device == null)
            {
                return NotFoundExtension("Device not found in database.");
            }

            if (device.Status == DeviceStatus.Locked || device.Status == DeviceStatus.LockedOffline)
            {
                return StatusCodeExtension(423, "Device is locked.");
            }

            try
            {
                _devicesRepository.Remove(id);
            }
            catch (Exception ex)
            {
                return StatusCodeExtension(500, "Failed to Remove device from database. " + ex.Message);
            }

            return OkExtension();
        }

        /// <summary>
        /// Restarts the device.
        /// </summary>
        /// <returns>null</returns>
        /// <param name="id">device id</param>
        [HttpPost("{id}/restart")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status423Locked)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult RestartDevice(string id)
        {
            var device = _devicesRepository.Find(id);
            if (device == null)
            {
                return NotFoundExtension("Device not found in database.");
            }

            if (!device.Available)
            {
                return StatusCodeExtension(423, "Device is locked.");
            }

            var restartOutput = _deviceUtils.RestartDevice(device);

            if (!string.IsNullOrEmpty(restartOutput))
            {
                return StatusCodeExtension(500, $"Failed to restart device. [{restartOutput}]");
            }

            return OkExtension();
        }

        /// <summary>
        /// Contains ids of devices currently downloading screenshots from devices.
        /// </summary>
        public readonly List<string> ScreenshotLocked = new List<string>();

        /// <summary>
        /// Time to wait for screenshotLock to get free - in ms.
        /// </summary>
        public int ScreenshotLockedTimeout = 20000;

        /// <summary>
        /// Gets device screenshot the by identifier.
        /// </summary>
        /// <returns>The device screenshot.</returns>
        /// <param name="id">Identifier.</param>
        [HttpGet("{id}/screenshot", Name = "getDeviceScreenshot")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult GetDeviceScreenshotById(string id)
        {
            var device = _devicesRepository.Find(id);

            if (device == null)
            {
                return NotFoundExtension("Device not found in database.");
            }

            if (device.Status == DeviceStatus.Offline || device.Status == DeviceStatus.FailedToInitialize ||
                device.Status == DeviceStatus.LockedOffline)
            {
                try
                {
                    return _screenshotService.LoadScreenshotForOfflineDevice(device);
                }
                catch (Exception e)
                {
                    return StatusCodeExtension(500, e.Message);
                }
            }

            var start = DateTime.Now;
            while (ScreenshotLocked.Contains(device.Id))
            {
                if (start + TimeSpan.FromMilliseconds(ScreenshotLockedTimeout) <= DateTime.Now)
                {
                    throw new TimeoutException($"Too many requests for screenshots on device {device.Id}.");
                }

                Thread.Sleep(100);
            }

            ScreenshotLocked.Add(device.Id);

            try
            {
                switch (device.Type)
                {
                    case DeviceType.IOS:
                        return _screenshotService.TakeScreenshotIosDevice(device);
                    case DeviceType.Android:
                        return _screenshotService.TakeScreenshotAndroidDevice(device);
                    case DeviceType.Unspecified:
                        return BadRequestExtension($"{device.Type} devices are not supported for screenshots.");
                    default:
                        return BadRequestExtension($"{device.Type} devices are not supported for screenshots.");
                }
            }
            catch (Exception e)
            {
                return StatusCodeExtension(500, e.Message);
            }
            finally
            {
                ScreenshotLocked.Remove(device.Id);
            }
        }
    }
}
