using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceRequestAPI.DTOs;
using ServiceRequestAPI.Services;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;

namespace ServiceRequestAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [SwaggerTag("Service Requests management endpoints")]
    public class ServiceRequestsController : ControllerBase
    {
        private readonly IServiceRequestService _service;
        private readonly ILogger<ServiceRequestsController> _logger;

        public ServiceRequestsController(
            IServiceRequestService service,
            ILogger<ServiceRequestsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Get all service requests
        /// </summary>
        /// <remarks>
        /// Retrieves a list of all service requests in the system.
        /// Requires valid JWT token in Authorization header.
        /// </remarks>
        /// <returns>List of all service requests</returns>
        /// <response code="200">Successfully retrieved all requests</response>
        /// <response code="401">Unauthorized - missing or invalid token</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [SwaggerOperation(Summary = "Get all service requests", Description = "Returns a list of all service requests")]
        [ProducesResponseType(typeof(IEnumerable<ServiceRequestDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<ServiceRequestDto>>> GetAllRequests()
        {
            try
            {
                var requests = await _service.GetAllRequestsAsync();
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAllRequests: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching requests");
            }
        }

        /// <summary>
        /// Get a specific service request by ID
        /// </summary>
        /// <remarks>
        /// Retrieves detailed information about a specific service request.
        /// </remarks>
        /// <param name="id">The ID of the service request</param>
        /// <returns>Service request details</returns>
        /// <response code="200">Successfully retrieved request</response>
        /// <response code="400">Invalid request ID</response>
        /// <response code="401">Unauthorized - missing or invalid token</response>
        /// <response code="404">Request not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get request by ID", Description = "Returns a specific service request")]
        [ProducesResponseType(typeof(ServiceRequestDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ServiceRequestDto>> GetRequestById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("Invalid request ID");

                var request = await _service.GetRequestByIdAsync(id);
                if (request == null)
                    return NotFound($"Request with ID {id} not found");

                return Ok(request);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetRequestById: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching the request");
            }
        }

        /// <summary>
        /// Create a new service request
        /// </summary>
        /// <remarks>
        /// Creates a new service request with the provided details.
        /// Status is automatically set to "Open" for new requests.
        /// 
        /// Sample request:
        ///
        ///     POST /api/servicerequests
        ///     {
        ///        "title": "Network issue",
        ///        "description": "Cannot connect to VPN",
        ///        "createdBy": "admin"
        ///     }
        /// </remarks>
        /// <param name="dto">Service request details</param>
        /// <returns>Created service request with ID</returns>
        /// <response code="201">Service request created successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">Unauthorized - missing or invalid token</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        [SwaggerOperation(Summary = "Create new service request", Description = "Creates a new service request")]
        [ProducesResponseType(typeof(ServiceRequestDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ServiceRequestDto>> CreateRequest(
            [FromBody] CreateServiceRequestDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var createdRequest = await _service.CreateRequestAsync(dto);
                return CreatedAtAction(nameof(GetRequestById), 
                    new { id = createdRequest.Id }, createdRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CreateRequest: {ex.Message}");
                return StatusCode(500, "An error occurred while creating the request");
            }
        }

        /// <summary>
        /// Update an existing service request
        /// </summary>
        /// <remarks>
        /// Updates specified fields of an existing service request.
        /// Only provided fields will be updated (partial update).
        /// 
        /// Sample request:
        ///
        ///     PUT /api/servicerequests/1
        ///     {
        ///        "title": "Updated title",
        ///        "status": "In Progress"
        ///     }
        /// </remarks>
        /// <param name="id">The ID of the service request to update</param>
        /// <param name="dto">Updated service request data</param>
        /// <returns>Updated service request</returns>
        /// <response code="200">Service request updated successfully</response>
        /// <response code="400">Invalid request ID or data</response>
        /// <response code="401">Unauthorized - missing or invalid token</response>
        /// <response code="404">Request not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Update service request", Description = "Updates an existing service request")]
        [ProducesResponseType(typeof(ServiceRequestDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ServiceRequestDto>> UpdateRequest(
            int id,
            [FromBody] UpdateServiceRequestDto dto)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("Invalid request ID");

                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                var updatedRequest = await _service.UpdateRequestAsync(id, dto, username);

                if (updatedRequest == null)
                    return NotFound($"Request with ID {id} not found");

                return Ok(updatedRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdateRequest: {ex.Message}");
                return StatusCode(500, "An error occurred while updating the request");
            }
        }

        /// <summary>
        /// Delete a service request
        /// </summary>
        /// <remarks>
        /// Permanently deletes a service request from the system.
        /// This action cannot be undone.
        /// </remarks>
        /// <param name="id">The ID of the service request to delete</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">Service request deleted successfully</response>
        /// <response code="400">Invalid request ID</response>
        /// <response code="401">Unauthorized - missing or invalid token</response>
        /// <response code="404">Request not found</response>
        /// <response code="500">Internal server error</response>
        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete service request", Description = "Deletes a service request")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DeleteRequest(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("Invalid request ID");

                var result = await _service.DeleteRequestAsync(id);
                if (!result)
                    return NotFound($"Request with ID {id} not found");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in DeleteRequest: {ex.Message}");
                return StatusCode(500, "An error occurred while deleting the request");
            }
        }

        /// <summary>
        /// Get service requests filtered by status
        /// </summary>
        /// <remarks>
        /// Retrieves all service requests with a specific status.
        /// Valid statuses: "Open", "In Progress", "Closed"
        /// </remarks>
        /// <param name="status">The status to filter by</param>
        /// <returns>List of requests matching the status</returns>
        /// <response code="200">Successfully retrieved filtered requests</response>
        /// <response code="400">Status parameter is missing or invalid</response>
        /// <response code="401">Unauthorized - missing or invalid token</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("filter/status")]
        [SwaggerOperation(Summary = "Get requests by status", Description = "Filters service requests by status")]
        [ProducesResponseType(typeof(IEnumerable<ServiceRequestDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<ServiceRequestDto>>> GetRequestsByStatus(
            [FromQuery] string status)
        {
            try
            {
                if (string.IsNullOrEmpty(status))
                    return BadRequest("Status parameter is required");

                var requests = await _service.GetRequestsByStatusAsync(status);
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetRequestsByStatus: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching requests");
            }
        }
    }
}
