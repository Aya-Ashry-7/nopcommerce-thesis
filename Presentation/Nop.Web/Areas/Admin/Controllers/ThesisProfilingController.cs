using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Security;
using Nop.Services.Security;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Infrastructure.Thesis;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class ThesisProfilingController : BaseAdminController
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ThesisProfilingSettings _thesisProfilingSettings;
    private readonly IThesisReviewDatasetService _thesisReviewDatasetService;

    public ThesisProfilingController(IWebHostEnvironment webHostEnvironment,
        ThesisProfilingSettings thesisProfilingSettings,
        IThesisReviewDatasetService thesisReviewDatasetService)
    {
        _webHostEnvironment = webHostEnvironment;
        _thesisProfilingSettings = thesisProfilingSettings;
        _thesisReviewDatasetService = thesisReviewDatasetService;
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_SETTINGS)]
    public virtual async Task<IActionResult> GenerateDataset([FromBody] ThesisDatasetGenerationRequest request)
    {
        if (!IsAllowedEnvironment())
            return Json(new { Succeeded = false, Message = "Thesis profiling is not allowed in the current environment." });

        if (!_thesisProfilingSettings.Enabled || !_thesisProfilingSettings.EnableDatasetTools)
            return Json(new { Succeeded = false, Message = "Thesis profiling dataset tools are disabled." });

        var result = await _thesisReviewDatasetService.GenerateAsync(request);
        return Json(result);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_SETTINGS)]
    public virtual async Task<IActionResult> CleanupDataset([FromBody] ThesisDatasetCleanupRequest request)
    {
        if (!IsAllowedEnvironment())
            return Json(new { Succeeded = false, Message = "Thesis profiling is not allowed in the current environment." });

        if (!_thesisProfilingSettings.Enabled || !_thesisProfilingSettings.EnableDatasetTools)
            return Json(new { Succeeded = false, Message = "Thesis profiling dataset tools are disabled." });

        var result = await _thesisReviewDatasetService.CleanupAsync(request);
        return Json(result);
    }

    protected virtual bool IsAllowedEnvironment()
    {
        var environmentName = _webHostEnvironment.EnvironmentName;

        return _thesisProfilingSettings.AllowedEnvironments?.Any(allowedEnvironment =>
            allowedEnvironment.Equals(environmentName, StringComparison.InvariantCultureIgnoreCase)) == true;
    }
}
