using System;
using GeoAPI.CoordinateSystems.Transformations;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace ROSOnTrack_MQTT
{
    public struct GPSHeader
    {
        public double stamp;
    }

    public struct GPSObservation
    {
        public GPSHeader header;
        public double longitude;
        public double latitude;
        public double altitude;
    }

    public struct GPSOrigin
    {
        public String location;
        public GPSObservation origin;
        public GPSObservation x_calib;
    }

    public sealed class GPSUtils
    {
        public static readonly CoordinateTransformationFactory ctf = new CoordinateTransformationFactory();
        public static readonly CoordinateSystemFactory cf = new CoordinateSystemFactory();
        public static double origin_lon = 0.0, origin_lat = 0.0, origin_alt = 0.0;
        public static double x_calib_lon = 0.0, x_calib_lat = 0.0, x_calib_alt = 0.0;
        public static double origin_X = 0.0, origin_Y = 0.0, origin_Z = 0.0;
        public static double x_calib_X = 0.0, x_calib_Y = 0.0, x_calib_Z = 0.0;

        static CoordinateTransformationFactory _ctf = new CoordinateTransformationFactory();
        static ICoordinateTransformation WGS84ToUTM = _ctf.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, ProjectedCoordinateSystem.WGS84_UTM(34, true));
        static ICoordinateTransformation UTMToWGS84 = _ctf.CreateFromCoordinateSystems(ProjectedCoordinateSystem.WGS84_UTM(34, true), GeographicCoordinateSystem.WGS84);

        public GPSUtils(GPSOrigin gpsOrigin)
        {
            Program.location = gpsOrigin.location;
            origin_lon = gpsOrigin.origin.longitude;
            origin_lat = gpsOrigin.origin.latitude;
            origin_alt = gpsOrigin.origin.altitude;
            x_calib_lon = gpsOrigin.x_calib.longitude;
            x_calib_lat = gpsOrigin.x_calib.latitude;
            x_calib_alt = gpsOrigin.x_calib.altitude;
            double[] origin_calib_gps = { origin_lon, origin_lat, origin_alt };
            double[] x_calib_gps = { x_calib_lon, x_calib_lat, x_calib_alt };
            var origin_cartesian = WGS84ToUTM.MathTransform.Transform(origin_calib_gps);
            var x_calib_cartesian = WGS84ToUTM.MathTransform.Transform(x_calib_gps);
            origin_X = origin_cartesian[0];
            origin_Y = origin_cartesian[1];
            origin_Z = origin_cartesian[2];
            x_calib_X = x_calib_cartesian[0];
            x_calib_Y = x_calib_cartesian[1];
            x_calib_Z = x_calib_cartesian[2];
        }

        public GPSObservation XYZtoGPS(double x, double y, double z)
        {
            double theta = Math.Atan2(x_calib_Y- origin_Y, x_calib_X - origin_X);
            double originalPos_X = x * Math.Cos(theta) - y * Math.Sin(theta) + origin_X;
            double originalPos_Y = x * Math.Sin(theta) + y * Math.Cos(theta) + origin_Y;
            double originalPos_Z = z + origin_Z;
            double[] originalPos = { originalPos_X, originalPos_Y, originalPos_Z };
            double[] resultPos = UTMToWGS84.MathTransform.Transform(originalPos);

            return new GPSObservation
            {
                header = new GPSHeader
                {
                    stamp = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
                },
                longitude = resultPos[0],
                latitude = resultPos[1],
                altitude = resultPos[2],
            };
        }
    }
}
