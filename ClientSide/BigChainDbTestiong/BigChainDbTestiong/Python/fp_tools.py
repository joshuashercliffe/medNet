# Code written by Bastian Raschke
# git repo: https://github.com/bastianraschke/pyfingerprint
#!/usr/bin/env python
# -*- coding: utf-8 -*-

"""
PyFingerprint
Copyright (C) 2015 Bastian Raschke <bastian.raschke@posteo.de>
All rights reserved.
"""
import serial
import hashlib
import time
import tempfile
import serial.tools.list_ports as lsp
from pyfingerprint.pyfingerprint import PyFingerprint

# CONSTANTS
BAUDRATE = 57600

# helper functions
def init():
    # list avaiable COM devices
    coms = None
    coms = list(lsp.comports())
    if coms == None:
        print("No COM ports available!")
        return
    f = None
    # cycle through the different COM devices
    for com in coms:
        try: 
            f = PyFingerprint(com.device, BAUDRATE)
            if(f == None):
                print("No fingerprint sensor detected!")
                return
            if(f.verifyPassword() == False):
                raise ValueError('The fingerprint sensor password is wrong.')
            else:
                print(com.device, ': Connected to fingerprint sensor')
                break   
        except Exception:
            print(com.device, ': Not connected to fingerprint sensor')

    # exit if fingerprint could not be found
    if not f:
        print('Fingerprint sensor could not be found on any COM port.\n\
                Please double check fingerprint sensor connection.')
    return [f, com.device]

def search(f):
        ## Gets some sensor information
    print('Currently used templates: ' + str(f.getTemplateCount()) +'/'+ str(f.getStorageCapacity()))

    ## Tries to search the finger and calculate hash
    try:
        print('Waiting for finger...')

        ## Wait that finger is read
        while ( f.readImage() == False ):
            pass

        ## Converts read image to characteristics and stores it in charbuffer 1
        f.convertImage(0x01)

        ## Searchs template
        result = f.searchTemplate()

        positionNumber = result[0]
        accuracyScore = result[1]

        if ( positionNumber == -1 ):
            print('No match found!')
            exit(0)
        else:
            print('Found template at position #' + str(positionNumber))
            print('The accuracy score is: ' + str(accuracyScore))

        ## OPTIONAL stuff
        ##

        ## Loads the found template to charbuffer 1
        f.loadTemplate(positionNumber, 0x01)

        ## Downloads the characteristics of template loaded in charbuffer 1
        characterics = str(f.downloadCharacteristics(0x01)).encode('utf-8')

        ## Hashes characteristics of template
        print('SHA-2 hash of template: ' + hashlib.sha256(characterics).hexdigest())

    except Exception as e:
        print('Operation failed!')
        print('Exception message: ' + str(e))
        # exit(1)
        return

def delete(f):
    ## Gets some sensor information
    print('Currently used templates: ' + str(f.getTemplateCount()) +'/'+ str(f.getStorageCapacity()))

    ## Tries to delete the template of the finger
    try:
        positionNumber = input('Please enter the template position you want to delete: ')
        positionNumber = int(positionNumber)

        if (f.deleteTemplate(positionNumber) == True ):
            print('Template deleted!')

    except Exception as e:
        print('Operation failed!')
        print('Exception message: ' + str(e))
        # exit(1)
        return



def downloadimage(f):
    ## Reads image and download it
    ## Gets some sensor information
    print('Currently used templates: ' + str(f.getTemplateCount()) +'/'+ str(f.getStorageCapacity()))

    ## Tries to read image and download it
    try:
        print('Waiting for finger...')

        ## Wait that finger is read
        while ( f.readImage() == False ):
            pass

        print('Downloading image (this take a while)...')

        imageDestination =  tempfile.gettempdir() + '/fingerprint.bmp'
        f.downloadImage(imageDestination)

        print('The image was saved to "' + imageDestination + '".')

    except Exception as e:
        print('Operation failed!')
        print('Exception message: ' + str(e))
        # exit(1)
        return

def enroll(f):
    ## Gets some sensor information
    print('Currently used templates: ' + str(f.getTemplateCount()) +'/'+ str(f.getStorageCapacity()))

    ## Tries to enroll new finger
    try:
        print('Waiting for finger...')

        ## Wait that finger is read
        while ( f.readImage() == False ):
            pass

        ## Converts read image to characteristics and stores it in charbuffer 1
        f.convertImage(0x01)

        ## Checks if finger is already enrolled
        result = f.searchTemplate()
        positionNumber = result[0]

        if ( positionNumber >= 0 ):
            print('Template already exists at position #' + str(positionNumber))
            # exit(0)
            return

        print('Remove finger...')
        time.sleep(2)

        print('Waiting for same finger again...')

        ## Wait that finger is read again
        while ( f.readImage() == False ):
            pass

        ## Converts read image to characteristics and stores it in charbuffer 2
        f.convertImage(0x02)

        ## Compares the charbuffers
        if ( f.compareCharacteristics() == 0 ):
            raise Exception('Fingers do not match')

        ## Creates a template
        f.createTemplate()

        ## Saves template at new position number
        positionNumber = f.storeTemplate()
        print('Finger enrolled successfully!')
        print('New template position #' + str(positionNumber))

    except Exception as e:
        print('Operation failed!')
        print('Exception message: ' + str(e))
        # exit(1)
        return
    
    return f

def genrandom(f):
    ## Tries to generate a 32-bit random number
    try:
        print(f.generateRandomNumber())

    except Exception as e:
        print('Operation failed!')
        print('Exception message: ' + str(e))
        # exit(1)
        return

def index(f):
    ## Gets some sensor information
    print('Currently used templates: ' + str(f.getTemplateCount()) +'/'+ str(f.getStorageCapacity()))

    ## Tries to show a template index table page
    try:
        page = input('Please enter the index page (0, 1, 2, 3) you want to see: ')
        page = int(page)

        tableIndex = f.getTemplateIndex(page)

        for i in range(0, len(tableIndex)):
            print('Template at position #' + str(i) + ' is used: ' + str(tableIndex[i]))

    except Exception as e:
        print('Operation failed!')
        print('Exception message: ' + str(e))
        # exit(1)
        return

def quit(com_dev):
    # disconnect from fingerprint sensor
    # list avaiable COM devices
    com = serial.Serial()
    try:
        com = serial.Serial(com_dev, BAUDRATE, timeout=2)
    except:
        com.close()
        print("Closed", com_dev)

def deleteall(f):
    f.clearDatabase()