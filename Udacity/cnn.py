# -*- coding: utf-8 -*-
"""CNNonnxTest.ipynb

Automatically generated by Colaboratory.

Original file is located at
    https://colab.research.google.com/drive/1G481tdhbjEhkplprywTlsh3UPYqBSg-n

#init
"""

!pip install onnx onnxruntime

# Commented out IPython magic to ensure Python compatibility.
import torch
import numpy as np
import matplotlib.pyplot as plt
import torch.nn as nn
import cv2
import zipfile
import pandas as pd
import os

# %matplotlib inline

from torchvision import transforms
from torch.utils.data import random_split
from torch.autograd import Variable
from torch.optim import Adam, SGD
from sklearn.model_selection import train_test_split
import torch.onnx
import onnx
import onnxruntime

from google.colab import files
data = zipfile.ZipFile('records.zip', 'r')
data.extractall()
data.printdir()

"""#main code

###load data
"""

# data augmentation

def choose_random(center, left, right, steering_angle):
    """
    Randomly choose an image from the center, left or right, and adjust
    the steering angle.
    """
    choice = np.random.choice(3)
    if choice == 0:
        return cv2.imread(os.path.join("records/IMG", left)), steering_angle + 0.2
    elif choice == 1:
        return cv2.imread(os.path.join("records/IMG", right)), steering_angle - 0.2
    return cv2.imread(os.path.join("records/IMG", center)), steering_angle


def random_flip(image, steering_angle):
    """
    Randomly flipt the image left <-> right, and adjust the steering angle.
    """
    if np.random.rand() < 0.5:
        image = cv2.flip(image, 1)
        steering_angle = -steering_angle
    return image, steering_angle


def random_translate(image, steering_angle, range_x, range_y):
    """
    Randomly shift the image virtially and horizontally (translation).
    """
    trans_x = range_x * (np.random.rand() - 0.5)
    trans_y = range_y * (np.random.rand() - 0.5)
    steering_angle += trans_x * 0.002
    trans_m = np.float32([[1, 0, trans_x], [0, 1, trans_y]])
    height, width = image.shape[:2]
    image = cv2.warpAffine(image, trans_m, (width, height))
    return image, steering_angle


def random_shadow(image):
    """
    Generates and adds random shadow
    """
    # (x1, y1) and (x2, y2) forms a line
    # xm, ym gives all the locations of the image
    x1, y1 = 320 * np.random.rand(), 0
    x2, y2 = 320 * np.random.rand(), 160
    xm, ym = np.mgrid[0:160, 0:320]

    # mathematically speaking, we want to set 1 below the line and zero otherwise
    # Our coordinate is up side down.  So, the above the line: 
    # (ym-y1)/(xm-x1) > (y2-y1)/(x2-x1)
    # as x2 == x1 causes zero-division problem, we'll write it in the below form:
    # (ym-y1)*(x2-x1) - (y2-y1)*(xm-x1) > 0
    mask = np.zeros_like(image[:, :, 1])
    mask[(ym - y1) * (x2 - x1) - (y2 - y1) * (xm - x1) > 0] = 1

    # choose which side should have shadow and adjust saturation
    cond = mask == np.random.randint(2)
    s_ratio = np.random.uniform(low=0.2, high=0.5)

    # adjust Saturation in HLS(Hue, Light, Saturation)
    hls = cv2.cvtColor(image, cv2.COLOR_RGB2HLS)
    hls[:, :, 1][cond] = hls[:, :, 1][cond] * s_ratio
    return cv2.cvtColor(hls, cv2.COLOR_HLS2RGB)


def random_brightness(image):
    """
    Randomly adjust brightness of the image.
    """
    # HSV (Hue, Saturation, Value) is also called HSB ('B' for Brightness).
    hsv = cv2.cvtColor(image, cv2.COLOR_RGB2HSV)
    ratio = 1.0 + 0.4 * (np.random.rand() - 0.5)
    hsv[:,:,2] =  hsv[:,:,2] * ratio
    return cv2.cvtColor(hsv, cv2.COLOR_HSV2RGB)


def preprocess(image):
    image = image[40:-16, :, :] # remove the sky and the car front
    image = cv2.resize(image, (200, 66), cv2.INTER_AREA)
    #image = cv2.cvtColor(image, cv2.COLOR_RGB2YUV).transpose(-1, 0, 1)
    image = cv2.cvtColor(image, cv2.COLOR_BGR2YUV).transpose(-1, 0, 1)
    return image


def augment(image, steering_angle, range_x=100, range_y=10):
    """
    Generate an augumented image and adjust steering angle.
    (The steering angle is associated with the center image)
    """
    center, left, right = image
    if np.random.rand() < 0.6:
      image, steering_angle = choose_random(center, left, right, steering_angle)
      image, steering_angle = random_flip(image, steering_angle)
      image, steering_angle = random_translate(image, steering_angle, range_x, range_y)
      image = random_shadow(image)
      image = random_brightness(image)
    else:
      image = cv2.imread(os.path.join("records/IMG", center))
    image = preprocess(image)
    return image, np.array([steering_angle])

data = pd.read_csv("records/log.csv")
images = data[['center', 'left', 'right']].values
angles = data['angle'].values
del(data)

# keep one for arbitrary checking
img, angle = augment(images[300], angles[300])
checker = [img, angle]

"""###build model"""

class MyModel(nn.Module):
    def __init__(self):
        super(MyModel, self).__init__()
        self.layers = nn.Sequential(
            nn.Conv2d(3, 24, kernel_size=5, stride=2),
            nn.ELU(inplace=True),
            nn.Conv2d(24, 36, kernel_size=5, stride=2),
            nn.ELU(inplace=True),
            nn.Conv2d(36, 48, kernel_size=5, stride=2),
            nn.ELU(inplace=True),
            nn.Conv2d(48, 64, kernel_size=3, stride=1),
            nn.ELU(inplace=True),
            nn.Conv2d(64, 64, kernel_size=3, stride=1),
            nn.ELU(inplace=True),

            nn.Dropout(p=0.5),
            nn.Flatten(),

            nn.Linear(1152, 100),
            nn.ELU(inplace=True),
            nn.Linear(100, 50),
            nn.ELU(inplace=True),
            nn.Linear(50, 10),
            nn.ELU(inplace=True),
            nn.Linear(10, 1)
        )
        
    def forward(self, x):
        x = x/127.5 - 1.0
        x = self.layers(x)
        return x

model = MyModel()

# Defining the optimizer
optimizer = Adam(model.parameters(), lr=1.0e-4)
# Defining the loss function
criterion = torch.nn.MSELoss()
    
print(model)

def train(epoch):
    train_losses.clear()
    val_losses.clear()

    model.train()
    tr_loss = 0

    # Clearing the Gradients of the model parameters
    optimizer.zero_grad()
    
    train_x, train_y = [], []
    # Prediction for training and validation set
    for i in range(len(angles)):
      img, ang = augment(images[i], angles[i])

      train_x.append(img)
      train_y.append(ang)
      if i % 100 == 99:
        train_x, val_x, train_y, val_y = train_test_split(train_x, train_y, test_size = 0.2)
        output_train = model(torch.Tensor(train_x))
        output_val = model(torch.Tensor(val_x))

        # Computing the training and validation loss
        loss_train = criterion(output_train, torch.Tensor(train_y))
        loss_val = criterion(output_val, torch.Tensor(val_y))
        train_losses.append(loss_train)
        val_losses.append(loss_val)
        del(train_x)
        del(train_y)
        train_x, train_y = [], []

        # Computing the updated weights of all the model parameters
        loss_train.backward()
        optimizer.step()
        tr_loss = loss_train.item()
      
        print('Epoch : ', epoch+1, '\t', 'Batch : ', (i//100)+1, '\t', 'loss :', loss_val.item())

"""###train model"""

# Defining the number of epochs
n_epochs = 20
# Empty list to store training losses
train_losses = []
# Empty list to store validation losses
val_losses = []

# Training the model
for epoch in range(n_epochs):
    train(epoch)
    if epoch % 5 == 0:
      torch.save(model, 'cnn.pt')

# Plotting the training and validation loss
plt.plot(train_losses, label='Training loss')
plt.plot(val_losses, label='Validation loss')
plt.legend()
plt.show()

"""#save model as pt"""

torch.save(model, 'cnn.pt')
