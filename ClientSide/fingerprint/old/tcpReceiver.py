import socket
import threading

#HOST = '192.168.0.17' #jacob's computer
HOST = '0.0.0.0'
PORT = 15501

s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
s.settimeout(None)
s.bind((HOST, PORT))
s.listen()
conn, addr = s.accept()

try:
    print('Connected by', addr)
    while True:
        data = conn.recv(1024)
        # if data != b'':
        #     print(repr(data))
        # conn.sendall(data)
except:
    print("Port timed out")
    s.close()

# if port closes, need to restart it
# threaded 