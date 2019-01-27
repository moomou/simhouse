import os
import pprint
import random
from collections import Counter

import numpy as np
from annoy import AnnoyIndex

import name_util
import data

F_SIZE = 512


def build_combined_index(*prefixes):
    t = AnnoyIndex(F_SIZE, metric='euclidean')  # Length of item vector that will be indexed

    offset = 0
    for prefix in prefixes:
        arr = np.load('%s.npy' % prefix)
        arr = arr.reshape(arr.shape[0], -1)
        for i in range(arr.shape[0]):
            t.add_item(i + offset, arr[i])
        offset += arr.shape[0]

    t.build(100)
    t.save('combined.ann')


def build_index(prefix):
    arr = np.load('%s.npy' % prefix)
    arr = arr.reshape(arr.shape[0], -1)

    f = arr.shape[-1]
    t = AnnoyIndex(f, metric='euclidean')  # Length of item vector that will be indexed
    for i in range(arr.shape[0]):
        t.add_item(i, arr[i])

    t.build(100)
    t.save('%s.ann' % prefix)


def judge(room_type='bathroom', use_combined=False, sample_size=20):
    '''
    open a generated index, randomly pick 10 gimg embedding,
    search for top 10 neighbors, get intersection, and sort
    by repeat count and pick top 5
    '''
    print('Judging', room_type)
    arr = np.load('gimg_%s_resnet18.npy' % room_type)
    arr = arr.reshape(arr.shape[0], -1)
    n, f = arr.shape

    u = AnnoyIndex(f, metric='euclidean')
    if use_combined:
        print('searching in combined')
        u.load('combined.ann')
    else:
        print('searching in %s' % name_util.prefix())
        u.load('%s.ann' % name_util.prefix()) # super fast, will just mmap the file

    samples = np.random.randint(0, n, size=sample_size)
    all_nns = []
    for idx in samples:
        nns, dist = u.get_nns_by_vector(arr[idx], 10, search_k=-1, include_distances=True)
        # print(nns, dist)
        all_nns.extend(nns)

    nn_sorted = Counter(all_nns).most_common()
    # print(nn_sorted)

    if use_combined:
        f0 = data.fnames(data.GENERATOR_ROOT, 'bathroom')
        f1 = data.fnames(data.GENERATOR_ROOT, 'bedroom')
        fnames = f0 + f1
    else:
        fnames = data.fnames(data.GENERATOR_ROOT, name_util.room_type())

    print('BEST')
    pprint.pprint([
        fnames[i] for i, _ in nn_sorted[:10]
    ])


def build_all():
    print('building...')
    prefixes = ['gen_bathroom_resnet18', 'gen_bedroom_resnet18']
    build_index(prefixes[0])
    build_index(prefixes[1])
    build_combined_index(*prefixes)


if __name__ == '__main__':
    import fire

    fire.Fire({
        'build': build_all,
        'judge': judge,
    })
