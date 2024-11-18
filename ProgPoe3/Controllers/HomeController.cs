using Microsoft.AspNetCore.Mvc;
using ProgPoe3.Models;
using ProgPoe3.Data;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ProgPoe3.Controllers
{
    public class HomeController : Controller
    {
        private readonly ClaimDbContext _context;


        public HomeController(ClaimDbContext context)
        {
            _context = context;
        }

        // New Login action
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password, string role)
        {
            if (IsValidUser(username, password, role))
            {
                HttpContext.Session.SetString("UserRole", role);
                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.ErrorMessage = "Invalid login credentials";
                return View();
            }
        }

        private bool IsValidUser(string username, string password, string role)
        {
            return (username == "Lec1" && password == "Password1" && role == "Lecturer") ||
                   (username == "Cor1" && password == "Password1" && role == "Coordinator") ||
                   (username == "Man1" && password == "Password1" && role == "Manager") ||
                   (username == "Hr1" && password == "Password1" && role == "HR");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // Existing actions with added access control
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult SubmitClaim()
        {
            if (HttpContext.Session.GetString("UserRole") != "Lecturer")
            {
                return RedirectToAction("AccessDenied");
            }
            return View();
        }

        public async Task<IActionResult> ViewClaims()
        {
            var claims = await _context.Claims.ToListAsync();
            return View(claims);
        }

        public async Task<IActionResult> ApprovalDashboard()
        {
            if (HttpContext.Session.GetString("UserRole") != "Coordinator" && HttpContext.Session.GetString("UserRole") != "Manager")
            {
                return RedirectToAction("AccessDenied");
            }
            var pendingClaims = await _context.Claims.Where(c => c.Status == "Pending").ToListAsync();
            foreach (var claim in pendingClaims)
            {
                claim.AutomatedChecks = PerformAutomatedChecks(claim);
            }
            return View(pendingClaims);
        }

        public async Task<IActionResult> HRDashboard()
        {
            if (HttpContext.Session.GetString("UserRole") != "HR")
            {
                return RedirectToAction("AccessDenied");
            }
            var claims = await _context.Claims.ToListAsync();
            return View(claims);
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        // Automated Checks method
        private string PerformAutomatedChecks(Claim claim)
        {
            var checks = new List<string>();

            // Check if hours worked exceed 160
            if (claim.HoursWorked > 160)
            {
                checks.Add("Hours worked exceed 160");
            }

            // Check if hourly rate is within an acceptable range (e.g., between 50 and 500)
            if (claim.HourlyRate < 50m)
            {
                checks.Add($"Hourly rate (R{claim.HourlyRate}) is below the acceptable range (R50 - R500)");
            }
            else if (claim.HourlyRate > 500m)
            {
                checks.Add($"Hourly rate (R{claim.HourlyRate}) is above the acceptable range (R50 - R500)");
            }
            else
            {
                checks.Add($"Hourly rate (R{claim.HourlyRate}) is within the acceptable range (R50 - R500)");
            }

            // Check if supporting document is provided
            if (string.IsNullOrEmpty(claim.SupportingDocumentName))
            {
                checks.Add("Supporting document is missing");
            }

            return string.Join(", ", checks);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitClaim(Claim claim, IFormFile file)
        {
            if (HttpContext.Session.GetString("UserRole") != "Lecturer")
            {
                return RedirectToAction("AccessDenied");
            }

            // Back-end validation: Check if hours worked exceed 160 hours per month
            if (claim.HoursWorked > 160)
            {
                ModelState.AddModelError("HoursWorked", "Hours worked cannot exceed 160.");
            }

            // Ensure hourly rate and hours worked are positive
            if (claim.HourlyRate <= 0 || claim.HoursWorked <= 0)
            {
                ModelState.AddModelError(string.Empty, "Hourly rate and hours worked must be greater than zero.");
            }

            // Check if a file is provided
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("file", "A supporting document is required.");
            }

            // Check if the ModelState is valid before saving the claim
            if (!ModelState.IsValid)
            {
                return View(claim); // Return form with validation errors
            }

            // Handle file upload logic
            claim.SupportingDocumentName = await UploadFileToAzure(file);

            // Add the claim to the database
            _context.Add(claim);
            await _context.SaveChangesAsync();
            return RedirectToAction("ViewClaims");
        }

        private async Task<string> UploadFileToAzure(IFormFile file)
        {
            // Placeholder for Azure file upload logic
            return file.FileName; // Placeholder, return file name for now
        }

        [HttpPost]
        public async Task<IActionResult> UpdateClaimStatus(int claimId, string status)
        {
            if (HttpContext.Session.GetString("UserRole") != "Coordinator" && HttpContext.Session.GetString("UserRole") != "Manager")
            {
                return RedirectToAction("AccessDenied");
            }

            var claim = await _context.Claims.FindAsync(claimId);
            if (claim != null)
            {
                claim.Status = status;

                // If approved, copy the claim to the ApprovedClaims table
                if (status == "Approved")
                {
                    var approvedClaim = new ApprovedClaim
                    {
                        LecturerName = claim.LecturerName,
                        HoursWorked = claim.HoursWorked,
                        HourlyRate = claim.HourlyRate,
                        Notes = claim.Notes,
                        SupportingDocumentName = claim.SupportingDocumentName,
                        SubmissionDate = claim.SubmissionDate,
                        ApprovalDate = DateTime.Now // Set the approval date
                    };

                    _context.ApprovedClaims.Add(approvedClaim); // Add to the approved claims table
                }

                await _context.SaveChangesAsync(); // Save changes to both tables
            }
            return RedirectToAction(nameof(ApprovalDashboard));
        }

        public async Task<IActionResult> EditClaim(int id)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null)
            {
                return NotFound();
            }
            return View(claim);
        }

        [HttpPost]
        public async Task<IActionResult> EditClaim(int id, Claim updatedClaim, IFormFile file)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole == "Lecturer")
            {
                // Lecturer can only edit specific fields
                var claim = await _context.Claims.FindAsync(id);
                if (claim == null)
                {
                    return NotFound();
                }
                claim.LecturerName = updatedClaim.LecturerName;
                claim.HoursWorked = updatedClaim.HoursWorked;
                claim.HourlyRate = updatedClaim.HourlyRate;
                claim.Notes = updatedClaim.Notes;
                // Handle file upload if needed
            }
            else if (userRole == "Coordinator" || userRole == "Manager")
            {
                // Coordinator and Manager can only change status
                var claim = await _context.Claims.FindAsync(id);
                if (claim == null)
                {
                    return NotFound();
                }
                claim.Status = updatedClaim.Status;
            }
            else
            {
                return RedirectToAction("AccessDenied");
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ViewClaims));
        }

        [HttpPost]
        public async Task<IActionResult> RemoveClaim(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "Coordinator" && HttpContext.Session.GetString("UserRole") != "Manager")
            {
                return RedirectToAction("AccessDenied");
            }

            var claim = await _context.Claims.FindAsync(id);
            if (claim != null)
            {
                _context.Claims.Remove(claim);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("ViewClaims");
        }

        [HttpPost]
        public async Task<IActionResult> GenerateInvoice(int claimId)
        {
            if (HttpContext.Session.GetString("UserRole") != "HR")
            {
                return RedirectToAction("AccessDenied");
            }

            var claim = await _context.Claims.FindAsync(claimId);
            if (claim == null || claim.Status != "Approved")
            {
                return NotFound("Claim not found or not approved");
            }

            // Create a basic invoice string
            var invoiceDetails = $"Invoice for Claim ID: {claim.Id}\n" +
                                 $"Lecturer: {claim.LecturerName}\n" +
                                 $"Total Amount: R {(claim.HoursWorked * claim.HourlyRate):F2}\n" +
                                 $"Date: {DateTime.Now}\n";

            // Save the invoice to a text file
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "invoices", $"invoice_{claim.Id}.txt");
            await System.IO.File.WriteAllTextAsync(filePath, invoiceDetails);

            // Return the file to download
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "text/plain", $"invoice_{claim.Id}.txt");
        }
    }
}
