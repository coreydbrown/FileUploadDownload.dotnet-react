using System;
using System.Collections.Generic;

namespace FileApi.Models;

public partial class FileModel
{
    public int Id { get; set; }

    public byte[]? Content { get; set; }

    public string? UntrustedName { get; set; }

    public string? ContentType { get; set; }

    public string? Note { get; set; }

    public long? Size { get; set; }

    public DateTime? UploadDt { get; set; }
}
