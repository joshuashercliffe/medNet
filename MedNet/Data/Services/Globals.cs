namespace MedNet.Data.Services
{
    public static class Globals
    {
        public const string currentDSPriK = "currentDoctorSignPrivateKey";
        public const string currentDAPriK = "currentDoctorAgreePrivateKey";
        public const string currentPSPriK = "currentPatientSignPrivateKey";
        public const string currentPAPriK = "currentPatientAgreePrivateKey";
        public const string currentPSPubK = "currentPatientSignPublickey";
        public const string currentPAPubK = "currentPatientAgreePublicKey";
        public const string currentPPHN = "currentPatientPHN";
        public const string currentUserName = "currentUserName";
        public const string currentUserID = "currentUserID";
        public static readonly string[] nodes = { //"anode.lifeblocks.site", "bnode.lifeblocks.site",
                "cnode.lifeblocks.site", "dnode.lifeblocks.site", "enode.lifeblocks.site"};
        public static readonly string[] relationships = { "","Parent", "Patner", "Relative", "Friend", "Other"};
        public static readonly string[] provincesShort = { "", "AB", "BC", "MB", "NB", "NL", "NT", "NS", "NU", "ON", "PE", "QC", "SK", "YT" };
    }
}
