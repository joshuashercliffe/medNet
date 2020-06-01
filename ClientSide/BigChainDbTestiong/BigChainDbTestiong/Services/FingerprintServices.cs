using System;
using BigChainDbTestiong.Models;
using System.IO;
using System.Diagnostics;

namespace BigChainDbTestiong.Services 
{
    public class FingerprintServices
    {
        // string py_path = @"C:\Users\Ashmit Ahuja\MEDNET\project-name-tdb\fingerprint\fp_enroll.py"; // ashmit
        // string py_path = @"C:\Users\jacob\Documents\School\ensc capstone\MedNet\fingerprint\fp_enroll.py"; // jw laptop
        string py_fingerprint = @"C:\Users\JacobChristopherWong\Documents\School Documents\MedNet\BigChainDbTestiong\BigChainDbTestiong\Python\"; // jw desktop

        public FingerprintServices()
        {   
            return;
        }
        public void AddUser(string userListId) 
        {
            // add a user to the database
            string username, keyword;
            Console.Write("user_id: ");
            username = Console.ReadLine();
            Console.Write("keyword: ");
            keyword = Console.ReadLine();

            Console.WriteLine("Your input:\nuser_id: " + username + "\nkeyword: " + keyword);

            // create user object
            var user_obj = new User 
            {
                userId = username,
                keyId = keyword,
            };

            // call fp_enroll.py to get fp_id and fp_hex
            string py_enroll = py_fingerprint + "fp_enroll.py";

            string fp_id = "";
            string fp_data = "";
            fpEnroll(py_enroll, ref fp_id, ref fp_data);

            user_obj.fpId = fp_id;
            user_obj.fpData = fp_data;

            Console.WriteLine("User fp_id: " + user_obj.fpId);
            Console.WriteLine("User fp_data: " + user_obj.fpData);

            // add user object to ActiveUsersAsset (in Metadata)


            return;
        }

        public void VerifyUser()
        {
            // verifies user with fingerprint
        }

        public void GetUsers()
        {
            // prints a list of the usernames with their object
        }

        public void fpEnroll(string cmd, ref string id, ref string data)
        {
            string output = "";
            python(cmd, ref output);

            // get the fp_id and fp_hex
            data = get_between(output, "HEX:", ":HEX");
            id = get_between(output, "ID:", ":ID");
        }


        public static void python(string cmd, ref string result)
        {
            // Initialize path to python.exe
            // string py_dir = @"C:\Users\Ashmit Ahuja\AppData\Local\Programs\Python\Python38\"; // ashmit
            string py_dir = @"C:/Users/JacobChristopherWong/AppData/Local/Programs/Python/Python38-32/"; // jw desktop

            // python.exe
            string py_exe = py_dir + "python.exe";

            // Check if python file exists
            bool py_exists = File.Exists(py_exe);
            if (!py_exists)
            {
                Console.WriteLine("ERROR: Specified .exe file does not exist");
                Console.Write("Your input: ");
                Console.WriteLine(py_exe);
                return;
            }

            ProcessStartInfo py = new ProcessStartInfo();
            py.FileName = py_exe;
            py.Arguments = string.Format("\"{0}\"", cmd);
            py.UseShellExecute = false;
            py.CreateNoWindow = false;
            py.RedirectStandardOutput = true;
            py.RedirectStandardError = true;

            //Console.WriteLine("--Python Script Output--");
            using (Process process = Process.Start(py))
            {
                using(StreamReader reader = process.StandardOutput)
                {
                    string stderr = process.StandardError.ReadToEnd();
                    result = reader.ReadToEnd();
                    //Console.WriteLine(result);
                }
            }
            return;
        }

        public static string get_between(string input, string start, string end)
        {
            int start_idx, end_idx;
            if (input.Contains(start) && input.Contains(end))
            {
                start_idx = input.IndexOf(start, 0) + start.Length;
                end_idx = input.IndexOf(end, start_idx);

                return input.Substring(start_idx, end_idx - start_idx);
            }
            return "";
        }

    }
}