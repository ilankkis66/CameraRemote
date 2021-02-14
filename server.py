import socket
import threading

IP = "192.168.1.28"
PORT = 8820
CHUNK = 1024
SEPARATOR = "###"
connected_devices = {}


def receive(client_socket):
    try:
        return client_socket.recv(CHUNK).decode()
    except socket.error:
        return ''


def send(client_socket, send_data):
    """
    :param client_socket:
    :type send_data: string
    """
    client_socket.send(send_data.encode())
    print("send to", get_key_by_address(client_socket), "<--------->", send_data)


def send_except_one(data, key):
    """send data to all players except the players in players[key]"""
    for k in get_keys_list(connected_devices):
        if k != key:
            send(connected_devices[k][0], data)


def get_keys_list(dic):
    """return list with all the keys of dic"""
    keys = []
    for key in dic.keys():
        keys.append(key)
    return keys


def get_key_by_address(client_address):
    """return key in players by client address = players[key][0]"""
    for key in get_keys_list(connected_devices):
        if connected_devices[key][0] == client_address:
            return key
    return -1


def main():
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind((IP, PORT))
    server_socket.listen(2)
    cs, a = server_socket.accept()
    data = receive(cs)
    connected_devices[data] = [cs, a]
    send(cs, "ENDD")
    print("main", a, data)
    # server_socket.settimeout(0.0001)
    while True:
        # try:
        #     cs, a = server_socket.accept()
        #     data = receive(cs)
        #     connected_devices[data] = [cs, a]
        #     print("while", a, data)
        #     for key in get_keys_list(connected_devices):
        #         if key != data:
        #             send(cs, key + "###")
        #             print("send to", data, key + "###")
        #     # send_except_one("ADDD"+SEPARATOR+data, data)
        #     send(cs, "ENDD")
        # except socket.error:
        #     pass
        try:
            for key in get_keys_list(connected_devices):
                t = threading.Thread(target=handle_client, args=(connected_devices[key][0],))
                t.start()
        except KeyError:
            pass


def handle_client(client_socket):
    # data = receive(client_socket).split(SEPARATOR)
    # if data[0] == "COND" and data[1] in get_keys_list(connected_devices):
    #     send(client_socket, "DADR" + SEPARATOR + str(connected_devices[data[1]][1]) + SEPARATOR + "server")
    #     send(connected_devices[data[1]][0], "DADR" + SEPARATOR +
    #          str(connected_devices[get_key_by_address(client_socket)][1]) + SEPARATOR + "client")
    # elif data[0] != "":
    #     print(data)
    pass


if __name__ == '__main__':
    main()
