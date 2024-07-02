﻿using EventSphere.Business.Services.Interfaces;
using EventSphere.Domain.DTOs.User;
using EventSphere.Domain.DTOs;
using EventSphere.Domain.Entities;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventSphere.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IMapper _mapper;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, IMapper mapper, ILogger<UserController> logger)
        {
            _userService = userService;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet("getUsers")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                var userDtos = _mapper.Map<IEnumerable<UpdateUserDTO>>(users);
            
                return Ok(userDtos);
            }
            catch (Exception ex)
            {
               
                return StatusCode(500, new { Error = "An error occurred while processing your request." });
            }
        }

        [HttpGet("count")]
        public async Task<ActionResult<int>> GetUserCount()
        {
            try
            {
                var count = await _userService.GetUserCountAsync();
              
                return Ok(count);
            }
            catch (Exception ex)
            {
               
                return StatusCode(500, new { Error = "An error occurred while processing your request." });
            }
        }

        [HttpGet("getUser/{id}")]
        [Authorize(Policy = "All")]
        public async Task<IActionResult> GetUser(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    Log.Warning("User not found: {Id}", id);
                    return NotFound();
                }
                var userDto = _mapper.Map<UpdateUserDTO>(user);
               
                return Ok(userDto);
            }
            catch (Exception ex)
            {
              
                return StatusCode(500, new { Error = "An error occurred while processing your request." });
            }
        }

        [HttpPut("updateUser")]
        [Authorize(Policy = "All")]
        public async Task<IActionResult> UpdateUser(UpdateUserDTO updateUserDto)
        {
            try
            {
                await _userService.UpdateUserAsync(updateUserDto);
                Log.Information("User updated successfully: {@User}", updateUserDto);
                return NoContent();
            }
            catch (Exception ex)
            {
                Log.Fatal("An error occurred while updating the user: {@Error}", ex);
                return StatusCode(500, new { Error = "An error occurred while processing your request." });
            }
        }

        [HttpPatch("updateUserPassword/{id}")]
        [Authorize(Policy = "All")]
        public async Task<IActionResult> UpdateUserPassword(int id, UpdatePasswordDto updatePasswordDto)
        {
            try
            {
                await _userService.UpdateUserPasswordAsync(id, updatePasswordDto);
                Log.Information("User password updated successfully for user ID: {UserId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                Log.Fatal("An error occurred while updating user password: {@Error}", ex);
                return StatusCode(500, new { Error = "An error occurred while processing your request." });
            }
        }

        [HttpDelete("deleteUser/{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                await _userService.DeleteUserAsync(id);
                Log.Information("User deleted successfully: {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                Log.Fatal("An error occurred while deleting the user: {@Error}", ex);
                return StatusCode(500, new { Error = "An error occurred while processing your request." });
            }
        }
    }
}
