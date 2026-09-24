using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace CafeApp.Business.Services.Abstract
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(IFormFile file);
    }
}
