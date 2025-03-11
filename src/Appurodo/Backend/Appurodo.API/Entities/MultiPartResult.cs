using Microsoft.AspNetCore.WebUtilities;

namespace Appurodo.API.Entities;

public record MultiPartResult(KeyValueAccumulator FormAccumulator, string UntrustedFileNameForStorage, string TrustedFileName, byte[] StreamedFileContent);