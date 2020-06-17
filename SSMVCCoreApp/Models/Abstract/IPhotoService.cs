using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace SSMVCCoreApp.Models.Abstract
{
    public interface IPhotoService
    {
        Task<string> UploadPhotoAsync(string category, IFormFile photoToUpload);
        Task<bool> DeletePhotoAsync(string category, string photoUrl);
    }
}
