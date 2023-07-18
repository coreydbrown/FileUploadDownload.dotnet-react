using FileApi.Models;
using FileApi.Filters;
using FileApi.Utilities;
using System.Net;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;
using FileApi.Services;

namespace FileApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly IFileRepository _fileRepository;
        private readonly long _fileSizeLimit;
        private readonly string[] _prohibitedExtensions = { ".exe" };
        private static readonly FormOptions _defaultFormOptions = new FormOptions();

        public FileController(IFileRepository fileRepository, IConfiguration config)
        {
            _fileRepository = fileRepository;
            _fileSizeLimit = config.GetValue<long>("FileSizeLimit");
        }

        [HttpPost]
        [Route("upload")]
        [DisableFormValueModelBinding]
        public async Task<IActionResult> Upload()
        {
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                ModelState.AddModelError("File", "The request couldn't be processed. The content-type must be multipart.");
                return BadRequest(ModelState);
            }

            var contentType = string.Empty;
            var trustedFileNameForDisplay = string.Empty;
            var untrustedFileNameForStorage = string.Empty;
            
            var streamedFileContent = Array.Empty<byte>();
            // Accumulate the form data key-value pairs in the request
            var formAccumulator = new KeyValueAccumulator();

            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(Request.ContentType),
                _defaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);

            var section = await reader.ReadNextSectionAsync();

            while (section != null)
            {
                var hasContentDispositionHeader =
                    ContentDispositionHeaderValue.TryParse(
                        section.ContentDisposition, out var contentDisposition);

                if (hasContentDispositionHeader)
                {
                    // executes if multipart section is a file
                    if (MultipartRequestHelper
                        .HasFileContentDisposition(contentDisposition))
                    {
                        untrustedFileNameForStorage = contentDisposition.FileName.Value;
                        // The file name sent by the client is HTML-encoded for safe displaying/logging.
                        trustedFileNameForDisplay = WebUtility.HtmlEncode(
                                contentDisposition.FileName.Value);

                        contentType = section.ContentType;

                        streamedFileContent =
                            await FileHelpers.ProcessStreamedFile(section, contentDisposition,
                                ModelState, _prohibitedExtensions, _fileSizeLimit);

                        if (!ModelState.IsValid)
                        {
                            return BadRequest(ModelState);
                        }
                    }
                    // executes if multipart section is form data
                    else if (MultipartRequestHelper
                        .HasFormDataContentDisposition(contentDisposition))
                    {
                        var key = HeaderUtilities
                            .RemoveQuotes(contentDisposition.Name).Value;
                        var encoding = FileHelpers.GetEncoding(section);

                        if (encoding == null)
                        {
                            ModelState.AddModelError("File",
                                "Failed to determine encoding for section content.");

                            return BadRequest(ModelState);
                        }

                        using (var streamReader = new StreamReader(
                            section.Body,
                            encoding,
                            detectEncodingFromByteOrderMarks: true,
                            bufferSize: 1024,
                            leaveOpen: true))
                        {
                            // The value length limit is enforced by MultipartBodyLengthLimit
                            var value = await streamReader.ReadToEndAsync();

                            if (string.Equals(value, "undefined",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                value = string.Empty;
                            }

                            formAccumulator.Append(key, value);

                            if (formAccumulator.ValueCount >
                                _defaultFormOptions.ValueCountLimit)
                            {
                                // Form key count limit of _defaultFormOptions.ValueCountLimit is exceeded.
                                ModelState.AddModelError("File",
                                    "The request contains too many form entries.");

                                return BadRequest(ModelState);
                            }
                        }
                    }
                }

                section = await reader.ReadNextSectionAsync();
            }

            // Bind form data to the model
            var formData = new FormData() { Note = string.Empty };
            var formValueProvider = new FormValueProvider(
                BindingSource.Form,
                new FormCollection(formAccumulator.GetResults()),
                CultureInfo.CurrentCulture);
            var bindingSuccessful = await TryUpdateModelAsync(formData, prefix: "",
                valueProvider: formValueProvider);

            if (!bindingSuccessful)
            {
                ModelState.AddModelError("File",
                    "Failed to bind form data to model.");

                return BadRequest(ModelState);
            }

            // In production scenarios, an anti-virus/anti-malware scanner API should be used to scan file before making the file available for download or for use by other systems.

            var file = new FileModel()
            {
                Content = streamedFileContent,
                UntrustedName = untrustedFileNameForStorage,
                ContentType = contentType,
                Note = formData.Note,
                Size = streamedFileContent.Length,
                UploadDt = DateTime.UtcNow
            };

            try
            {
                await _fileRepository.AddFileAsync(file);
                return Created($"api/File/download/{file.Id}", new { id = file.Id, filename = trustedFileNameForDisplay });
            }
            catch (Exception ex)
            {
                // Implement logging
                System.Diagnostics.Debug.WriteLine(ex);
                return StatusCode(500, "An error occured. Please try again later");
            }
        }

        [HttpGet]
        [Route("download/{id}")]
        public async Task<IActionResult> DownloadFile(int id)
        {
            try
            {
                var fileData = await _fileRepository.GetFileAsync(id);
                Response.RegisterForDispose(fileData.dataStream);
                return File(fileData.dataStream, fileData.contentType, fileData.fileName);
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                // Implement logging
                System.Diagnostics.Debug.WriteLine(ex);
                return StatusCode(500, "An error occured. Please try again later");
            }
        }
    }
}
