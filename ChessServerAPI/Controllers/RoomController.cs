using Microsoft.AspNetCore.Mvc;
using ChessServerAPI.Models;
using ChessServerAPI.Hubs;
using System.Collections.Generic;
using System.Linq;

namespace ChessServerAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoomController : ControllerBase
    {
        [HttpGet]
        public IEnumerable<RoomInfo> GetActiveRooms()
        {
            return ChessHub.ActiveRooms.Values.ToList();
        }
    }
}
