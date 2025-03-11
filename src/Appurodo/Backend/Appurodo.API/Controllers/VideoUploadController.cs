using System.Globalization;
using Appurodo.API.Entities;
using Appurodo.API.Services;
using Appurodo.Application.UseCases;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Appurodo.API.Controllers;


public class VideoUploadController : ControllerBase
{
	[HttpPost]
	[DisableFormValueModelBinding]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> UploadVideo()
	{
		var multiPartResult = await MultiPartRequestService.Run(Request, HttpContext);

		var formData = new FormData();

		var formValueProvider = new FormValueProvider(BindingSource.Form, new FormCollection(multiPartResult!
		                                                                                     .FormAccumulator
		                                                                                     .GetResults()),
			CultureInfo.CurrentCulture);
		var bindingSuccessful = await TryUpdateModelAsync(formData, prefix: "", valueProvider: formValueProvider);
		if (!bindingSuccessful)
		{
			return BadRequest();
		}
		
		var useCase = new UploadVideoUseCase();
		
		await useCase.Execute();
		
		return Ok();
	}
	
}