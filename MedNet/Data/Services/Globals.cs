namespace MedNet.Data.Services
{
    public static class Globals
    {
        public const string currentDSPriK = "currentDoctorSignPrivateKey";
        public const string currentDAPriK = "currentDoctorAgreePrivateKey";
        public const string currentPSPriK = "currentPatientSignPrivateKey";
        public const string currentPAPriK = "currentPatientAgreePrivateKey";
        public const string currentPSPubK = "currentPatientSignPrivateKey";
        public const string currentPAPubK = "currentPatientAgreePrivateKey";
        public const string currentPPHN = "currentPatientPHN";
        public const string currentUserName = "currentDoctorName";
        public const string currentUserID = "currentDoctorMinsc";
        public static readonly string[] nodes = { //"anode.lifeblocks.site", "bnode.lifeblocks.site",
                "cnode.lifeblocks.site", "dnode.lifeblocks.site", "enode.lifeblocks.site"};
    }
}
