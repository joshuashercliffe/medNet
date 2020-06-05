import serial
import hashlib
import time
import tempfile
import serial.tools.list_ports as lsp
from pyfingerprint import PyFingerprint
from PIL import Image
import pdb
import numpy as np
import socket 
import io 
import _thread as thread
from requests import get 
import sys
import base64
import binascii

DELIM = b'MEDNETFP' # Delimiter for TCP message to backend server
MEDNET_IP = b'3.23.5.132' # Backend public IP address of server
MEDNET_KEY = b'MEDNETFP:START' # Keyword to wait from backend server 
BAUDRATE = 57600
SIZE = (227, 257)
CROP = (15, 15, 15, 15)
TCP_IP = '0.0.0.0'
PORT = 15326 # actual port we're using
# PORT = 15327 # debug

def fpScan():
    # initialize fingerprint sensor
    [f, found] = initFP()

    if not found:
        print("No fingerprint scanner detected.")
        return

    # enroll fingerprint functionality
    img = getImage(f)

    return img

def getImage(f):
    ## Reads image and download it
    ## Gets some sensor information
    # print('Currently used templates: ' + str(f.getTemplateCount()) +'/'+ str(f.getStorageCapacity()))

    ## Tries to read image and download it
    try:
        print('Waiting for finger...')

        ## Wait that finger is read
        while ( f.readImage() == False ):
            pass

        print('Downloading bytes (this take a while)...')
        # imageDestination =  tempfile.gettempdir() + '/fingerprint.bmp'
        img = f.getImage()

    except Exception as e:
        print('Operation failed!')
        print('Exception message: ' + str(e))
        # exit(1)
        return
    
    return img

def initFP():
    # list avaiable COM devices
    coms = None
    coms = list(lsp.comports())
    if coms == None:
        print("No COM ports available!")
        return
    f = None
    found = False
    # cycle through the different COM devices
    for com in coms:
        try: 
            f = PyFingerprint(com.device, BAUDRATE)
            if(f.verifyPassword() == False):
                raise ValueError('The fingerprint sensor password is wrong.')
            else:
                print(com.device, ': Connected to fingerprint sensor')
                found = True
                break   
        except Exception:
            print(com.device, ': Not connected to fingerprint sensor')

    return [f, found]

def main():
    # Always listen for incoming connections
    while True:
        # Setup TCP Server listening for connections on specific port
        sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM) # Setup TCP Socket
        sock.settimeout(None)
        sock.bind((TCP_IP, PORT)) # Setup socket on LOCAL_IP and on specific PORT (15326)
        sock.listen() # Listen for incoming connection
        print("TCP socket on ({}, {})".format(TCP_IP, PORT))
        conn, addr = sock.accept() # Accept the incoming connection

        # When a connection is established, wait for KEY_STRING
        # Start FP Process
        try:
            print('Connected by', addr)
            data = conn.recv(1024)

            if data != b'':
                # parse the input 
                inData = data.splitlines()[0].split(b'|')
                clientIP = inData[0]
                fpKey = inData[1]
                numScans = int(inData[2])

                # get public IP
                ip = get('https://api.ipify.org').text
                bip = bytes(ip, 'utf-8') 
                
                baddr = bytes(addr[0], 'utf-8')

                # Check if the Client_IP, MEDNET_KEY, and MEDNET_IP are expected
                inLst = [clientIP, fpKey, baddr]

                # if inLst == [bip, MEDNET_KEY, MEDNET_IP]: # Actual: check for IP and message
                if inLst[0:2] == [bip, MEDNET_KEY]: # DEBUG: only look at the message
                    print("Authentication granted, starting FP process")
                    for i in range(numScans):
                        print("Scan {0}/{1}".format(i+1, numScans))
                        # get fingerprint image
                        fpImg = fpScan() 
                        newImg = fpImg.resize(SIZE)
                        # newImg = fpImg.crop(CROP)
                
                        # convert to bytearray 
                        bdata = io.BytesIO() 
                        newImg.save(bdata, 'bmp') 
                        bfp = bdata.getvalue()        
                        
                        # print(sys.getsizeof(bfp))
                        
                        bmsg = DELIM + bfp
                        # Create formatted message
                        if i == 0:
                            # Send IP for first message
                            bmsg = bip + bmsg
                        
                        # Convert message to base64 string, ASCII
                        b64Msg = base64.b64encode(bmsg)

                        # Send encoded message back to the AWS Server
                        conn.send(b64Msg)
                else: 
                    conn.send(b'AuthenticaDtion FAILED\n') # DEBUG
        except:
            print("Connection Lost. Closing socket.")
            sock.close()
        # Close the current connection
        conn.close()

# actual main
if __name__ == '__main__':
    main()    
