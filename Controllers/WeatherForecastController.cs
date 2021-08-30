using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace PrevisaoTempo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet("SemCache")]
        public IEnumerable<WeatherForecast> SemCache()
        {
            return BuscarPrevisaoTempo();
        }

        [HttpGet("MemoryCache")]
        public IEnumerable<WeatherForecast> MemoryCache([FromServices] IMemoryCache cache)
        {
            return cache.GetOrCreate("PrevisaoTempo", config => {
                config.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(45);
                return BuscarPrevisaoTempo();
            });
        }

        [HttpGet("DistributedCache")]
        public IEnumerable<WeatherForecast> DistributedCache([FromServices] IDistributedCache cache)
        {
            var cached = cache.GetString("PrevisaoTempo");
            if (cached != null) return JsonSerializer.Deserialize<IEnumerable<WeatherForecast>>(cached);
            
            var previsao = BuscarPrevisaoTempo();

            cache.SetString("PrevisaoTempo", JsonSerializer.Serialize(previsao), new DistributedCacheEntryOptions 
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(45)
            });

            return previsao;
        }

        [HttpGet("DistributedCacheEx")]
        public IEnumerable<WeatherForecast> DistributedCacheEx([FromServices] IDistributedCache cache)
        {
            return cache.GetOrCreate("PrevisaoTempo", config => {
                config.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(45);
                return BuscarPrevisaoTempo();
            }, _logger);
        }

        [HttpGet("DistributedCacheMultiplexer")]
        public IEnumerable<WeatherForecast> DistributedCache([FromServices] IConfiguration config)
        {
            var conexaoRedis = ConnectionMultiplexer.Connect(config.GetConnectionString("CacheRedis"));
            var cache = conexaoRedis.GetDatabase();
            var cached = cache.StringGet("PrevisaoTempo");
            if (cached.HasValue) return JsonSerializer.Deserialize<IEnumerable<WeatherForecast>>(cached);

            var previsao = BuscarPrevisaoTempo();

            cache.StringSet("PrevisaoTempo", JsonSerializer.Serialize(previsao), TimeSpan.FromSeconds(45));

            return previsao;
        }

        private IEnumerable<WeatherForecast> BuscarPrevisaoTempo()
        {
            _logger.LogInformation("Buscando a previsão do tempo...");
            
            System.Threading.Thread.Sleep(5000);
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
