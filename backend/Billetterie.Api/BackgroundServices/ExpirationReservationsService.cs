using Billetterie.Application.Interfaces;

namespace Billetterie.Api.BackgroundServices
{
    public class ExpirationReservationsService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ExpirationReservationsService> _logger;
        private readonly TimeSpan _intervalle;

        public ExpirationReservationsService( IServiceScopeFactory scopeFactory, ILogger<ExpirationReservationsService> logger, IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _intervalle = TimeSpan.FromSeconds( configuration.GetValue("Reservation:IntervalleExpirationSecondes", 60));
            _logger.LogInformation("Service d'expiration : intervalle = {Secondes}s", _intervalle.TotalSeconds);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_intervalle);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IReservationService>();

                    var nombre = await service.ExpirerReservationsAsync(stoppingToken);
                    if (nombre > 0)
                    {
                        _logger.LogInformation("{Nombre} réservation(s) expirée(s).", nombre);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur pendant l'expiration des réservations.");
                }
            }
        }
    }
}