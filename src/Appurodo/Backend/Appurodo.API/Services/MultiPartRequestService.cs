using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Appurodo.API.Entities;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using ContentDispositionHeaderValue = Microsoft.Net.Http.Headers.ContentDispositionHeaderValue;
using MediaTypeHeaderValue = Microsoft.Net.Http.Headers.MediaTypeHeaderValue;

namespace Appurodo.API.Services;

public static class MultiPartRequestService
{
	private static string GetBoundary(MediaTypeHeaderValue contentType, int lengthLimit)
	{
		var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;
		if (string.IsNullOrWhiteSpace(boundary))
		{
			// remove throws later
			throw new Exception("Missing boundary.");
		}

		if (boundary.Length > lengthLimit)
		{
			// remove throws later
			throw new Exception($"Multipart boundary length limit {lengthLimit} exceeded.");
		}
		return boundary;
	}

	private static bool IsMultipartContentType(string contentType)
	{
		return !string.IsNullOrWhiteSpace(contentType) && contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static bool HasFormDataContentDisposition(ContentDispositionHeaderValue contentDisposition)
	{
		return contentDisposition != null && contentDisposition.DispositionType.Equals("form-data") && !string.IsNullOrWhiteSpace(contentDisposition.FileName.Value) && !string.IsNullOrWhiteSpace(contentDisposition.FileNameStar.Value);
	}

	private static bool HasFileContentDisposition(ContentDispositionHeaderValue contentDisposition)
	{
		return contentDisposition != null &&  contentDisposition.DispositionType.Equals("form-data") && (!string.IsNullOrWhiteSpace(contentDisposition.FileName.Value) || !string.IsNullOrWhiteSpace(contentDisposition.FileNameStar.Value));
	}

	private static Encoding? GetEncoding(MultipartSection section)
	{
		var contentType = section.ContentType;

    if (string.IsNullOrEmpty(contentType))
    {
        return null;
    }

    var contentTypeValue = MediaTypeHeaderValue.Parse(contentType);
    var charset = contentTypeValue.Charset.ToString();

    if (string.IsNullOrEmpty(charset))
    {
        return null;
    }

    return Encoding.GetEncoding(charset);
	}

	public static async Task<MultiPartResult?> Run(HttpRequest request, HttpContext httpContext)
	{
		if (!IsMultipartContentType(request.ContentType!))
		{
			return null;
		}

		var formAccumulator = new KeyValueAccumulator();
		var trustedFileNameForDisplay = string.Empty;
		var untrustedFileNameForStorage = string.Empty;
		var streamedFileContent = Array.Empty<byte>();
		var boundary = GetBoundary(MediaTypeHeaderValue.Parse(request.ContentType), 10000);
		var reader = new MultipartReader(boundary, httpContext.Request.Body);
		var section = await reader.ReadNextSectionAsync();

		while (section != null)
		{
			var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition,
				out ContentDispositionHeaderValue? contentDisposition);

			if (hasContentDispositionHeader)
			{
				if (HasFormDataContentDisposition(contentDisposition!))
				{
					// change this
					return null;
				}

				untrustedFileNameForStorage = contentDisposition!.FileName.Value;
				trustedFileNameForDisplay = WebUtility.UrlDecode(untrustedFileNameForStorage);
				// change this later
				streamedFileContent = await FileService.ProcessStreamedFile(section, contentDisposition, 
					permittedExtensions: ["1"], 100000); 
			}

			if (HasFormDataContentDisposition(contentDisposition!))
			{
				var key = HeaderUtilities.RemoveQuotes(contentDisposition!.Name).Value;
				var encoding = GetEncoding(section);
				if (encoding == null)
				{
					// "The request couldn't be processed (Error 2).");
					return null;
				}

				using (var streamReader = new StreamReader(section.Body, encoding, detectEncodingFromByteOrderMarks:
					true, bufferSize: 1024, leaveOpen: true))
				{
					var value = await streamReader.ReadToEndAsync();
					if (string.Equals(value, "undefined", StringComparison.OrdinalIgnoreCase))
					{
						value = string.Empty;
					}
					
					formAccumulator.Append(key!, value);
					// form value count
					if (formAccumulator.ValueCount > 100320)
					{
						
                        // Form key count limit of 
                        // _defaultFormOptions.ValueCountLimit 
                        // is exceeded.
                        return null;
					}

				}
			}

			section = await reader.ReadNextSectionAsync();
		}
		return new MultiPartResult(formAccumulator, untrustedFileNameForStorage!,trustedFileNameForDisplay!,streamedFileContent!);
		
	}
	
	


}