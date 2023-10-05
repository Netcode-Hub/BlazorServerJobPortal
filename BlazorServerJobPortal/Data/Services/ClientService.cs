using BlazorServerJobPortal.Authentication;
using BlazorServerJobPortal.Data.Configuration;
using BlazorServerJobPortal.Data.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace BlazorServerJobPortal.Data.Services
{
    public class ClientService : IClientService
    {
        private readonly AppDbContext appDbContext;
        private readonly AuthenticationStateProvider authStateProvider;

        public ClientService(AppDbContext appDbContext, AuthenticationStateProvider authStateProvider)
        {
            this.appDbContext = appDbContext;
            this.authStateProvider = authStateProvider;
        }

        public async Task<(bool flag, string Message)> PostJobAsync(PostJob model)
        {
            var checkIfJobAlreadyPosted = await appDbContext.Jobs.Where(_ => _.CompanyEmail!.ToLower().Equals(model.CompanyEmail!.ToLower()) && _.Title!.ToLower().Equals(model.Title!.ToLower())).FirstOrDefaultAsync();
            if (checkIfJobAlreadyPosted != null) return (false, $"Job: {model.Title} already posted");

            appDbContext.Jobs.Add(model);
            await appDbContext.SaveChangesAsync();
            return (true, "Job Posted");
        }


        public async Task<(bool flag, string Message)> RegisterUserAsync(RegistrationModel model)
        {
            var userRole = new UserRole();
            //Check if admin already exist
            if (model.Email!.ToLower().StartsWith("admin"))
            {
                var chkIfAdminExist = await appDbContext.UserRoles.Where(_ => _.RoleName!.ToLower().Equals("admin")).FirstOrDefaultAsync();
                if (chkIfAdminExist != null) return (false, "Sorry Admin already exist, please change the email address");

                userRole.RoleName = "Admin";
            }

            var checkIfUserAlreadyCreated = await appDbContext.Registrations.Where(_ => _.Email!.ToLower().Equals(model.Email!.ToLower())).FirstOrDefaultAsync();
            if (checkIfUserAlreadyCreated != null) return (false, $"Email: {model.Email} already registered");


            var hashedPassword = HashPassword(model.Password!);
            var registeredEntity = appDbContext.Registrations.Add(new Registration()
            {
                Name = model.Name,
                Email = model.Email,
                Password = hashedPassword,
                CompanyAddress = model.CompanyAddress,
                CompanyCertificateName = model.CompanyCertificateName,
                CompanyLocation = model.CompanyLocation,
                CompanyLogo = model.CompanyLogo,
                CompanyName = model.CompanyName,
                Phone = model.Phone.ToString(),
            }).Entity;
            await appDbContext.SaveChangesAsync();


            if (string.IsNullOrEmpty(userRole.RoleName))
                userRole.RoleName = "User";

            userRole.UserId = registeredEntity.Id;
            appDbContext.UserRoles.Add(userRole);
            await appDbContext.SaveChangesAsync();

            return (true, "Account Created");
        }

        // Encrypt user password
        private static string HashPassword(string password)
        {
            byte[] salt = new byte[16];
            using (var randomGenerator = RandomNumberGenerator.Create())
            {
                randomGenerator.GetBytes(salt);
            }
            var rfcPassword = new Rfc2898DeriveBytes(password, salt, 1000, HashAlgorithmName.SHA1);
            byte[] rfcPasswordHash = rfcPassword.GetBytes(20);

            byte[] passwordHash = new byte[36];
            Array.Copy(salt, 0, passwordHash, 0, 16);
            Array.Copy(rfcPasswordHash, 0, passwordHash, 16, 20);
            return Convert.ToBase64String(passwordHash);
        }

        public async Task<(bool flag, string Message)> LoginUserAsync(Login model)
        {
            var getUser = await appDbContext.Registrations.Where(_ => _.Email!.Equals(model.Email)).FirstOrDefaultAsync();
            if (getUser == null) return (false, "Sorry you don't have account, kindly register");

            var checkIfPasswordMatch = VerifyUserPassword(model.Password!, getUser.Password!);
            if (checkIfPasswordMatch)
            {
                var getUserRole = await appDbContext.UserRoles.Where(_ => _.Id == getUser.Id).FirstOrDefaultAsync();
                var customAuthStateProvider = (CustomAuthenticationStateProvider)authStateProvider;
                await customAuthStateProvider.UpdateAuthenticationState(new UserSession
                {
                    Username = getUser.Email,
                    UserRole = getUserRole!.RoleName
                });
                return (true, "success");
            }
            return (false, "Invalid username/password");
        }

        //Decrypt user database password and encrypt user raw password and compare
        private static bool VerifyUserPassword(string rawPassword, string databasePassword)
        {
            byte[] dbPasswordHash = Convert.FromBase64String(databasePassword);
            byte[] salt = new byte[16];
            Array.Copy(dbPasswordHash, 0, salt, 0, 16);
            var rfcPassword = new Rfc2898DeriveBytes(rawPassword, salt, 1000, HashAlgorithmName.SHA1);
            byte[] rfcPasswordHash = rfcPassword.GetBytes(20);
            for (int i = 0; i < rfcPasswordHash.Length; i++)
            {
                if (dbPasswordHash[i + 16] != rfcPasswordHash[i])
                    return false;
            }
            return true;
        }

        public async Task LogoutAsync()
        {
            var customAuthStateProvider = (CustomAuthenticationStateProvider)authStateProvider;
            await customAuthStateProvider.UpdateAuthenticationState(null!);
        }


        public async Task<List<GetJob>> GetAllJobsAsync(string filter)
        {
            var AllJobs = new List<GetJob>();
            List<PostJob> availableJobs = new();

            if (string.IsNullOrEmpty(filter))
                availableJobs = await appDbContext.Jobs.Where(_ => _.Active).ToListAsync();
            else
                availableJobs = await appDbContext.Jobs.Where(_ => _.Active && _.Title!.ToLower().Contains(filter.ToLower())).ToListAsync();


            if (availableJobs is null) return null!;
            //get all companies
            var companiesList = await appDbContext.Registrations.ToListAsync();

            //loop through jobs
            foreach (var job in availableJobs)
            {
                var getjobProvider = companiesList.Where(_ => _.Email!.ToLower().Equals(job.CompanyEmail!.ToLower())).FirstOrDefault();
                AllJobs.Add(new GetJob()
                {
                    Id = job.Id,
                    Title = job.Title,
                    Function = job.Function,
                    Description = job.Description,
                    MinSalaryRange = job.MinSalaryRange,
                    MaxSalaryRange = job.MaxSalaryRange,
                    JobMode = job.JobMode,
                    JobLocation = job.JobLocation,
                    Featured = job.Featured,
                    CompanyName = getjobProvider?.CompanyName,
                    CompanyAddress = getjobProvider?.CompanyAddress,
                    CompanyEmail = getjobProvider?.Email,
                    CompanyLocation = getjobProvider?.CompanyLocation,
                    CompanyLogo = getjobProvider?.CompanyLogo,
                    DateAdded = job.DateAdded
                });
            }
            return AllJobs;
        }
    }
}
