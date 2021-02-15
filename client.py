import socket

IP = "192.168.1.28"
PORT = 8888
CHUNK = 1024
SEPARATOR = "###"


def main():
    t = ""
    my_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    my_socket.connect((IP, PORT))
    my_socket.send("python client1".encode())
    while not t.endswith("ENDD"):
        t += my_socket.recv(CHUNK).decode()
        print(t)
    t = t.split(SEPARATOR)
    print(t)
    t = my_socket.recv(CHUNK).decode()
    print(t)
    a = t.split(SEPARATOR)
    dIP = a[1][2:]
    dIP = dIP[:dIP.find("'")]
    print(dIP)
    print(a)
    socket1 = socket.socket()
    if a[2] == "client":
        socket1.connect((dIP, 6666))
        print(socket1.recv(CHUNK).decode())
    elif a[2] == "server":
        socket1.bind((IP, 6666))
        socket1.listen(2)
        cs, a = socket1.accept()
        cs.send(str("ilan from the server").encode())


if __name__ == '__main__':
    main()

