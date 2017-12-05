﻿using System;
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
        public double latitude;
        public double longitude;
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

        static readonly ICoordinateSystem epsg4326 = cf.CreateFromWkt("GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]]");

        static readonly ICoordinateSystem epsg3785 = cf.CreateFromWkt("PROJCS[\"Popular Visualisation CRS / Mercator\", GEOGCS[\"Popular Visualisation CRS\", DATUM[\"Popular Visualisation Datum\", SPHEROID[\"Popular Visualisation Sphere\", 6378137, 0, AUTHORITY[\"EPSG\", \"7059\"]], TOWGS84[0, 0, 0, 0, 0, 0, 0], AUTHORITY[\"EPSG\", \"6055\"]], PRIMEM[\"Greenwich\", 0, AUTHORITY[\"EPSG\", \"8901\"]], UNIT[\"degree\", 0.0174532925199433, AUTHORITY[\"EPSG\", \"9102\"]], AXIS[\"E\", EAST], AXIS[\"N\", NORTH], AUTHORITY[\"EPSG\", \"4055\"]], PROJECTION[\"Mercator\"], PARAMETER[\"False_Easting\", 0], PARAMETER[\"False_Northing\", 0], PARAMETER[\"Central_Meridian\", 0], PARAMETER[\"Latitude_of_origin\", 0], UNIT[\"metre\", 1, AUTHORITY[\"EPSG\", \"9001\"]], AXIS[\"East\", EAST], AXIS[\"North\", NORTH], AUTHORITY[\"EPSG\", \"3785\"]]");

        static readonly ICoordinateTransformation ct = ctf.CreateFromCoordinateSystems(epsg3785, epsg4326);
        static readonly ICoordinateTransformation ct_reverse = ctf.CreateFromCoordinateSystems(epsg4326, epsg3785);

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
            var origin_cartesian = ct_reverse.MathTransform.Transform(origin_calib_gps);
            var x_calib_cartesian = ct_reverse.MathTransform.Transform(x_calib_gps);
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
            double[] resultPos = ct.MathTransform.Transform(originalPos);

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