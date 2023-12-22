using AutoMapper;
using MagicVilla_VillaAPI.Data;
using MagicVilla_VillaAPI.Models;
using MagicVilla_VillaAPI.Models.Dto;
using MagicVilla_VillaAPI.Repository.IRepository;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MagicVilla_VillaAPI.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IMapper _mapper;
        private string secretKey;
        public UserRepository(ApplicationDbContext db, IConfiguration configuration, UserManager<ApplicationUser> userManager, IMapper mapper, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _mapper = mapper;
            secretKey = configuration.GetValue<string>("ApiSettings:Secret");
            _roleManager = roleManager;

        }

        public bool IsUniqueUser(string userName)
        {
            var localUser = _db.AplicationUsers.FirstOrDefault(x => x.UserName == userName);
            if (localUser == null)
            {
                return true;
            }
            return false;
        }

        public async Task<LoginResponseDTO> Login(LoginRequestDTO loginRequestDTO)
        {
            var user = await _db.AplicationUsers.FirstOrDefaultAsync(u => u.UserName.ToLower() == loginRequestDTO.UserName.ToLower());

            bool isValid = await _userManager.CheckPasswordAsync(user, loginRequestDTO.Password);

            if (user == null || isValid== false)
            {
                return new LoginResponseDTO() 
                {
                    Token = "",
                    User = null
                };
            }

            // if user found, generate JWT Token
            var roles = await _userManager.GetRolesAsync(user);
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                            {
                                new Claim(ClaimTypes.Name,user.Id.ToString()),
                                new Claim(ClaimTypes.Role,roles.FirstOrDefault())
                            }),
                Expires = DateTime.UtcNow.AddDays(7),
                 SigningCredentials = new (new SymmetricSecurityKey(key),SecurityAlgorithms.HmacSha256Signature)
            };

            //foreach (var role in roles)
            //{
            //    tokenDescriptor.Subject.AddClaim(new Claim(ClaimTypes.Role,role));
            //}

            var token = tokenHandler.CreateToken(tokenDescriptor);

            LoginResponseDTO loginResponseDTO = new LoginResponseDTO 
            {
                User = _mapper.Map<UserDTO>(user),
                Token = tokenHandler.WriteToken(token),
                Role = roles.FirstOrDefault()
            };

            return loginResponseDTO;
        }

        public async Task<UserDTO> Register(RegistrationRequestDTO registrationRequestDTO)
        {

            ApplicationUser user = new ()
            {
                UserName = registrationRequestDTO.UserName,
                Email = registrationRequestDTO.UserName,
                NormalizedEmail = registrationRequestDTO.UserName.ToUpper(),
                Name = registrationRequestDTO.Name
            };

            try
            {
                var result = await _userManager.CreateAsync(user,registrationRequestDTO.Password);
                if (result.Succeeded)
                {
                    if(!await _roleManager.RoleExistsAsync("Admin"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Admin"));
                        await _roleManager.CreateAsync(new IdentityRole("Customer"));
                    }
                    await _userManager.AddToRoleAsync(user,"Admin");
                    var userToReturn = await _db.AplicationUsers.FirstOrDefaultAsync(u => u.UserName == registrationRequestDTO.UserName);
                    return _mapper.Map<UserDTO>(userToReturn);
                }
            }
            catch (Exception ex)
            {

                throw;
            }

            return new UserDTO();
        }
    }
}
