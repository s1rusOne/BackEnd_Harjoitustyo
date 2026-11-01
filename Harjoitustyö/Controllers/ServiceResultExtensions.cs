using Harjoitustyö.Services;
using Microsoft.AspNetCore.Mvc;

namespace Harjoitustyö.Controllers
{
    public static class ServiceResultExtensions
    {
        public static ActionResult ErrorResult<T>(this ControllerBase controller, ServiceResult<T> result)
        {
            return result.Status switch
            {
                ServiceStatus.NotFound => controller.NotFound(),
                ServiceStatus.Forbidden => controller.StatusCode(StatusCodes.Status403Forbidden, result.Error),
                ServiceStatus.BadRequest => controller.BadRequest(result.Error),
                _ => controller.StatusCode(StatusCodes.Status500InternalServerError)
            };
        }
    }
}
