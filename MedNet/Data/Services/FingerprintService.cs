using PatternRecognition.FingerprintRecognition.FeatureExtractors;
using PatternRecognition.FingerprintRecognition.Matchers;
using System.Drawing;
using System.IO;

namespace MedNet.Data.Services
{
    public static class FingerprintService
    {
        public static bool CompareFingerprints(byte[] inputFingerprint, byte[] databaseFingerprint )
        {
            // Convert fingerprint byte arrays into Bitmap image objects
            var incomingImage1 = Image.FromStream(new MemoryStream(inputFingerprint));
            var bitmap1 = new Bitmap(incomingImage1);

            var incomingImage2 = Image.FromStream(new MemoryStream(databaseFingerprint));
            var bitmap2 = new Bitmap(incomingImage2);

            var fingerprintImg1 = bitmap1;
            var fingerprintImg2 = bitmap2;

            // Building feature extractor and extracting features
            var featExtractor = new MTripletsExtractor() { MtiaExtractor = new Ratha1995MinutiaeExtractor() };
            var features1 = featExtractor.ExtractFeatures(fingerprintImg1);
            var features2 = featExtractor.ExtractFeatures(fingerprintImg2);

            // Building matcher and matching
            var matcher = new M3gl();
            var similarity = matcher.Match(features1, features2);

            // Check if similarity is greater than 0.40
            if(similarity >= 0.40)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
