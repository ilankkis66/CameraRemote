import socket
import threading
import SqlORM

IP = "0.0.0.0"
PORT = 8820
CHUNK = 1024
SEPARATOR = "###"
connected_devices = {}
users_db = SqlORM.Users()


def receive(client_socket):
    try:
        return client_socket.recv(CHUNK).decode()
    except socket.error:
        return ''


def receive_data(client_socket):
    try:
        return client_socket.recv(110592)
    except socket.error:
        return ''


def send(client_socket, send_data):
    """
    :param client_socket:
    :type send_data: string
    """
    client_socket.send(send_data.encode())
    # print("send to", get_key_by_address(client_socket), "<--------->", send_data)


def accept(server_socket):
    global connected_devices,users_db
    try:
        cs, a = server_socket.accept()
        data = receive(cs)
        users_db.insert(data)
        connected_devices[data] = [cs, a]
        print(data, a)
        for key in get_keys_list(connected_devices):
            if key != data:
                send(cs, key + "###")
        send(cs, "ENDD")
    except socket.error:
        pass


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
    server_socket = socket.socket()
    server_socket.bind((IP, PORT))
    server_socket.listen(2)
    server_socket.settimeout(0.0001)
    while True:
        accept(server_socket)
        try:
            for key in get_keys_list(connected_devices):
                t = threading.Thread(target=handle_client, args=(connected_devices[key][0],))
                t.start()
        except KeyError:
            pass


def handle_client(client_socket):
    global users_db
    data = receive_data(client_socket)
    if data:
        if data[:4].decode() == "COND":
            data = data.decode().split(SEPARATOR)
            send(client_socket, "DADR" + SEPARATOR + str(connected_devices[data[1]][1]) + SEPARATOR + "server")
            send(connected_devices[data[1]][0], "DADR" + SEPARATOR +
                 str(connected_devices[get_key_by_address(client_socket)][1]) + SEPARATOR + "client")
        elif data[:4].decode() == "SPIC":
            num = users_db.get_photos_number(get_key_by_address(client_socket))[0]
            with open("d:\\ilan\\" + get_key_by_address(client_socket) + " number " + str(num) + ".png", "wb") as f:
                f.write(data[7:])
                users_db.add_photo(get_key_by_address(client_socket), f.name)
        elif data[0] != "":
            print("else:" + data)

    """ 
def handle_client(client_socket):
    try:
        data = client_socket.recv(2056)
        if data[0] == "COND" and data[1] in get_keys_list(connected_devices):
            send(client_socket, "DADR" + SEPARATOR + str(connected_devices[data[1]][1]) + SEPARATOR + "server")
            send(connected_devices[data[1]][0], "DADR" + SEPARATOR +
                 str(connected_devices[get_key_by_address(client_socket)][1]) + SEPARATOR + "client")
        else:
            print(data)
            with open("d:\\ilan\\i.png","wb") as f:
                f.write(data)
    except:
        pass
     """


if __name__ == '__main__':
    main()
