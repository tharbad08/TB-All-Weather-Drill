namespace Timberborn.WaterSourceSystem
{
    public class HazardousWeatherObserver
    {
        private object _weatherService;
        public bool IsBadtideWeather { get; set; }
        public bool IsDroughtWeather { get; set; }
    }
    public class WaterSource : Timberborn.BaseComponentSystem.BaseComponent {}
    public class UnderlyingWaterSource
    {
        public WaterSource WaterSource { get; set; }
        public void DisableDroughtInfluence() {}
    }
}
// The source imports this namespace, but the actual HazardousWeatherObserver type
// belongs to Timberborn.WaterSourceSystem (verified against original DLL metadata).
namespace Timberborn.HazardousWeatherSystem { internal static class NamespaceOnly {} }
