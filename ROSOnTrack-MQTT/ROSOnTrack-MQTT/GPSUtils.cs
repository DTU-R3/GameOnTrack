using System;
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
        public GPSObservation origin;
        public GPSObservation x1m;
    }

    public sealed class GPSUtils
    {
        public static readonly CoordinateTransformationFactory ctf = new CoordinateTransformationFactory();
        public static readonly CoordinateSystemFactory cf = new CoordinateSystemFactory();

        static readonly ICoordinateSystem epsg4326 = cf.CreateFromWkt("GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]]");

        static readonly ICoordinateSystem epsg3785 = cf.CreateFromWkt("PROJCS[\"Popular Visualisation CRS / Mercator\", GEOGCS[\"Popular Visualisation CRS\", DATUM[\"Popular Visualisation Datum\", SPHEROID[\"Popular Visualisation Sphere\", 6378137, 0, AUTHORITY[\"EPSG\", \"7059\"]], TOWGS84[0, 0, 0, 0, 0, 0, 0], AUTHORITY[\"EPSG\", \"6055\"]], PRIMEM[\"Greenwich\", 0, AUTHORITY[\"EPSG\", \"8901\"]], UNIT[\"degree\", 0.0174532925199433, AUTHORITY[\"EPSG\", \"9102\"]], AXIS[\"E\", EAST], AXIS[\"N\", NORTH], AUTHORITY[\"EPSG\", \"4055\"]], PROJECTION[\"Mercator\"], PARAMETER[\"False_Easting\", 0], PARAMETER[\"False_Northing\", 0], PARAMETER[\"Central_Meridian\", 0], PARAMETER[\"Latitude_of_origin\", 0], UNIT[\"metre\", 1, AUTHORITY[\"EPSG\", \"9001\"]], AXIS[\"East\", EAST], AXIS[\"North\", NORTH], AUTHORITY[\"EPSG\", \"3785\"]]");

        public GPSUtils(GPSOrigin gpsOrigin)
        {
            
        }

        public GPSObservation XYZtoGPS(double x, double y, double z)
        {
            ICoordinateTransformation ct = ctf.CreateFromCoordinateSystems(epsg3785, epsg4326);
            double[] originalPos = { x, y, z };
            double[] resultPos = ct.MathTransform.Transform(originalPos);
            resultPos[0] = resultPos[0] + 52.5112561156;

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
