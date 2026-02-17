using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.Common.ConstantManager;

namespace BrowserAutomationMaster.Managers.Python.BrowserStack
{
    internal class Devices
    {
        // ========================================================================
        // Data Structures
        // ========================================================================

        public struct PlatformSupport
        {
            // Windows
            public bool WindowsXP;
            public bool Windows7;
            public bool Windows8;
            public bool Windows8_1;
            public bool Windows10;
            public bool Windows11;

            // macOS / OS X
            public bool OSX_SnowLeopard;
            public bool OSX_Lion;
            public bool OSX_MountainLion;
            public bool OSX_Mavericks;
            public bool OSX_Yosemite;
            public bool OSX_ElCapitan;
            public bool OSX_Sierra;
            public bool OSX_HighSierra;
            public bool OSX_Mojave;
            public bool OSX_Catalina;
            public bool OSX_BigSur;
            public bool OSX_Monterey;
            public bool OSX_Ventura;
            public bool OSX_Sonoma;
            public bool OSX_Sequoia;
            public bool OSX_Tahoe;
        }

        public class BrowserDefinition
        {
            public required string Name { get; set; }
            public required string Version { get; set; }
            public string DisplayName { get; set; } = "";
            public PlatformSupport Support;
        }

        public class MobileDeviceDefinition
        {
            public required string DeviceName { get; set; }
            public required string OS { get; set; }
            public required string OSVersion { get; set; }
            public List<string> Browsers { get; set; } = [];
        }

        // ========================================================================
        // Hardcoded Data (Originally in browserstack.json)
        // ========================================================================

        public static readonly List<BrowserDefinition> AvailableBrowsers =
        [
            // ------------------------------------------------------------------------
            // Google Chrome (Versions 14.0 -> 145.0 beta)
            // ------------------------------------------------------------------------
            GenerateChrome("145.0 beta", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("144.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("143.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("142.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("141.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("140.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("139.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("138.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("137.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("136.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("135.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("134.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("133.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("132.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("131.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("130.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("129.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("128.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("127.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("126.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("125.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("124.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("123.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("122.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("121.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("120.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("119.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("118.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("117.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("116.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("115.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateChrome("114.0", win7: true, win8: true, win81: true, win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true),
            GenerateChrome("113.0", win7: true, win8: true, win81: true, win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true),
            GenerateChrome("112.0", win7: true, win8: true, win81: true, win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true),
            GenerateChrome("111.0", win7: true, win8: true, win81: true, win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true),
            GenerateChrome("110.0", win7: true, win8: true, win81: true, win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true),
            GenerateChrome("109.0", win7: true, win8: true, win81: true, win10: true, win11: true, macSierra: true, macHighSierra: true, macMojave: true, macCatalina: true, macBigSur: true, macMonterey: true, macVentura: true),
            GenerateChrome("108.0", win7: true, win8: true, win81: true, win10: true, win11: true, macSierra: true, macHighSierra: true, macMojave: true, macCatalina: true, macBigSur: true, macMonterey: true, macVentura: true),
            GenerateChrome("107.0", win7: true, win8: true, win81: true, win10: true, win11: true, macSierra: true, macHighSierra: true, macMojave: true, macCatalina: true, macBigSur: true, macMonterey: true, macVentura: true),
            GenerateChrome("106.0", win7: true, win8: true, win81: true, win10: true, win11: true, macSierra: true, macHighSierra: true, macMojave: true, macCatalina: true, macBigSur: true, macMonterey: true, macVentura: true),
            GenerateChrome("105.0", win7: true, win8: true, win81: true, win10: true, win11: true, macSierra: true, macHighSierra: true, macMojave: true, macCatalina: true, macBigSur: true, macMonterey: true, macVentura: true),
            GenerateChrome("104.0", win7: true, win8: true, win81: true, win10: true, win11: true, macSierra: true, macHighSierra: true, macMojave: true, macCatalina: true, macBigSur: true, macMonterey: true, macVentura: true),
            GenerateChrome("103.0", win7: true, win8: true, win81: true, win10: true, win11: true, macYosemite: true, macElCapitan: true, macSierra: true, macHighSierra: true, macMojave: true, macCatalina: true, macBigSur: true, macMonterey: true),
            GenerateChrome("102.0", win7: true, win8: true, win81: true, win10: true, win11: true, macYosemite: true, macElCapitan: true, macSierra: true, macHighSierra: true, macMojave: true, macCatalina: true, macBigSur: true, macMonterey: true),
            GenerateChrome("101.0", win7: true, win8: true, win81: true, win10: true, win11: true, macYosemite: true, macElCapitan: true, macSierra: true, macHighSierra: true, macMojave: true, macCatalina: true, macBigSur: true, macMonterey: true),
            GenerateChrome("100.0", win7: true, win8: true, win81: true, win10: true, win11: true, macYosemite: true, macElCapitan: true, macSierra: true, macHighSierra: true, macMojave: true, macCatalina: true, macBigSur: true, macMonterey: true),
            GenerateChrome("99.0", win7: true, win8: true, win81: true, win10: true, win11: true, macSierra: true, macHighSierra: true, macMojave: true, macCatalina: true, macBigSur: true, macMonterey: true),
            GenerateChrome("90.0", win7: true, win8: true, win81: true, win10: true, win11: true, macSierra: true, macHighSierra: true, macMojave: true, macCatalina: true, macBigSur: true),
            GenerateChrome("80.0", win7: true, win8: true, win81: true, win10: true, win11: true, macHighSierra: true, macMojave: true, macCatalina: true),
            GenerateChrome("70.0", win7: true, win8: true, win81: true, win10: true, macSierra: true, macHighSierra: true, macMojave: true),
            GenerateChrome("60.0", win7: true, win8: true, win81: true, win10: true, macElCapitan: true, macSierra: true, macHighSierra: true),
            GenerateChrome("50.0", winXP: true, win7: true, win8: true, win81: true, win10: true, macYosemite: true, macElCapitan: true),
            GenerateChrome("40.0", winXP: true, win7: true, win8: true, win81: true, win10: true, macMavericks: true, macYosemite: true),
            GenerateChrome("30.0", winXP: true, win7: true, win8: true, win81: true, macMountainLion: true, macMavericks: true),
            GenerateChrome("22.0", winXP: true, win7: true, win8: true, macLion: true, macMountainLion: true),
            GenerateChrome("14.0", winXP: true, win7: true, macSnowLeopard: true, macLion: true),

            // ------------------------------------------------------------------------
            // Mozilla Firefox (Versions 3.6 -> 148.0 beta)
            // ------------------------------------------------------------------------
            GenerateFirefox("148.0 beta", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("147.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("146.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("145.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("144.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("143.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("142.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("141.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("140.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("139.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("138.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("137.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("136.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("135.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("134.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("133.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("132.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("131.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("130.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("129.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("128.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("127.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("126.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("125.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("124.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("123.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("122.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("121.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("120.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("119.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("118.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("117.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateFirefox("116.0", win7: true, win8: true, win81: true, win10: true, win11: true, macSierra: true, macHighSierra: true, macMojave: true, macCatalina: true, macBigSur: true, macMonterey: true, macVentura: true),
            GenerateFirefox("115.0", win7: true, win8: true, win81: true, win10: true, win11: true, macSierra: true, macHighSierra: true, macMojave: true, macCatalina: true, macBigSur: true, macMonterey: true, macVentura: true),
            GenerateFirefox("110.0", win7: true, win8: true, win81: true, win10: true, win11: true, macSierra: true, macHighSierra: true, macMojave: true, macCatalina: true, macBigSur: true, macMonterey: true),
            GenerateFirefox("100.0", win7: true, win8: true, win81: true, win10: true, win11: true, macSierra: true, macHighSierra: true, macMojave: true, macCatalina: true, macBigSur: true, macMonterey: true),
            GenerateFirefox("90.0", win7: true, win8: true, win81: true, win10: true, win11: true, macSierra: true, macHighSierra: true, macMojave: true, macCatalina: true, macBigSur: true),
            GenerateFirefox("80.0", win7: true, win8: true, win81: true, win10: true, win11: true, macSierra: true, macHighSierra: true, macMojave: true, macCatalina: true),
            GenerateFirefox("70.0", win7: true, win8: true, win81: true, win10: true, macSierra: true, macHighSierra: true, macMojave: true),
            GenerateFirefox("60.0", win7: true, win8: true, win81: true, win10: true, macElCapitan: true, macSierra: true, macHighSierra: true),
            GenerateFirefox("50.0", winXP: true, win7: true, win8: true, win81: true, win10: true, macYosemite: true, macElCapitan: true, macSierra: true),
            GenerateFirefox("40.0", winXP: true, win7: true, win8: true, win81: true, win10: true, macMavericks: true, macYosemite: true, macElCapitan: true),
            GenerateFirefox("30.0", winXP: true, win7: true, win8: true, win81: true, macSnowLeopard: true, macLion: true, macMountainLion: true, macMavericks: true),
            GenerateFirefox("20.0", winXP: true, win7: true, win8: true, macSnowLeopard: true, macLion: true, macMountainLion: true),
            GenerateFirefox("10.0", winXP: true, win7: true, macSnowLeopard: true, macLion: true),
            GenerateFirefox("3.6", winXP: true, macSnowLeopard: true),

            // ------------------------------------------------------------------------
            // Microsoft Edge (Versions 15.0 -> 145.0 beta)
            // ------------------------------------------------------------------------
            GenerateEdge("145.0 beta", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("144.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("143.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("142.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("141.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("140.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("139.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("138.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("137.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("136.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("135.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("134.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("133.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("132.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("131.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("130.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("129.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("128.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("127.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("126.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("125.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("124.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("123.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("122.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("121.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("120.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("119.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("118.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("117.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("116.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("115.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("114.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("113.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("112.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("111.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("110.0", win10: true, win11: true, macBigSur: true, macMonterey: true, macVentura: true, macSonoma: true, macSequoia: true),
            GenerateEdge("109.0", win7: true, win8: true, win81: true, win10: true, win11: true, macSierra: true, macHighSierra: true, macMojave: true, macCatalina: true, macBigSur: true, macMonterey: true, macVentura: true),
            GenerateEdge("100.0", win7: true, win8: true, win81: true, win10: true, win11: true, macSierra: true, macHighSierra: true, macMojave: true, macCatalina: true, macBigSur: true, macMonterey: true),
            GenerateEdge("90.0", win7: true, win8: true, win81: true, win10: true, win11: true, macSierra: true, macHighSierra: true, macMojave: true, macCatalina: true, macBigSur: true),
            GenerateEdge("80.0", win7: true, win8: true, win81: true, win10: true, win11: true, macSierra: true, macHighSierra: true, macMojave: true, macCatalina: true),
            // Legacy Edge
            GenerateEdge("18.0", win10: true),
            GenerateEdge("17.0", win10: true),
            GenerateEdge("16.0", win10: true),
            GenerateEdge("15.0", win10: true),

            // ------------------------------------------------------------------------
            // Safari (Tied to macOS Version)
            // ------------------------------------------------------------------------
            GenerateSafari("18.4", macSequoia: true),
            GenerateSafari("18.0", macSequoia: true),
            GenerateSafari("17.0", macSonoma: true),
            GenerateSafari("16.5", macVentura: true),
            GenerateSafari("16.0", macVentura: true),
            GenerateSafari("15.0", macMonterey: true),
            GenerateSafari("14.1", macBigSur: true),
            GenerateSafari("13.1", macCatalina: true),
            GenerateSafari("12.1", macMojave: true),
            GenerateSafari("11.1", macHighSierra: true),
            GenerateSafari("10.1", macSierra: true),
            GenerateSafari("9.1", macElCapitan: true),
            GenerateSafari("8.0", macYosemite: true),
            GenerateSafari("7.1", macMavericks: true),
            GenerateSafari("6.0", macLion: true, macMountainLion: true),
            GenerateSafari("5.1", winXP: true, win7: true, macSnowLeopard: true, macLion: true),

            // ------------------------------------------------------------------------
            // Internet Explorer
            // ------------------------------------------------------------------------
            GenerateIE("11.0", win7: true, win8: true, win81: true, win10: true),
            GenerateIE("10.0", win7: true, win8: true),
            GenerateIE("9.0", win7: true),
            GenerateIE("8.0", winXP: true, win7: true),
            GenerateIE("7.0", winXP: true),
            GenerateIE("6.0", winXP: true),

            // ------------------------------------------------------------------------
            // Opera
            // ------------------------------------------------------------------------
            GenerateOpera("12.16", winXP: true, win7: true, win8: true, win81: true, macSnowLeopard: true, macLion: true, macMountainLion: true),
            GenerateOpera("12.15", winXP: true, win7: true, win8: true, win81: true, macSnowLeopard: true, macLion: true, macMountainLion: true)
        ];

        public static readonly List<MobileDeviceDefinition> MobileDevices =
        [
            // ------------------------------------------------------------------------
            // Google Pixel
            // ------------------------------------------------------------------------
            new() { DeviceName = "Google Pixel 10 Pro XL", OS = "android", OSVersion = "16.0", Browsers = ["Chrome"] },
            new() { DeviceName = "Google Pixel 10 Pro", OS = "android", OSVersion = "16.0", Browsers = ["Chrome"] },
            new() { DeviceName = "Google Pixel 10", OS = "android", OSVersion = "16.0", Browsers = ["Chrome"] },
            new() { DeviceName = "Google Pixel 9 Pro XL", OS = "android", OSVersion = "15.0", Browsers = ["Chrome"] },
            new() { DeviceName = "Google Pixel 9 Pro", OS = "android", OSVersion = "15.0", Browsers = ["Chrome"] },
            new() { DeviceName = "Google Pixel 9", OS = "android", OSVersion = "16.0", Browsers = ["Chrome"] },
            new() { DeviceName = "Google Pixel 9", OS = "android", OSVersion = "15.0", Browsers = ["Chrome"] },
            new() { DeviceName = "Google Pixel 8 Pro", OS = "android", OSVersion = "14.0", Browsers = ["Chrome"] },
            new() { DeviceName = "Google Pixel 8", OS = "android", OSVersion = "14.0", Browsers = ["Chrome"] },
            new() { DeviceName = "Google Pixel 7 Pro", OS = "android", OSVersion = "13.0", Browsers = ["Chrome"] },
            new() { DeviceName = "Google Pixel 7", OS = "android", OSVersion = "13.0", Browsers = ["Chrome"] },
            new() { DeviceName = "Google Pixel 6 Pro", OS = "android", OSVersion = "15.0", Browsers = ["Chrome"] },
            new() { DeviceName = "Google Pixel 6 Pro", OS = "android", OSVersion = "13.0", Browsers = ["Chrome"] },
            new() { DeviceName = "Google Pixel 6 Pro", OS = "android", OSVersion = "12.0", Browsers = ["Chrome"] },
            new() { DeviceName = "Google Pixel 6", OS = "android", OSVersion = "12.0", Browsers = ["Chrome"] },
            new() { DeviceName = "Google Pixel 5", OS = "android", OSVersion = "11.0", Browsers = ["Chrome"] },

            // ------------------------------------------------------------------------
            // Samsung Galaxy S-Series
            // ------------------------------------------------------------------------
            new() { DeviceName = "Samsung Galaxy S25 Ultra", OS = "android", OSVersion = "15.0", Browsers = ["Chrome", "Samsung"] },
            new() { DeviceName = "Samsung Galaxy S25", OS = "android", OSVersion = "15.0", Browsers = ["Chrome", "Samsung"] },
            new() { DeviceName = "Samsung Galaxy S24 Ultra", OS = "android", OSVersion = "14.0", Browsers = ["Chrome", "Samsung"] },
            new() { DeviceName = "Samsung Galaxy S24", OS = "android", OSVersion = "14.0", Browsers = ["Chrome", "Samsung"] },
            new() { DeviceName = "Samsung Galaxy S23 Ultra", OS = "android", OSVersion = "13.0", Browsers = ["Chrome", "Samsung"] },
            new() { DeviceName = "Samsung Galaxy S23 Plus", OS = "android", OSVersion = "13.0", Browsers = ["Chrome", "Samsung"] },
            new() { DeviceName = "Samsung Galaxy S23", OS = "android", OSVersion = "13.0", Browsers = ["Chrome", "Samsung"] },
            new() { DeviceName = "Samsung Galaxy S22 Ultra", OS = "android", OSVersion = "12.0", Browsers = ["Chrome", "Samsung"] },
            new() { DeviceName = "Samsung Galaxy S22 Plus", OS = "android", OSVersion = "12.0", Browsers = ["Chrome", "Samsung"] },
            new() { DeviceName = "Samsung Galaxy S22", OS = "android", OSVersion = "12.0", Browsers = ["Chrome", "Samsung"] },
            new() { DeviceName = "Samsung Galaxy S21 Ultra", OS = "android", OSVersion = "11.0", Browsers = ["Chrome", "Samsung"] },
            new() { DeviceName = "Samsung Galaxy S21 Plus", OS = "android", OSVersion = "11.0", Browsers = ["Chrome", "Samsung"] },
            new() { DeviceName = "Samsung Galaxy S21", OS = "android", OSVersion = "12.0", Browsers = ["Chrome", "Samsung"] },
            new() { DeviceName = "Samsung Galaxy S21", OS = "android", OSVersion = "11.0", Browsers = ["Chrome", "Samsung"] },
            new() { DeviceName = "Samsung Galaxy S20 Ultra", OS = "android", OSVersion = "10.0", Browsers = ["Chrome", "Samsung"] },
            new() { DeviceName = "Samsung Galaxy S20", OS = "android", OSVersion = "10.0", Browsers = ["Chrome", "Samsung"] },
            new() { DeviceName = "Samsung Galaxy S10", OS = "android", OSVersion = "9.0", Browsers = ["Chrome", "Samsung"] },

            // ------------------------------------------------------------------------
            // Samsung Galaxy Note, A, M Series
            // ------------------------------------------------------------------------
            new() { DeviceName = "Samsung Galaxy Note 20", OS = "android", OSVersion = "10.0", Browsers = ["Chrome", "Samsung"] },
            new() { DeviceName = "Samsung Galaxy Note 9", OS = "android", OSVersion = "8.1", Browsers = ["Chrome", "Samsung"] },
            new() { DeviceName = "Samsung Galaxy A52", OS = "android", OSVersion = "11.0", Browsers = ["Chrome", "Samsung"] },
            new() { DeviceName = "Samsung Galaxy A51", OS = "android", OSVersion = "10.0", Browsers = ["Chrome", "Samsung"] },
            new() { DeviceName = "Samsung Galaxy A11", OS = "android", OSVersion = "10.0", Browsers = ["Chrome"] },
            new() { DeviceName = "Samsung Galaxy A10", OS = "android", OSVersion = "9.0", Browsers = ["Chrome", "Samsung"] },
            new() { DeviceName = "Samsung Galaxy M52", OS = "android", OSVersion = "11.0", Browsers = ["Chrome", "Samsung"] },
            new() { DeviceName = "Samsung Galaxy M32", OS = "android", OSVersion = "11.0", Browsers = ["Chrome", "Samsung"] },

            // ------------------------------------------------------------------------
            // Apple iPhone
            // ------------------------------------------------------------------------
            new() { DeviceName = "iPhone 17 Pro Max", OS = "ios", OSVersion = "26", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 17 Pro", OS = "ios", OSVersion = "26", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 17", OS = "ios", OSVersion = "26", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone Air", OS = "ios", OSVersion = "26", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 16 Pro Max", OS = "ios", OSVersion = "18", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 16 Pro", OS = "ios", OSVersion = "18", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 16 Plus", OS = "ios", OSVersion = "18", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 16", OS = "ios", OSVersion = "18", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 16e", OS = "ios", OSVersion = "18", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 15 Pro Max", OS = "ios", OSVersion = "26", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 15 Pro Max", OS = "ios", OSVersion = "17", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 15 Pro", OS = "ios", OSVersion = "17", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 15 Plus", OS = "ios", OSVersion = "17", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 15", OS = "ios", OSVersion = "26", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 15", OS = "ios", OSVersion = "17", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 14 Pro Max", OS = "ios", OSVersion = "16", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 14 Pro", OS = "ios", OSVersion = "26", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 14 Pro", OS = "ios", OSVersion = "16", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 14 Plus", OS = "ios", OSVersion = "16", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 14", OS = "ios", OSVersion = "26", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 14", OS = "ios", OSVersion = "18", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 14", OS = "ios", OSVersion = "16", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 13 Pro Max", OS = "ios", OSVersion = "18", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 13 Pro Max", OS = "ios", OSVersion = "15", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 13 Pro", OS = "ios", OSVersion = "15", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 13 Mini", OS = "ios", OSVersion = "15", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 13", OS = "ios", OSVersion = "18", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 13", OS = "ios", OSVersion = "17", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 13", OS = "ios", OSVersion = "16", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 13", OS = "ios", OSVersion = "15", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 12 Pro Max", OS = "ios", OSVersion = "14", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 12 Pro", OS = "ios", OSVersion = "18", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 12 Pro", OS = "ios", OSVersion = "17", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 12 Pro", OS = "ios", OSVersion = "14", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 12 Mini", OS = "ios", OSVersion = "14", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 12", OS = "ios", OSVersion = "17", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 12", OS = "ios", OSVersion = "14", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 11 Pro Max", OS = "ios", OSVersion = "13", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 11 Pro", OS = "ios", OSVersion = "13", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 11", OS = "ios", OSVersion = "13", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone XS Max", OS = "ios", OSVersion = "12", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone XS", OS = "ios", OSVersion = "12", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone XR", OS = "ios", OSVersion = "12", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone X", OS = "ios", OSVersion = "11", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 8 Plus", OS = "ios", OSVersion = "11", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 8", OS = "ios", OSVersion = "11", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPhone 7", OS = "ios", OSVersion = "10", Browsers = ["Safari"] },
            new() { DeviceName = "iPhone SE 2022", OS = "ios", OSVersion = "15", Browsers = ["Safari"] },
            new() { DeviceName = "iPhone SE 2020", OS = "ios", OSVersion = "16", Browsers = ["Safari"] },
            new() { DeviceName = "iPhone SE 2020", OS = "ios", OSVersion = "13", Browsers = ["Safari"] },

            // ------------------------------------------------------------------------
            // Apple iPad
            // ------------------------------------------------------------------------
            new() { DeviceName = "iPad Pro 13 2025", OS = "ios", OSVersion = "26", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPad Pro 11 2025", OS = "ios", OSVersion = "26", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPad Air 13 2025", OS = "ios", OSVersion = "26", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPad Air 13 2025", OS = "ios", OSVersion = "18", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPad Pro 13 2024", OS = "ios", OSVersion = "17", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPad Pro 11 2024", OS = "ios", OSVersion = "17", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPad Air 6", OS = "ios", OSVersion = "17", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPad Pro 12.9 2022", OS = "ios", OSVersion = "16", Browsers = ["Safari"] },
            new() { DeviceName = "iPad Pro 11 2022", OS = "ios", OSVersion = "16", Browsers = ["Safari"] },
            new() { DeviceName = "iPad 10th", OS = "ios", OSVersion = "16", Browsers = ["Safari"] },
            new() { DeviceName = "iPad Pro 12.9 2021", OS = "ios", OSVersion = "18", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPad Pro 12.9 2021", OS = "ios", OSVersion = "17", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPad Pro 12.9 2021", OS = "ios", OSVersion = "14", Browsers = ["Safari"] },
            new() { DeviceName = "iPad Pro 11 2021", OS = "ios", OSVersion = "18", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPad Pro 11 2021", OS = "ios", OSVersion = "14", Browsers = ["Safari"] },
            new() { DeviceName = "iPad Mini 2021", OS = "ios", OSVersion = "15", Browsers = ["Safari"] },
            new() { DeviceName = "iPad Air 5", OS = "ios", OSVersion = "26", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPad Air 5", OS = "ios", OSVersion = "15", Browsers = ["Safari"] },
            new() { DeviceName = "iPad 9th", OS = "ios", OSVersion = "18", Browsers = ["Safari", "Chrome"] },
            new() { DeviceName = "iPad 9th", OS = "ios", OSVersion = "15", Browsers = ["Safari"] },
            new() { DeviceName = "iPad Pro 12.9 2020", OS = "ios", OSVersion = "16", Browsers = ["Safari"] },
            new() { DeviceName = "iPad Pro 12.9 2020", OS = "ios", OSVersion = "14", Browsers = ["Safari"] },
            new() { DeviceName = "iPad Pro 12.9 2020", OS = "ios", OSVersion = "13", Browsers = ["Safari"] },
            new() { DeviceName = "iPad Pro 11 2020", OS = "ios", OSVersion = "16", Browsers = ["Safari"] },
            new() { DeviceName = "iPad Pro 11 2020", OS = "ios", OSVersion = "13", Browsers = ["Safari"] },
            new() { DeviceName = "iPad Air 4", OS = "ios", OSVersion = "14", Browsers = ["Safari"] },
            new() { DeviceName = "iPad 8th", OS = "ios", OSVersion = "16", Browsers = ["Safari"] },
            new() { DeviceName = "iPad 8th", OS = "ios", OSVersion = "14", Browsers = ["Safari"] },
            new() { DeviceName = "iPad 7th", OS = "ios", OSVersion = "13", Browsers = ["Safari"] },
            new() { DeviceName = "iPad 6th", OS = "ios", OSVersion = "11", Browsers = ["Safari"] },

            // ------------------------------------------------------------------------
            // OnePlus, Motorola, Vivo, Oppo, Xiaomi, Huawei
            // ------------------------------------------------------------------------
            new() { DeviceName = "OnePlus 13R", OS = "android", OSVersion = "15.0", Browsers = ["Chrome"] },
            new() { DeviceName = "OnePlus 12R", OS = "android", OSVersion = "14.0", Browsers = ["Chrome"] },
            new() { DeviceName = "OnePlus 11R", OS = "android", OSVersion = "13.0", Browsers = ["Chrome"] },
            new() { DeviceName = "OnePlus 9", OS = "android", OSVersion = "11.0", Browsers = ["Chrome"] },
            new() { DeviceName = "OnePlus 8", OS = "android", OSVersion = "10.0", Browsers = ["Chrome"] },
            new() { DeviceName = "Motorola Moto G71 5G", OS = "android", OSVersion = "11.0", Browsers = ["Chrome"] },
            new() { DeviceName = "Motorola Moto G9 Play", OS = "android", OSVersion = "10.0", Browsers = ["Chrome"] },
            new() { DeviceName = "Vivo Y21", OS = "android", OSVersion = "11.0", Browsers = ["Chrome"] },
            new() { DeviceName = "Vivo V21", OS = "android", OSVersion = "11.0", Browsers = ["Chrome"] },
            new() { DeviceName = "Vivo Y50", OS = "android", OSVersion = "10.0", Browsers = ["Chrome"] },
            new() { DeviceName = "Oppo Reno 6", OS = "android", OSVersion = "11.0", Browsers = ["Chrome"] },
            new() { DeviceName = "Oppo A96", OS = "android", OSVersion = "11.0", Browsers = ["Chrome"] },
            new() { DeviceName = "Oppo Reno 3 Pro", OS = "android", OSVersion = "10.0", Browsers = ["Chrome"] },
            new() { DeviceName = "Xiaomi Redmi Note 11", OS = "android", OSVersion = "11.0", Browsers = ["Chrome"] },
            new() { DeviceName = "Xiaomi Redmi Note 9", OS = "android", OSVersion = "10.0", Browsers = ["Chrome"] },
            new() { DeviceName = "Huawei P30", OS = "android", OSVersion = "9.0", Browsers = ["Chrome"] },

            // ------------------------------------------------------------------------
            // Android Tablets
            // ------------------------------------------------------------------------
            new() { DeviceName = "Samsung Galaxy Tab S9", OS = "android", OSVersion = "13.0", Browsers = ["Chrome", "Samsung"] },
            new() { DeviceName = "Samsung Galaxy Tab S8", OS = "android", OSVersion = "12.0", Browsers = ["Chrome", "Samsung"] }
        ];


        // ========================================================================
        // Helper Methods for Data Generation
        // ========================================================================

        private static BrowserDefinition GenerateChrome(string version, bool winXP = false, bool win7 = false, bool win8 = false, bool win81 = false, bool win10 = false, bool win11 = false, bool macSnowLeopard = false, bool macLion = false, bool macMountainLion = false, bool macMavericks = false, bool macYosemite = false, bool macElCapitan = false, bool macSierra = false, bool macHighSierra = false, bool macMojave = false, bool macCatalina = false, bool macBigSur = false, bool macMonterey = false, bool macVentura = false, bool macSonoma = false, bool macSequoia = false, bool macTahoe = false)
        {
            return new BrowserDefinition { 
                Name = "Chrome", 
                Version = version, 
                DisplayName = $"Chrome {version}", 
                Support = new PlatformSupport 
                { 
                    WindowsXP = winXP, 
                    Windows7 = win7, 
                    Windows8 = win8, 
                    Windows8_1 = win81, 
                    Windows10 = win10, 
                    Windows11 = win11, 
                    OSX_SnowLeopard = macSnowLeopard, 
                    OSX_Lion = macLion, 
                    OSX_MountainLion = macMountainLion, 
                    OSX_Mavericks = macMavericks, 
                    OSX_Yosemite = macYosemite, 
                    OSX_ElCapitan = macElCapitan, 
                    OSX_Sierra = macSierra, 
                    OSX_HighSierra = macHighSierra, 
                    OSX_Mojave = macMojave, 
                    OSX_Catalina = macCatalina, 
                    OSX_BigSur = macBigSur, 
                    OSX_Monterey = macMonterey, 
                    OSX_Ventura = macVentura, 
                    OSX_Sonoma = macSonoma, 
                    OSX_Sequoia = macSequoia, 
                    OSX_Tahoe = macTahoe 
                } 
            };
        }

        private static BrowserDefinition GenerateFirefox(string version, bool winXP = false, bool win7 = false, bool win8 = false, bool win81 = false, bool win10 = false, bool win11 = false, bool macSnowLeopard = false, bool macLion = false, bool macMountainLion = false, bool macMavericks = false, bool macYosemite = false, bool macElCapitan = false, bool macSierra = false, bool macHighSierra = false, bool macMojave = false, bool macCatalina = false, bool macBigSur = false, bool macMonterey = false, bool macVentura = false, bool macSonoma = false, bool macSequoia = false, bool macTahoe = false)
        {
            return new BrowserDefinition { 
                Name = "Firefox", 
                Version = version, 
                DisplayName = $"Firefox {version}", 
                Support = new PlatformSupport 
                { 
                    WindowsXP = winXP, 
                    Windows7 = win7, 
                    Windows8 = win8, 
                    Windows8_1 = win81, 
                    Windows10 = win10, 
                    Windows11 = win11, 
                    OSX_SnowLeopard = macSnowLeopard, 
                    OSX_Lion = macLion, 
                    OSX_MountainLion = macMountainLion, 
                    OSX_Mavericks = macMavericks, 
                    OSX_Yosemite = macYosemite, 
                    OSX_ElCapitan = macElCapitan, 
                    OSX_Sierra = macSierra, 
                    OSX_HighSierra = macHighSierra, 
                    OSX_Mojave = macMojave, 
                    OSX_Catalina = macCatalina, 
                    OSX_BigSur = macBigSur, 
                    OSX_Monterey = macMonterey, 
                    OSX_Ventura = macVentura, 
                    OSX_Sonoma = macSonoma, 
                    OSX_Sequoia = macSequoia, 
                    OSX_Tahoe = macTahoe 
                } 
            };
        }

        private static BrowserDefinition GenerateEdge(string version, bool win7 = false, bool win8 = false, bool win81 = false, bool win10 = false, bool win11 = false, bool macSierra = false, bool macHighSierra = false, bool macMojave = false, bool macCatalina = false, bool macBigSur = false, bool macMonterey = false, bool macVentura = false, bool macSonoma = false, bool macSequoia = false, bool macTahoe = false)
        {
            return new BrowserDefinition { 
                Name = "Edge", 
                Version = version, 
                DisplayName = $"Edge {version}", 
                Support = new PlatformSupport 
                { 
                    Windows7 = win7, 
                    Windows8 = win8, 
                    Windows8_1 = win81, 
                    Windows10 = win10, 
                    Windows11 = win11, 
                    OSX_Sierra = macSierra, 
                    OSX_HighSierra = macHighSierra, 
                    OSX_Mojave = macMojave, 
                    OSX_Catalina = macCatalina, 
                    OSX_BigSur = macBigSur, 
                    OSX_Monterey = macMonterey, 
                    OSX_Ventura = macVentura, 
                    OSX_Sonoma = macSonoma, 
                    OSX_Sequoia = macSequoia, 
                    OSX_Tahoe = macTahoe 
                } 
            };
        }

        private static BrowserDefinition GenerateSafari(string version, bool winXP = false, bool win7 = false, bool macSnowLeopard = false, bool macLion = false, bool macMountainLion = false, bool macMavericks = false, bool macYosemite = false, bool macElCapitan = false, bool macSierra = false, bool macHighSierra = false, bool macMojave = false, bool macCatalina = false, bool macBigSur = false, bool macMonterey = false, bool macVentura = false, bool macSonoma = false, bool macSequoia = false, bool macTahoe = false)
        {
            return new BrowserDefinition { 
                Name = "Safari", 
                Version = version, 
                DisplayName = $"Safari {version}", 
                Support = new PlatformSupport 
                { 
                    WindowsXP = winXP, 
                    Windows7 = win7, 
                    OSX_SnowLeopard = macSnowLeopard, 
                    OSX_Lion = macLion, 
                    OSX_MountainLion = macMountainLion, 
                    OSX_Mavericks = macMavericks, 
                    OSX_Yosemite = macYosemite, 
                    OSX_ElCapitan = macElCapitan, 
                    OSX_Sierra = macSierra, 
                    OSX_HighSierra = macHighSierra, 
                    OSX_Mojave = macMojave, 
                    OSX_Catalina = macCatalina, 
                    OSX_BigSur = macBigSur, 
                    OSX_Monterey = macMonterey, 
                    OSX_Ventura = macVentura, 
                    OSX_Sonoma = macSonoma, 
                    OSX_Sequoia = macSequoia, 
                    OSX_Tahoe = macTahoe 
                } 
            };
        }

        private static BrowserDefinition GenerateIE(string version, bool winXP = false, bool win7 = false, bool win8 = false, bool win81 = false, bool win10 = false)
        {
            return new BrowserDefinition { 
                Name = "IE", 
                Version = version, 
                DisplayName = $"Internet Explorer {version}", 
                Support = new PlatformSupport { 
                    WindowsXP = winXP, 
                    Windows7 = win7, 
                    Windows8 = win8, 
                    Windows8_1 = win81, 
                    Windows10 = win10 
                } 
            };
        }

        private static BrowserDefinition GenerateOpera(string version, bool winXP = false, bool win7 = false, bool win8 = false, bool win81 = false, bool macSnowLeopard = false, bool macLion = false, bool macMountainLion = false)
        {
            return new BrowserDefinition { 
                Name = "Opera", 
                Version = version, 
                DisplayName = $"Opera {version}", 
                Support = new PlatformSupport { 
                    WindowsXP = winXP, 
                    Windows7 = win7, 
                    Windows8 = win8, 
                    Windows8_1 = win81, 
                    OSX_SnowLeopard = macSnowLeopard, 
                    OSX_Lion = macLion, 
                    OSX_MountainLion = macMountainLion 
                } 
            };
        }

        // ========================================================================
        // Logic & Helpers
        // ========================================================================

        public static string[] OSNames = ["Android", "iOS", "MacOS", "Windows"];
        public static string[] WindowsVersions = ["11", "10", "8.1", "8", "7", "XP"];
        
        // Updated iOS Versions to match expanded device list
        public static string[] iOSVersions = ["26", "18", "17", "16", "15", "14", "13", "12", "11", "10"];
        
        public static string[] MacOSVersions = [
            "26 (Tahoe)",
            "15 (Sequoia)",
            "14 (Sonoma)",
            "13 (Ventura)",
            "12 (Monterey)",
            "11 (Big Sur)",
            "10.15 (Catalina)",
            "10.14 (Mojave)",
            "10.13 (High Sierra)",
            "10.12 (Sierra)",
            "10.11 (El Capitan)",
            "10.10 (Yosemite)",
            "10.9 (Mavericks)",
            "10.8 (Mountain Lion)",
            "10.7 (Lion)",
            "10.6 (Snow Leopard)"
        ];
        
        // Updated Android Versions to match expanded device list
        public static string[] AndroidVersions =
        [
            "16.0", "15.0", "14.0", "13.0", "12.0", "11.0", "10.0", "9.0", "8.1", "8.0"
        ];

        public static string SanitizeOSName(string rawOSName)
        {
            return rawOSName switch
            {
                "Android" => "android",
                "iOS" => "ios",
                "MacOS" => "OS X",
                "Windows" => "Windows",
                _ => "Windows"
            };
        }

        public static string SanitizeOSVersion(string rawOSVersion, string rawOSName, string[] versions)
        {
            if (rawOSVersion.EndsWith("Beta")) return rawOSVersion;

            bool isAndroid = rawOSName.Equals("android", StringComparison.OrdinalIgnoreCase);
            var osVersion = GetVersionNumber(rawOSVersion, isAndroid);

            if (osVersion == "Not Found")
            {
                Warning.Write($"No version number was provided, using the most recent version of {rawOSName}.");
                return versions.FirstOrDefault() ?? "latest";
            }

            return osVersion;
        }

        public static string GetDesiredBrowser(string rawOSName)
        {
            static string GetBrowser(string[] browsers) => Input.WriteListFromOptions(browsers, noun: "browser");

            return rawOSName switch
            {
                "Android" => "Chrome",
                "iOS" => GetBrowser(["Chrome", "Safari"]),
                "MacOS" => GetBrowser(["Chrome", "Firefox", "Safari", "Edge"]),
                "Windows" or _ => GetBrowser(["Chrome", "Firefox", "Edge", "IE"]),
            };
        }

        public static string GetVersionNumber(string versionString, bool isAndroid = false)
        {
            var chars = versionString.AsSpan();
            int index = 0;
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsNumber(chars[i]) && chars[i] != '.')
                {
                    index = i;
                    break;
                }
                index++;
            }

            if (index == 0) return "Not Found";

            var version = chars[..index].ToString();
            // Android versions in BS often use .0 suffix (e.g. 13.0)
            if (isAndroid && !version.Contains('.')) return version + ".0";
            return version;
        }

        public class DeviceHelper()
        {
            public static string[] GetMobileDeviceNames()
            {
                return [.. MobileDevices.Select(d => d.DeviceName).Distinct()];
            }

            public static string[] GetAndroidDeviceNames(string osVersion, string browserName)
            {
                return [.. MobileDevices
                    .Where(m => m.OS.Equals("android", OIC) &&
                                m.OSVersion.Equals(osVersion, OIC) &&
                                m.Browsers.Any(b => b.Contains(browserName, OIC)))
                    .Select(d => d.DeviceName)
                    .Distinct()];
            }

            public static string[] GetiOSDeviceNames(string osVersion, string browserName)
            {
                return [.. MobileDevices
                    .Where(m => m.OS.Equals("ios", OIC) &&
                                m.OSVersion.Equals(osVersion, OIC) &&
                                m.Browsers.Any(b => b.Contains(browserName, OIC)))
                    .Select(d => d.DeviceName)
                    .Distinct()];
            }

            public static string[] GetVersionsOfOS(string osName)
            {
                return osName switch
                {
                    "android" => AndroidVersions,
                    "ios" => iOSVersions,
                    "OS X" => MacOSVersions,
                    "Windows" => WindowsVersions,
                    _ => []
                };
            }

            public static string[] GetBrowserVersionsSupported(string browserName, string osName)
            {
                // Desktop Lookup
                if (osName == "OS X" || osName == "Windows")
                {
                    return [.. AvailableBrowsers
                        .Where(b => b.Name.Equals(browserName, OIC))
                        .Where(b => IsSupported(b.Support, osName))
                        .Select(b => b.Version)
                        .OrderDescending()];
                }

                // Mobile Lookup (Fallback to MobileDevices list)
                return [.. MobileDevices
                    .Where(m => m.OS.Equals(osName, OIC) &&
                                m.Browsers.Any(b => b.Contains(browserName, OIC)))
                    .Select(m => m.OSVersion)
                    .Distinct()
                    .OrderDescending()];
            }

            private static bool IsSupported(PlatformSupport support, string osName)
            {
                if (osName == "Windows")
                {
                    // Check against any Windows flag. 
                    return support.Windows10 || support.Windows11 || support.Windows8_1 || support.Windows8 || support.Windows7 || support.WindowsXP;
                }
                if (osName == "OS X")
                {
                    return support.OSX_Sequoia || support.OSX_Sonoma || support.OSX_Ventura || support.OSX_Monterey || 
                           support.OSX_BigSur || support.OSX_Catalina || support.OSX_Mojave || support.OSX_HighSierra || 
                           support.OSX_Sierra || support.OSX_ElCapitan || support.OSX_Yosemite || support.OSX_Mavericks ||
                           support.OSX_MountainLion || support.OSX_Lion || support.OSX_SnowLeopard;
                }
                return false;
            }

            public static string GetDesiredBrowerVersion(string browserName, string osName, string osVersion)
            {
                if (browserName == "Safari" && osName == "OS X")
                {
                    // Safari is tied to the OS version on macOS
                    return GetDesiredSafariVersion(osVersion);
                }
                else if (browserName == "Safari" && osName == "ios")
                {
                    return ""; // iOS Safari doesn't use a separate browser version
                }
                
                return "latest";
            }

            private static string GetDesiredSafariVersion(string osVersion)
            {
                // Clean up string like "15 (Sequoia)" to "15"
                var versionNum = GetVersionNumber(osVersion);
                
                // Approximate mapping based on latest available
                return versionNum switch
                {
                    "15" => "18.0", // Sequoia
                    "14" => "17.0", // Sonoma
                    "13" => "16.0", // Ventura
                    "12" => "15.0", // Monterey
                    "11" => "14.1", // Big Sur
                    "10.15" => "13.1", // Catalina
                    "10.14" => "12.1", // Mojave
                    "10.13" => "11.1", // High Sierra
                    "10.12" => "10.1", // Sierra
                    "10.11" => "9.1",  // El Capitan
                    "10.10" => "8.0",  // Yosemite
                    "10.9" => "7.1",   // Mavericks
                    "10.8" => "6.0",   // Mountain Lion
                    "10.7" => "6.0",   // Lion
                    "10.6" => "5.1",   // Snow Leopard
                    _ => "latest"
                };
            }
        }
    }
}