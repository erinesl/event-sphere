﻿using EventSphere.Business.Services;
using EventSphere.Business.Services.Interfaces;
using EventSphere.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventSphere.API.Controllers
{
    [Route("api/roles")]
    [ApiController]
    [Authorize]
    public class RoleController : Controller
    {
        private readonly IRoleServices _roleServices;

        public RoleController(IRoleServices roleServices)
        {
            _roleServices = roleServices;
        }
        [HttpPost]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> CreateRole(Role role)
        {
            var createdRole = await _roleServices.AddRoleAsync(role);
            return CreatedAtAction(nameof(GetRoleById), new { id = createdRole.Id }, createdRole);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> GetRoleById(int id)
        {
            var role = await _roleServices.GetRoleByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }
            return Ok(role);
        }

        [HttpGet]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> GetAllRoles()
        {
            var roles = await _roleServices.GetAllRolesAsync();
            return Ok(roles);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> UpdateRole(int id, Role role)
        {
            if (id != role.Id)
            {
                return BadRequest();
            }

            await _roleServices.UpdateRoleAsync(role);
            return Ok(role);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            await _roleServices.DeleteRoleAsync(id);
            return NoContent();
        }
    }
}
