using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MobileManager.Controllers.Interfaces;
using MobileManager.Database.Repositories.Interfaces;
using MobileManager.Logging.Logger;
using MobileManager.Models.Logger;

namespace MobileManager.Controllers
{
    /// <inheritdoc cref="IMmLogController" />
    /// <summary>
    /// MM log controller.
    /// </summary>
    [Route("api/v1/log/")]
    [EnableCors("AllowAllHeaders")]
    public class MMLogController : ControllerExtensions, IMmLogController
    {
        private readonly IRepository<LogMessage> _repository;
        private readonly IManagerLogger _logger;

        /// <inheritdoc />
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="repository">Logger repository.</param>
        /// <param name="logger">Logger.</param>
        public MMLogController(IRepository<LogMessage> repository, IManagerLogger logger) : base(logger)
        {
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// Gets last number of lines from MM log
        /// </summary>
        /// <returns>MM log.</returns>
        /// <param name="numberOfLines">Specify number of last lines to display</param>
        /// <param name="filter">string filter for fulltext search</param>
        [HttpGet("filter", Name = "getMMLog")]
        [ProducesResponseType(typeof(IEnumerable<LogMessage>),StatusCodes.Status200OK)]
        public IActionResult GetLines(int numberOfLines, string filter = "")
        {
            var logMessages = _repository.GetAll().Where(l => l.Message.Contains(filter)).Take(numberOfLines);

            return JsonExtension(logMessages);
        }
    }
}
