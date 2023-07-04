namespace FileApi.Models;

public partial class FileModel
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? ContentType { get; set; }

    public byte[]? Data { get; set; }
}
