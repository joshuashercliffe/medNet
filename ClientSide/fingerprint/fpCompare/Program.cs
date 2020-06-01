using PatternRecognition.FingerprintRecognition.FeatureExtractors;
using PatternRecognition.FingerprintRecognition.Matchers;
using System.Drawing;
using System.IO;
using System;

namespace fpCompare
{
    class Program
    {
        static void Main(string[] args)
        {
            string dir = @"C:\\Users\\JacobChristopherWong\\Desktop\\fpCompare\\idealFpImg";
            // Load fpImg1 
            string fpImg = dir + "\\..\\fp00.bmp";
            var bmp1 = new Bitmap(fpImg);
            
            // Get Test Fingerprints
            string[] files = Directory.GetFiles(dir);
            foreach(string filename in files)
            {
                var bmp2 = new Bitmap(filename);
                var conf = CompareFingerprints(bmp1, bmp2);
                string msg = filename + ": " + conf.ToString();
                Console.WriteLine(msg);
            }
        }
        public static double CompareFingerprints(Bitmap bitmap1, Bitmap bitmap2)
        //public static double CompareFingerprints(byte[] inputFingerprint, byte[] databaseFingerprint)
        {
            //var incomingImage1 = Image.FromStream(new MemoryStream(inputFingerprint));
            //var bitmap1 = new Bitmap(incomingImage1);

            //var incomingImage2 = Image.FromStream(new MemoryStream(databaseFingerprint));
            //var bitmap2 = new Bitmap(incomingImage2);

            var fingerprintImg1 = bitmap1;
            var fingerprintImg2 = bitmap2;

            // Building feature extractor and extracting features
            var featExtractor = new MTripletsExtractor() { MtiaExtractor = new Ratha1995MinutiaeExtractor() };
            var features1 = featExtractor.ExtractFeatures(fingerprintImg1);
            var features2 = featExtractor.ExtractFeatures(fingerprintImg2);

            // Building matcher and matching
            var matcher = new M3gl();
            var similarity = matcher.Match(features1, features2);

            return similarity;
        }
    }
}
