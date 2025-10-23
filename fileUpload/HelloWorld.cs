using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class HelloWorld : Controller
    {
        private IWebHostEnvironment Environment;
        public HelloWorld(IWebHostEnvironment _environment)
        {
            Environment = _environment;
        }

        public IActionResult Index()
        {
            return View(new FileUpload());
        }


        public string getFile(FileUpload files)
        {
            var uniqueFileName = files.FormFile.FileName;
            //var uniqueFileName = GetUniqueFileName(files.FormFile.FileName);
            var uploads = Path.Combine(Environment.WebRootPath, "image");
            var filePath = Path.Combine(uploads, uniqueFileName);
            files.FormFile.CopyTo(new FileStream(filePath, FileMode.Create));

            return "upload ok";
        }

        private string GetUniqueFileName(string fileName)
        {
            fileName = Path.GetFileName(fileName);
            return Path.GetFileNameWithoutExtension(fileName)
                      + "_"
                      + Guid.NewGuid().ToString().Substring(0, 4)
                      + Path.GetExtension(fileName);

        }
    }
}
