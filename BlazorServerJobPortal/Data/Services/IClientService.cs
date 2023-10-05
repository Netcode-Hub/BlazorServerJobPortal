using BlazorServerJobPortal.Data.Models;
namespace BlazorServerJobPortal.Data.Services
{
    public interface IClientService
    {
        Task<(bool flag, string Message)> RegisterUserAsync(RegistrationModel model);
        Task<(bool flag, string Message)> LoginUserAsync(Login model);
        Task<(bool flag, string Message)> PostJobAsync(PostJob model);

        Task LogoutAsync();
        Task<List<GetJob>> GetAllJobsAsync(string filter);
    }
}
