﻿using FileApi.Models;
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
                throw new BadHttpRequestException("The request couldn't be processed. The content-type must be multipart.");
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
            var reader = new MultipartReader(boundary, Request.Body);

            var section = await reader.ReadNextSectionAsync();

            while (section != null)
            {
                var hasContentDispositionHeader =
                    ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);

                if (hasContentDispositionHeader)
                {
                    // executes if multipart section is a file
                    if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                    {
                        untrustedFileNameForStorage = contentDisposition.FileName.Value;
                        // The file name sent by the client is HTML-encoded for safe displaying/logging.
                        trustedFileNameForDisplay = WebUtility.HtmlEncode(contentDisposition.FileName.Value);

                        contentType = section.ContentType;

                        streamedFileContent =
                            await FileHelpers.ProcessStreamedFile(section, contentDisposition,
                                _prohibitedExtensions, _fileSizeLimit);
                    }
                    // executes if multipart section is form data
                    else if (MultipartRequestHelper.HasFormDataContentDisposition(contentDisposition))
                    {
                        var key = HeaderUtilities.RemoveQuotes(contentDisposition.Name).Value;
                        var encoding = FileHelpers.GetEncoding(section);

                        if (encoding == null)
                        {
                            throw new BadHttpRequestException("Failed to determine encoding for section content.");
                        }

                        using (var streamReader = new StreamReader(
                            section.Body,
                            encoding,
                            detectEncodingFromByteOrderMarks: true))
                        {
                            // The value length limit is enforced by MultipartBodyLengthLimit
                            var value = await streamReader.ReadToEndAsync();

                            if (string.Equals(value, "undefined", StringComparison.OrdinalIgnoreCase))
                            {
                                value = string.Empty;
                            }

                            formAccumulator.Append(key, value);

                            if (formAccumulator.ValueCount > _defaultFormOptions.ValueCountLimit)
                            {
                                throw new BadHttpRequestException("The request contains too many form entries.");
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
                throw new BadHttpRequestException("Failed to bind form data to model.");
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

            await _fileRepository.AddFileAsync(file);
            return Created($"api/File/download/{file.Id}", new { id = file.Id, filename = trustedFileNameForDisplay });
        }

        [HttpGet]
        [Route("download/{id}")]
        public async Task<IActionResult> DownloadFile(int id)
        {
            var fileData = await _fileRepository.GetFileAsync(id);
            Response.RegisterForDispose(fileData.dataStream);
            return File(fileData.dataStream, fileData.contentType, fileData.fileName);
        }
    }
}
