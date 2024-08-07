﻿using EventSphere.Business.Services.Interfaces;
using EventSphere.Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EventSphere.Business.Helper;

namespace EventSphere.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly IEmailService _emailService;
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;
    

        public ReportController(IReportService reportService, IEmailService emailService, IUserService userService, INotificationService notificationService)
        {
            _reportService = reportService;
            _emailService = emailService;
            _userService = userService;
            _notificationService = notificationService;
        }

        [HttpGet]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> GetAllReports()
        {
            try
            {
                var reports = await _reportService.GetAllReportsAsync();
                
                return Ok(reports);
            }
            catch (Exception ex)
            {
              
                return StatusCode(500, new { Error = "An error occurred while processing your request." });
            }
        }

        [HttpGet("count")]
        public async Task<ActionResult<int>> GetReportCount()
        {
            try
            {
                var count = await _reportService.GetReportCountAsync();
            
                return Ok(count);
            }
            catch (Exception ex)
            {
            
                return StatusCode(500, new { Error = "An error occurred while processing your request." });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "All")]
        public async Task<IActionResult> GetReportId(int id)
        {
            try
            {
                var report = await _reportService.GetReportByIdAsync(id);
                if (report == null)
                {
                    Log.Error("Report not found: {Id}", id);
                    return NotFound();
                }
               
                return Ok(report);
            }
            catch (Exception ex)
            {
              
                return StatusCode(500, new { Error = "An error occurred while processing your request." });
            }
        }

        [HttpGet("GetReportByUserId/{userId}")]
        [Authorize(Policy = "All")]
        public async Task<IActionResult> GetReportByUserId(int userId)
        {
            try
            {
                var reports = await _reportService.GetReportByUserIdAsync(userId);
              
                return Ok(reports);
            }
            catch (Exception ex)
            {
              
                return StatusCode(500, new { Error = "An error occurred while processing your request." });
            }
        }

        [HttpPost]
        [Authorize(Policy = "All")]
        public async Task<IActionResult> Create(ReportDTO reportDTO)
        {
            var userEmail = HttpContext.User.FindFirst(ClaimTypes.Email)?.Value;
            if (reportDTO == null)
            {
                Log.Error("Invalid report data.");
                return BadRequest(new { Error = "Invalid report data." });
            }

            try
            {
                var report = await _reportService.CreateAsync(reportDTO);

              
                var adminUsers = await _userService.GetUsersByRoleAsync("Admin");
                if (adminUsers == null || !adminUsers.Any())
                {
                    Log.Error("No admin users found.");
                    return NotFound(new { Error = "No admin users found." });
                }

       
                var emailTasks = adminUsers.Select(user => 
                {
                    var mailRequest = new MailRequest
                    {
                        ToEmail = user.Email,
                        Subject = $"Report from {report.userName}",
                        Body = $@"
                        <p>Report name: {report.reportName}</p>
                        ---------------------------------------------------
                        <p>Report description: {report.reportDesc}</p>
                        "
                    };

                     _notificationService.SendNotificationAsync(user.ID, $"New Report was send from {report.userName} ");
                    return _emailService.SendEmailAsync(mailRequest);
                });

                await Task.WhenAll(emailTasks);

                Log.Information("Report created and emails sent successfully: {@Report} by {userEmail}", report , userEmail);
                return CreatedAtAction(nameof(GetReportId), new { id = report.reportId }, report);
            }
            catch (Exception ex)
            {
                Log.Fatal("An error occurred while creating the report: by {userEmail}", userEmail);
                return StatusCode(500, new { Error = "An error occurred while processing your request." });
            }
        }
        
        [HttpPut("{id}")]
        [Authorize(Policy = "All")]
        public async Task<IActionResult> Update(int id, ReportDTO reportDTO)
        {
            var userEmail = HttpContext.User.FindFirst(ClaimTypes.Email)?.Value;
            if (id != reportDTO.reportId)
            {
                Log.Error("Invalid ID or report data.");
                return BadRequest(new { Error = "Invalid ID or report data." });
            }

            try
            {
                await _reportService.UpdateAsync(id, reportDTO);
                Log.Information("Report updated successfully: {@Report} by {userEmail}", reportDTO , userEmail);
                return NoContent();
            }
            catch (Exception ex)
            {
                Log.Fatal("An error occurred while updating the report: by {userEmail}", userEmail);
                return StatusCode(500, new { Error = "An error occurred while processing your request." });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "All")]
        public async Task<IActionResult> Delete(int id)
        {
            var userEmail = HttpContext.User.FindFirst(ClaimTypes.Email)?.Value;
            try
            {
                await _reportService.DeleteAsync(id);
                Log.Information("Report deleted successfully: {Id} by {userEmail}", id , userEmail);
                return NoContent();
            }
            catch (Exception ex)
            {
                Log.Fatal("An error occurred while deleting the report: by {userEmail}", userEmail);
                return StatusCode(500, new { Error = "An error occurred while processing your request." });
            }
        }
    }
}