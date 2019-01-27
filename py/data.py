import os

from torchvision import transforms
from torch.utils.data import Dataset, DataLoader
from skimage import io

# Ignore warnings
import warnings
warnings.filterwarnings("ignore")

GENERATOR_ROOT = os.path.expanduser("~/Downloads/interior/gen")
GIMG_ROOT = os.path.expanduser("~/Downloads/interior/gimg")


def fnames(root_dir, room_type):
    data_dir = os.path.join(root_dir, room_type)
    data_fnames = sorted([f for f in os.listdir(data_dir)
        if (f.endswith('.jpg') or f.endswith('.png') or f.endswith('.jpeg'))
    ])
    return [
        os.path.join(data_dir, f) for f in data_fnames
    ]


class InteriorDesignDataset(Dataset):
    def __init__(self, root_dir, room_type, transform=None):
        self.data_dir = os.path.join(root_dir, room_type)
        self.data_fnames = sorted([f for f in os.listdir(self.data_dir) if (f.endswith('.jpg') or f.endswith('.png') or f.endswith('.jpeg'))])
        assert len(self.data_fnames), '%s is EMPTY' % self.data_dir
        self.transform = transform

    def __len__(self):
        return len(self.data_fnames)

    def __getitem__(self, idx):
        img_name = os.path.join(self.data_dir, self.data_fnames[idx])
        image = io.imread(img_name)

        if self.transform: image = self.transform(image)
        return {'image': image}


def get_loader(root_dir, room_type):
    dataset = InteriorDesignDataset(root_dir, room_type, transform=transforms.Compose([
        transforms.ToPILImage(),
        transforms.Resize((224, 224)),
        transforms.ToTensor(),
        transforms.Normalize([0.485, 0.456, 0.406], [0.229, 0.224, 0.225]),
    ]))
    dataloader = DataLoader(dataset, batch_size=1, shuffle=False)
    return dataloader
