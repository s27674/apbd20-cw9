using Apbd9.Data;
using Apbd9.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Apbd9.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripsController : ControllerBase
{
    private readonly Apbd9Context _context;

    public TripsController(Apbd9Context context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult> GetTrips([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var trips = await _context.Trips
            .Include(t => t.IdCountries)
            .Include(t => t.ClientTrips)
            .ThenInclude(ct => ct.IdClientNavigation)
            .OrderByDescending(t => t.DateFrom)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalTrips = await _context.Trips.CountAsync();
        var totalPages = (int)Math.Ceiling(totalTrips / (double)pageSize);

        var response = new
        {
            pageNum = page,
            pageSize = pageSize,
            allPages = totalPages,
            trips = trips.Select(t => new
            {
                t.Name,
                t.Description,
                DateFrom = t.DateFrom.ToString("yyyy-MM-dd"),
                DateTo = t.DateTo.ToString("yyyy-MM-dd"),
                t.MaxPeople,
                Countries = t.IdCountries.Select(c => new { c.Name }),
                Clients = t.ClientTrips.Select(ct => new
                    { ct.IdClientNavigation.FirstName, ct.IdClientNavigation.LastName })
            })
        };

        return Ok(response);
    }

    [HttpDelete("{idClient}")]
    public async Task<IActionResult> DeleteClient(int idClient)
    {
        var client = await _context.Clients
            .Include(c => c.ClientTrips)
            .FirstOrDefaultAsync(c => c.IdClient == idClient);

        if (client == null)
        {
            return NotFound(new { message = "Client not found" });
        }

        if (client.ClientTrips.Any())
        {
            return BadRequest(new { message = "Client has trips assigned and cannot be deleted" });
        }

        _context.Clients.Remove(client);
        await _context.SaveChangesAsync();

        return NoContent();
    }


    [HttpPost("{idTrip}/clients")]
    public async Task<IActionResult> AddClientToTrip(int idTrip, [FromBody] Client client,
        [FromQuery] DateTime? paymentDate)
    {
        try
        {
            var existingClient = await _context.Clients
                .FirstOrDefaultAsync(c => c.Pesel == client.Pesel);

            if (existingClient != null)
            {
                var isAlreadyRegistered = await _context.ClientTrips
                    .AnyAsync(ct => ct.IdClient == existingClient.IdClient && ct.IdTrip == idTrip);

                if (isAlreadyRegistered)
                {
                    return BadRequest(new { message = "Client is already registered for this trip" });
                }

                existingClient.FirstName = client.FirstName;
                existingClient.LastName = client.LastName;
                existingClient.Email = client.Email;
                existingClient.Telephone = client.Telephone;
            }
            else
            {
                _context.Clients.Add(client);
                await _context.SaveChangesAsync();
                existingClient = client;
            }

            var trip = await _context.Trips.FindAsync(idTrip);
            if (trip == null || trip.DateFrom < DateTime.Now)
            {
                return BadRequest(new { message = "Trip does not exist or has already occurred" });
            }

            var clientTrip = new ClientTrip
            {
                IdClient = existingClient.IdClient,
                IdTrip = idTrip,
                PaymentDate = paymentDate,
                RegisteredAt = DateTime.Now
            };

            _context.ClientTrips.Add(clientTrip);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Client successfully added to trip" });
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal server error");
        }
    }
    
} 