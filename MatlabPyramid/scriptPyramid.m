clear all; 
close all; 

squaredImage = squareIm('squared.jpg'); 
% function [imInputSq] = squareIm(namefile)
% function squareIm crops an input image to the closest-lower power of 2 

pyramid(squaredImage, 4); 
% function [] = pyramid(inputImage, levels)
% function pyramid computes and shows the L downsampled levels of an square
% image

