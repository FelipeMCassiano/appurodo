using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace Appurodo.API.Services;

public static class FileService
{
	
	private static readonly byte[] _allowedChars = { };
        private static readonly Dictionary<string, List<byte[]>> _fileSignature = new Dictionary<string, List<byte[]>>
        {
            { ".gif", new List<byte[]> { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },
            { ".png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
            { ".jpeg", new List<byte[]>
                {
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE2 },
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE3 },
                }
            },
            { ".jpg", new List<byte[]>
                {
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 },
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE8 },
                }
            },
            { ".zip", new List<byte[]> 
                {
                    new byte[] { 0x50, 0x4B, 0x03, 0x04 }, 
                    new byte[] { 0x50, 0x4B, 0x4C, 0x49, 0x54, 0x45 },
                    new byte[] { 0x50, 0x4B, 0x53, 0x70, 0x58 },
                    new byte[] { 0x50, 0x4B, 0x05, 0x06 },
                    new byte[] { 0x50, 0x4B, 0x07, 0x08 },
                    new byte[] { 0x57, 0x69, 0x6E, 0x5A, 0x69, 0x70 },
                }
            },
        };

        public static async Task<byte[]?> ProcessStreamedFile(MultipartSection section, ContentDispositionHeaderValue
            contentDisposition, string[] permittedExtensions, long sizeLimit)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                await section.Body.CopyToAsync(memoryStream);
                if (memoryStream.Length == 0)
                {
                    // file is empty
                    return null;
                }

                if (memoryStream.Length > sizeLimit)
                {
                    // file exceeds
                    return null;
                }

                return !IsValidFileExtensionAndSignature(contentDisposition.FileName.Value!, memoryStream,
                           permittedExtensions) ?
                           //  "The file type isn't permitted or the file's " + "signature doesn't match the file's extension." 
                           null : memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                // File
                //"The upload failed. Please contact the Help Desk " +
                 //   $" for support. Error: {ex.HResult}";
                 Console.WriteLine(ex.Message);
                 return null;
            }
        }

        private static bool IsValidFileExtensionAndSignature(string fileName, Stream data, string[] permittedExtensions)
            {
                if (string.IsNullOrEmpty(fileName) || data == null || data.Length == 0)
                {
                    return false;
                }

                var ext = Path.GetExtension(fileName).ToLowerInvariant();

                if (string.IsNullOrEmpty(ext) || !permittedExtensions.Contains(ext))
                {
                    return false;
                }

                data.Position = 0;

                using (var reader = new BinaryReader(data))
                {
                    if (ext.Equals(".txt") || ext.Equals(".csv") || ext.Equals(".prn"))
                    {
                        if (_allowedChars.Length == 0)
                        {
                            // Limits characters to ASCII encoding.
                            for (var i = 0; i < data.Length; i++)
                            {
                                if (reader.ReadByte() > sbyte.MaxValue)
                                {
                                    return false;
                                }
                            }
                        }
                        else
                        {
                            for (var i = 0; i < data.Length; i++)
                            {
                                var b = reader.ReadByte();
                                if (b > sbyte.MaxValue ||
                                    !_allowedChars.Contains(b))
                                {
                                    return false;
                                }
                            }
                        }

                        return true;
                    }

                    var signatures = _fileSignature[ext];
                    var headerBytes = reader.ReadBytes(signatures.Max(m => m.Length));

                    return signatures.Any(signature => 
                        headerBytes.Take(signature.Length).SequenceEqual(signature));
                }
            }
    }           

