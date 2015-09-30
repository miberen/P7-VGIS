clear all; close all;

% Read an image
A = imread('fuji.png');
% Show the image
figure;
imshow(A);

% Choose the kernel

%No kernel
% k = [1];
 
% Average
% k = [1/4,1/4; 1/4,1/4];

% Gaussian kernel nxm with sigma s
 k = fspecial('gaussian',[3 3], 2);

% Choose the number of levels of the pyramid
l = 3;

% Analysis Pyramid
B = pyramidA(A,l,k);

C = pyramidS(B, l, 'bilinear');








