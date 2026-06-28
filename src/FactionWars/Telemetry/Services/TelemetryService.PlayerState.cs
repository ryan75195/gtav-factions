namespace FactionWars.Telemetry.Services
{
    public sealed partial class TelemetryService
    {
        private string BuildPlayerPositionDetails()
        {
            if (_getPlayerPosition == null) return string.Empty;

            var position = _getPlayerPosition();
            return Newtonsoft.Json.JsonConvert.SerializeObject(
                new PlayerPositionDetails(position.X, position.Y, position.Z));
        }
    }
}
