﻿using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using MobileManager.Appium;
using MobileManager.Controllers.Interfaces;
using MobileManager.Database.Repositories.Interfaces;
using MobileManager.Http.Clients.Interfaces;
using MobileManager.Logging.Logger;
using Newtonsoft.Json;

namespace MobileManager.Controllers
{
    /// <summary>
    /// Appium controller.
    /// </summary>
    [Route("api/v1/appium")]
    [EnableCors("AllowAllHeaders")]
    public class AppiumController : ControllerExtensions, IAppiumController
    {
        private readonly IRestClient _restClient;
        private readonly IRepository<AppiumProcess> _appiumRepository;
        private readonly IManagerLogger _logger;

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:MobileManager.Controllers.AppiumController" /> class.
        /// </summary>
        /// <param name="restClient">Rest client.</param>
        /// <param name="appiumRepository">Appium repository.</param>
        /// <param name="logger">Logger</param>
        public AppiumController(IRestClient restClient, IRepository<AppiumProcess> appiumRepository, IManagerLogger logger) : base(logger)
        {
            _restClient = restClient;
            _appiumRepository = appiumRepository;
            _logger = logger;
        }

        /// <inheritdoc />
        /// <summary>
        /// Create the specified appiumProcess. [INTERNAL-ONLY]
        /// </summary>
        /// <returns>Created new appium process.</returns>
        /// <param name="appiumProcess">Appium process.</param>
        /// <response code="200">AppiumProcess added.</response>
        /// <response code="400">Empty AppiumProcess in request.</response>
        /// <response code="409">AppiumProcess DeviceId already stored in database.</response>
        /// <response code="500">Failed to Add AppiumProcess in database.</response>
        [HttpPost]
        public IActionResult Create([FromBody] AppiumProcess appiumProcess)
        {
            LogRequestToDebug();

            if (appiumProcess == null)
            {
                return BadRequestExtension("Empty AppiumProcess in request");
            }

            if (_appiumRepository.Find(appiumProcess.DeviceId) != null)
            {
                // 409 = Conflict
                return StatusCodeExtension(409, "AppiumProcess DeviceId already stored in database.");
            }

            try
            {
                _appiumRepository.Add(appiumProcess);
            }
            catch (Exception ex)
            {
                return StatusCodeExtension(500, "Failed to Add AppiumProcess in database. " + ex.Message);
            }

            _logger.Debug(string.Format("Created new appiumProcess: [{0}]", JsonConvert.SerializeObject(appiumProcess)));

            return CreatedAtRoute("getAppiumProcess", new { id = appiumProcess.DeviceId }, appiumProcess);
        }

        /// <inheritdoc />
        /// <summary>
        /// Delete the specified appium process by id. [INTERNAL-ONLY]
        /// </summary>
        /// <returns>null</returns>
        /// <param name="id">Identifier of appium process.</param>
        /// <response code="200">AppiumProcess deleted.</response>
        /// <response code="400">Empty id in request.</response>
        /// <response code="500">Failed to delete AppiumProcess from database.</response>
        [HttpDelete("{id}")]
        public IActionResult Delete(string id)
        {
            LogRequestToDebug();

            var appiumProcess = _appiumRepository.Find(id);
            if (appiumProcess == null)
            {
                return NotFoundExtension("AppiumProcess not found in database.");
            }

            try
            {
                _appiumRepository.Remove(id);
            }
            catch (Exception ex)
            {
                return StatusCodeExtension(500, "Failed to Device reservation from database. " + ex.Message);
            }

            _logger.Debug(string.Format("Removed appiumProcess id: [{0}]", id));

            return OkExtension();
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets all appium processes. [INTERNAL-ONLY]
        /// </summary>
        /// <returns>Active appium processes.</returns>
        /// <response code="200">AppiumProcesses returned successfully.</response>
        [HttpGet]
        public IEnumerable<AppiumProcess> GetAll()
        {
            LogRequestToDebug();

            return _appiumRepository.GetAll();
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets appium process the by identifier. [INTERNAL-ONLY]
        /// </summary>
        /// <returns>Appium process.</returns>
        /// <param name="id">Identifier of appium process.</param>
        /// <response code="200">AppiumProcess returned successfully.</response>
        /// <response code="400">Empty id in request.</response>
        [HttpGet("{id}", Name = "getAppiumProcess")]
        public IActionResult GetById(string id)
        {
            LogRequestToDebug();

            var appiumProcess = _appiumRepository.Find(id);
            if (appiumProcess == null)
            {
                return NotFoundExtension("AppiumProcess not found in database.");
            }
            return JsonExtension(appiumProcess);
        }
    }
}
