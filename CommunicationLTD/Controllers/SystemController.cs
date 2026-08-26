using CommunicationLTD.Data;
using CommunicationLTD.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunicationLTD.Controllers;

[Authorize(Roles = "Admin")]
public class SystemController : Controller
{
    private readonly CommunicationLtdDbContext _context;

    public SystemController(CommunicationLtdDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        await PopulateDropdownsAsync();

        if (TempData["AddedCustomerName"] is string addedName)
        {
            ViewData["AddedCustomerName"] = addedName;
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(string fullName, string email, string phone, int sectorId, int packageId)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            ModelState.AddModelError(nameof(fullName), "Please enter the customer's full name.");
        }

        if (string.IsNullOrWhiteSpace(email) || !System.Net.Mail.MailAddress.TryCreate(email.Trim(), out _))
        {
            ModelState.AddModelError(nameof(email), "Please enter a valid email address.");
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            ModelState.AddModelError(nameof(phone), "Please enter a phone number.");
        }

        var sectorExists = await _context.Sectors.AnyAsync(s => s.Id == sectorId);
        if (!sectorExists)
        {
            ModelState.AddModelError(nameof(sectorId), "Please choose a valid sector.");
        }

        var packageExists = await _context.InternetPackages.AnyAsync(p => p.Id == packageId);
        if (!packageExists)
        {
            ModelState.AddModelError(nameof(packageId), "Please choose a valid internet package.");
        }

        if (ModelState.IsValid)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            if (await _context.Customers.AnyAsync(c => c.Email == normalizedEmail))
            {
                ModelState.AddModelError(nameof(email), "A customer with this email already exists.");
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return View();
        }

        var customer = new Customer
        {
            FullName = fullName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Phone = phone.Trim(),
            SectorId = sectorId,
            PackageId = packageId
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        TempData["AddedCustomerName"] = customer.FullName;
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync()
    {
        ViewData["Sectors"] = await _context.Sectors.OrderBy(s => s.Name).ToListAsync();
        ViewData["Packages"] = await _context.InternetPackages.OrderBy(p => p.Name).ToListAsync();
    }
}
