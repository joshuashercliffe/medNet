# default imports
import fp_tools as fp
import pdb

# custom imports
def main():
    # initialize fingerprint scanner
    [fps, com_dev] = fp.init()
    if not fps:
        exit(1)

    while True:
        not_valid = True
        while(not_valid):
            print("Select an option:\n",
                    "1. Enroll Fingerprint\n",
                    "2. Scan Fingerprint\n",
                    "3. Download Fingerprint Image\n",
                    "4. Delete Fingerprint\n",
                    "\n",
                    "0. Exit")
            option = int(input("Your input: "))
            if option in list(range(5))+[666]:
                not_valid = False
            else:
                print("Invalid input! Please try again.\n\n")
        
        if option == 1:
            print("Enrolling Fingerprint")
            fp.enroll(fps)
            print("\n")
        elif option == 2:
            print("Search for Fingerprint")
            fp.search(fps)
            print("\n")
        elif option == 3:
            print("Download Fingerprint image")
            fp.downloadimage(fps)
            print("\n")
        elif option == 4:
            print("Delete a Fingerprint")
            fp.delete(fps)
            print("\n")
        elif option == 666:
            delall = input("Are you sure you want to clear the whole database? (y/n): ")
            if delall == "y":
                fp.deleteall(fps)
        elif option == 0:
            print("Quitting application")
            fp.quit(com_dev)
            exit(1)
            print("\n")

if __name__ == '__main__':
    main()