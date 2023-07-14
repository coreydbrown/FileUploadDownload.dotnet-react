using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Text;

namespace FileApi.Utilities
{
    public static class FileHelpers
    {
        // ***In production scenarios, an anti-virus/anti-malware scanner API should be used to scan file before making the file available for download or for use by other systems.

        public static async Task<byte[]> ProcessStreamedFile(
            MultipartSection section, ContentDispositionHeaderValue contentDisposition,
            ModelStateDictionary modelState, string[] prohibitedExtensions, long sizeLimit)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    await section.Body.CopyToAsync(memoryStream);

                    // Check if the file is empty
                    if (memoryStream.Length == 0)
                    {
                        modelState.AddModelError("File", "The file is empty.");
                    }
                    // Check if the file exceeds the size limit
                    else if (memoryStream.Length > sizeLimit)
                    {
                        var megabyteSizeLimit = sizeLimit / 1048576;
                        modelState.AddModelError("File",
                        $"The file exceeds {megabyteSizeLimit:N1} MB.");
                    }
                    // Check if the file extension is permitted
                    else if (!IsValidFileExtension(
                        contentDisposition.FileName.Value, memoryStream,
                        prohibitedExtensions))
                    {
                        modelState.AddModelError("File",
                            "The file type isn't permitted");
                    }
                    else
                    {
                        return memoryStream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                // Implement logging
                modelState.AddModelError("File",
                    $"The upload failed. Error: {ex.HResult}");
            }

            return Array.Empty<byte>();
        }

        private static bool IsValidFileExtension(string fileName, Stream data, string[] prohibitedExtensions)
        {
            if (string.IsNullOrEmpty(fileName) || data == null || data.Length == 0)
            {
                return false;
            }

            var ext = Path.GetExtension(fileName).ToLowerInvariant();

            if (string.IsNullOrEmpty(ext) || prohibitedExtensions.Contains(ext))
            {
                return false;
            }

            return true;
        }

        public static Encoding GetEncoding(MultipartSection section)
        {
            var hasMediaTypeHeader =
                MediaTypeHeaderValue.TryParse(section.ContentType, out var mediaType);

            // UTF-7 is insecure and shouldn't be honored. UTF-8 succeeds in most cases.
            if (!hasMediaTypeHeader || Encoding.UTF7.Equals(mediaType.Encoding))
            {
                return Encoding.UTF8;
            }

            return mediaType.Encoding;
        }
    }
}