import os

def img_type():
    return os.environ.get('TYPE', 'gen')

def room_type():
    return os.environ.get('ROOM', 'bathroom')

def prefix():
    return '%s_%s_resnet18' % (img_type(), room_type())

def npy_name():
    return prefix() + '.npy', (img_type(), room_type())
