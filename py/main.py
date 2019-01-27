import os

import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import torch
import torch.nn as nn
import torchvision
import numpy as np
from tqdm import tqdm

import name_util
from data import GENERATOR_ROOT, GIMG_ROOT, get_loader


class RestnestFex(nn.Module):
    def __init__(self, original_model):
        super(RestnestFex, self).__init__()
        self.features = nn.Sequential(*list(original_model.children())[:-1])

    def forward(self, x):
        x = self.features(x)
        return x


def imshowgrid(batch):
    images_batch = sample_batched['image']
    batch_size = len(images_batch)
    im_size = images_batch.size(2)

    grid = torchvision.utils.make_grid(images_batch)
    plt.imshow(grid.numpy().transpose((1, 2, 0)))
    plt.title('Batch from dataloader')
    plt.savefig('test.png')


if __name__ == '__main__':
    model = torchvision.models.resnet18(pretrained=True)
    model = RestnestFex(model)

    model.eval()
    for param in model.parameters():
        param.requires_grad = False

    fname, (img_type, room_type) = name_util.npy_name()
    print('generating %s' % fname)

    dataloader = get_loader(GENERATOR_ROOT if img_type == 'gen' else GIMG_ROOT, room_type)
    all_result = None
    for sample_batched in tqdm(dataloader):
        outputs = model(sample_batched['image'])

        if all_result is None:
            all_result = outputs
        else:
            all_result = torch.cat((all_result, outputs), 0)

    np.save(fname, all_result.numpy())
