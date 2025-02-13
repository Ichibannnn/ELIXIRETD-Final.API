﻿using ELIXIRETD.DATA.CORE.INTERFACES.USER_INTERFACE;
using ELIXIRETD.DATA.DATA_ACCESS_LAYER.DTOs.USER_DTO;
using ELIXIRETD.DATA.DATA_ACCESS_LAYER.HELPERS;
using ELIXIRETD.DATA.DATA_ACCESS_LAYER.MODELS.USER_MODEL;
using ELIXIRETD.DATA.DATA_ACCESS_LAYER.STORE_CONTEXT;
using Microsoft.EntityFrameworkCore;
using System.Net.WebSockets;
using EFCore.BulkExtensions;
using Microsoft.Data.SqlClient;
using ELIXIRETD.DATA.DATA_ACCESS_LAYER.REPOSITORIES.USER_REPOSITORY.Excemption;

namespace ELIXIRETD.DATA.DATA_ACCESS_LAYER.REPOSITORIES
{
    public class UserRepository : IUserRepository
    {
        private readonly StoreContext _context;

        public UserRepository(StoreContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<UserDto>> GetAllActiveUsers()
        {
            var user = _context.Users.Where(x => x.IsActive == true)
                                     .Select(x => new UserDto
                                     {
                                         Id = x.Id,
                                         EmpId = x.EmpId,
                                         FullName = x.FullName,
                                         UserName = x.UserName,
                                         Password = x.Password,
                                         UserRoleId = x.UserRoleId,
                                         UserRole = x.UserRole.RoleName,
                                         Department = x.Department,
                                         DateAdded = x.DateAdded.ToString("MM/dd/yyyy"),
                                         IsActive = x.IsActive,
                                         AddedBy = x.AddedBy
                                     });

            return await user.ToListAsync();

        }

        public async Task<IReadOnlyList<UserDto>> GetAllInActiveUsers()
        {
            var user = _context.Users.Where(x => x.IsActive == false)
                                    .Select(x => new UserDto
                                    {
                                        Id = x.Id,
                                        EmpId = x.EmpId,
                                        FullName = x.FullName,
                                        UserName = x.UserName,
                                        Password = x.Password,
                                        UserRoleId = x.UserRoleId,
                                        UserRole = x.UserRole.RoleName,
                                        Department = x.Department,
                                        DateAdded = x.DateAdded.ToString("MM/dd/yyyy"),
                                        IsActive = x.IsActive,
                                        AddedBy = x.AddedBy
                                    });

            return await user.ToListAsync();

        }

        public async Task<bool> AddNewUser(User user)
        {

            user.Password = user.UserName;

            await _context.Users.AddAsync(user);
            return true;
        }

        public async Task<bool> ChangePassword(User user)
        {
            var pass = await _context.Users.Where(x => x.Id == user.Id)
                                           .FirstOrDefaultAsync();

            pass.Password = user.Password;

            return true;
        }


        public async Task<bool> UpdateUserInfo(User user)
        {
            var existingUser = await _context.Users.Where(x => x.Id == user.Id)
                                              .FirstOrDefaultAsync();

            
            user.UserName = existingUser.UserName;
            existingUser.Password = user.Password;
            
            return true;
        }


        public async Task<bool> ActivateUser(User user)
        {

            var Users = await _context.Users.Where(x => x.Id == user.Id)
                                            .Where(x => x.UserRoleId == user.UserRoleId)
                                            .FirstOrDefaultAsync();

            if( Users == null)
            {
                return false;
            }

            Users.IsActive = true;

            var Role = await _context.Roles.Where(x => x.Id == user.UserRoleId)                                             
                                           .FirstOrDefaultAsync();

            Role.IsActive = true;
            
            return true;
           
        }


        public async Task<bool> InActiveUser(User user)
        {
            var users = await _context.Users.Where(x => x.Id == user.Id)
                                             .FirstOrDefaultAsync();

            users.IsActive = false;

            return true;
        }

        public async Task<PagedList<UserDto>> GetAllUserWithPagination(bool status, UserParams userParams)
        {

            var users = _context.Users.Where(x => x.IsActive == status)
                                      .OrderByDescending(x => x.DateAdded)
                                      .Select(x => new UserDto
                                      {
                                          Id = x.Id,
                                          EmpId = x.EmpId,
                                          FullName = x.FullName,
                                          UserName = x.UserName,
                                          Password = x.Password,
                                          UserRoleId = x.UserRoleId,
                                          UserRole = x.UserRole.RoleName,
                                          Department = x.Department,
                                          DateAdded = x.DateAdded.ToString("MM/dd/yyyy"),
                                          IsActive = x.IsActive,
                                          AddedBy = x.AddedBy
                                      });

            return await PagedList<UserDto>.CreateAsync(users, userParams.PageNumber, userParams.PageSize);

        }

        public async Task<PagedList<UserDto>> GetAllUserWithPaginationOrig(UserParams userParams, bool status, string search)
        {

            var users = _context.Users.OrderByDescending(x => x.DateAdded)
                                      .Select(x => new UserDto
                                      {
                                          Id = x.Id,
                                          EmpId = x.EmpId,
                                          FullName = x.FullName,
                                          UserName = x.UserName,
                                          Password = x.Password,
                                          UserRoleId = x.UserRoleId,
                                          UserRole = x.UserRole.RoleName,
                                          Department = x.Department,
                                          DateAdded = x.DateAdded.ToString("MM/dd/yyyy"),
                                          IsActive = x.IsActive,
                                          AddedBy = x.AddedBy

                                      }).Where(x => x.IsActive == status)
                                        .Where(x => x.UserName.ToLower().Contains(search.Trim().ToLower())
                                         || x.EmpId.ToLower().Contains(search.Trim().ToLower())
                                         || x.FullName.ToLower().Contains(search.Trim().ToLower())
                                         || x.UserName.ToLower().Contains(search.Trim().ToLower())
                                         || x.UserRole.ToLower().Contains(search.Trim().ToLower())
                                         || x.Department.ToLower().Contains(search.Trim().ToLower()));

            return await PagedList<UserDto>.CreateAsync(users, userParams.PageNumber, userParams.PageSize);
        }


        //--------------Validation


        

        public async Task<bool> ValidateRoleId(int id)
        {
            var roles = await _context.Roles.FindAsync(id);

            if (roles == null)
                return false;

            return true;
        }

        public async Task<bool> ValidateUserExist(string username)
        {
            return await _context.Users.AnyAsync(x => x.UserName == username);
        }

        public async Task<bool> ValidateUserRolesModule(User user)
        {

            var roleModule = await _context.RoleModules.Where(x => x.RoleId == user.UserRoleId)
                                                        .FirstOrDefaultAsync();

            if (roleModule == null)
            {
                roleModule = new UserRoleModules
                {
                    RoleId = user.UserRoleId,
                    ModuleId = 0
                };
                _context.RoleModules.Add(roleModule);
                return true;

            }
            else
            {
                if (roleModule.ModuleId == 0)
                    return true;
                else
                    return false;
            }

        }

        public async Task<bool> ValidationPassword(User user)
        {
            var validation = await _context.Users.Where(x => x.Id == user.Id)
                                                 .Where(x => x.Password == user.Password)
                                                 .FirstOrDefaultAsync();

            if (validation == null)
                return false;

            return true;
                                   
        }

        public async Task<bool> ValidateRoleInUse(int role)
        {
            var valid = await _context.Users.Where(x => x.UserRoleId == role)
                                                    .Where(x => x.IsActive == true)
                                                    .FirstOrDefaultAsync();
            if (valid == null)
                return false;
            return true;
        }

    


    }
}

           
    