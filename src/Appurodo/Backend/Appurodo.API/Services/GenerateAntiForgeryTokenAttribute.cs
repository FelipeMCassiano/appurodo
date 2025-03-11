using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Appurodo.API.Services;

public class GenerateAntiForgeryTokenAttribute : ResultFilterAttribute
{
	public override void OnResultExecuting(ResultExecutingContext context)
	{
		var antiforgery = context.HttpContext.RequestServices.GetService<IAntiforgery>();
		
		var tokens = antiforgery.GetAndStoreTokens(context.HttpContext);
		
		context.HttpContext.Response.Cookies.Append(
			"RequestVerificationToken"
			, tokens.RequestToken!
			, new CookieOptions(){HttpOnly = false}
			);
	}

	public override void OnResultExecuted(ResultExecutedContext context)
	{
	}
}