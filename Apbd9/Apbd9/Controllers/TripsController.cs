using Apbd9.Data;
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
   public async Task<IActionResult> GetTrips()
   {
      var trips = await _context.Trips.Select();
      return Ok();
   }

}