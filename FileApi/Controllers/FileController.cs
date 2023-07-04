using FileApi.Data;
using FileApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly FileApiDbContext _dbContext;

        public FileController(FileApiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // upload file
        [HttpPost]
        [Route("upload")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
        {
            var fileModel = new FileModel();
            fileModel.Name = file.FileName;
            fileModel.ContentType = file.ContentType;
            using (var memoryStream = new MemoryStream())
            {
                file.CopyTo(memoryStream);
                fileModel.Data = memoryStream.ToArray();
            }

            _dbContext.Files.Add(fileModel);
            await _dbContext.SaveChangesAsync();

            return Ok(new { fileModel.Id, fileModel.Name });
        }

        // download file
        [HttpGet]
        [Route("download/{id}")]
        public async Task<IActionResult> DownloadFile(int id)
        {
            var fileModel = await _dbContext.Files.FindAsync(id);

            if (fileModel == null)
                return NotFound();

            return File(fileModel.Data, fileModel.ContentType, fileModel.Name);
        }
    }
}
