function [imInputSq] = squareIm(namefile)

imInputSq = imread(namefile); % store an image

figure; 
imshow(namefile); % show the image

[w,h,~] = size(imInputSq); % get the width and the height of the image

% the image will be cropped
if w<h % if the width is bigger than the height 
    w1 = nextpow2(w); % we compute the closest power of 2 of the width
    if 2.^w1 > w % if it is bigger than the real width (rounding) we will take the previous power of 2
        imInputSq = imInputSq(1:(2.^(w1-1)), 1:(2.^(w1-1)), :); % get the squared power of 2 image
    else % the width is smaller than the real one
        imInputSq = imInputSq(1:(2.^w), 1:(2.^w), :); % get the squared power of 2 image
    end
else  % do the same but in the case when the height is bigger than the height
    h1 = nextpow2(h); 
    if 2.^h1 > w 
        imInputSq = imInputSq(1:(2.^(h1-1)), 1:(2.^(h1-1)), :);
    else 
        imInputSq = imInputSq(1:(2.^h1), 1:(2.^h1), :); 
    end 
end 

figure;
imshow(imInputSq); % show the squared image

end

