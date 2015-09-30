clear all; 
close all; 

squaredImage = squareIm('kentucky.png'); 
% function [imInputSq] = squareIm(namefile)
% function squareIm crops an input image to the closest-lower power of 2 

outputSquared = pyramidAnalysis(squaredImage, 1); 
% function [outputImage] = pyramid(inputImage, levels)
% pyramidAnalysis performs a L-level downsampling of an squared
% image; it returns the lowest level of the input image (in that way, the
% reconstruction of the analysis can be done)

pyramidSynthesis_naive(outputSquared, 2);
% function [] = pyramidSynthesis_naive(inputImage, levels)
% pyramidSynthesis_naive performs a L-level upsampling of an squared image;
% it returns the highest level of the input image. 
% The procedure is not the one presented in the paper: every pixel of a
% lower level is duplicated (4 times) to the upper level, without applying
% any kind of weighted mean. 

% That is: 
% Upper level: 
% U(1,1) U(1,2)
% U(2,1) U(2,2) 

% Lower level (double size):
% (1,1) (1,2) (1,3) (1,4)     U(1,1) U(1,1) U(1,2) U(1,2)
% (2,1) (2,2) (2,3) (2,4)  =  U(1,1) U(1,1) U(1,2) U(1,2)
% (3,1) (3,2) (3,3) (3,4)  =  U(2,1) U(2,1) U(2,2) U(2,2) 
% (4,1) (4,2) (4,3) (4,4)     U(2,1) U(2,1) U(2,2) U(2,2) 

pyramidSynthesis(outputSquared, 2);
% function [] = pyramidSynthesis(inputImage, levels)
% pyramidSynthesis performs a L-level upsampling of an squared image;
% it returns the highest level of the input image. 
% The procedure is the one presented in the paper (weighted sum of the
% pixels in the neighborhood). 
