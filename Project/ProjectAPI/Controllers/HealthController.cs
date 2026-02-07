using Microsoft.AspNetCore.Mvc;

namespace ProjectAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;

        public HealthController(ILogger<HealthController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult GetHealth()
        {
            _logger.LogInformation("Health check requested");
            
            var health = new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
            };

            return Ok(health);
        }

        [HttpGet("ready")]
        public IActionResult GetReady()
        {
            // This endpoint confirms the application is ready to receive traffic
            return Ok(new
            {
                Status = "Ready",
                Timestamp = DateTime.UtcNow,
                Message = "Application is ready to serve requests"
            });
        }

        [HttpGet("live")]
        public IActionResult GetLive()
        {
            // This endpoint confirms the application is alive
            return Ok(new
            {
                Status = "Alive",
                Timestamp = DateTime.UtcNow,
                Message = "Application is alive and running"
            });
        }
    }
}