using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SanicballCore.Server;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SanicballServer.App.Controllers
{
    [Route("api/[controller]")]
    public class ServersController : Controller
    {
        private static ConcurrentDictionary<Guid, Server> _servers
            = new ConcurrentDictionary<Guid, Server>();

        private ILoggerFactory _loggerFactory;

        public ServersController(ILoggerFactory loggerFactory)
        {
            if(!_servers.TryGetValue(Guid.Empty, out var server))
            {
                server = new Server(new CommandQueue(), new ServerConfig() { ServerName = "Wan Kerr Co. Ltd.", MaxPlayers = 16 }, loggerFactory.CreateLogger($"Server {Guid.Empty}"));
                _servers[Guid.Empty] = server;

                Task.Run(server.Start);
            }

            _loggerFactory = loggerFactory;
        }

        // GET: api/<controller>
        [HttpGet]
        public IEnumerable<object> Get()
        {
            return _servers.Select(s => new { id = s.Key.ToString(), name = s.Value.Config.ServerName, maxPlayers = s.Value.Config.MaxPlayers, currentPlayers = s.Value.ConnectedClients, inGame = s.Value.InGame });
        }

        // GET api/<controller>/5
        [HttpGet("{id}")]
        public async Task Get(Guid id)
        {
            if (HttpContext.WebSockets.IsWebSocketRequest && _servers.TryGetValue(id, out var server))
            {
                var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await server.ConnectClientAsync(socket);
                await Task.Delay(-1);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        // POST api/<controller>
        [HttpPost]
        public string Post([FromBody]string name)
        {
            var id = Guid.NewGuid();
            var server = new Server(new CommandQueue(), new ServerConfig() { MaxPlayers = 8, ServerName = name }, _loggerFactory.CreateLogger($"Server {id}"));

            _servers[id] = server;

            return id.ToString();
        }

        // DELETE api/<controller>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
