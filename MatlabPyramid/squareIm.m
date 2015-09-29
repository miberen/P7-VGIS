function [imInputSq] = squareIm(namefile)

imInputSq = imread(namefile); 

figure; 
imshow(namefile); 

[w,h,~] = size(imInputSq)

if w<h 
    w1 = nextpow2(w); 
    if 2.^w1 > w 
        imInputSq = imInputSq(1:(2.^(w1-1)), 1:(2.^(w1-1)), :);
    else 
        imInputSq = imInputSq(1:(2.^w), 1:(2.^w), :); 
    end
else 
    h1 = nextpow2(h); 
    if 2.^h1 > w 
        imInputSq = imInputSq(1:(2.^(h1-1)), 1:(2.^(h1-1)), :);
    else 
        imInputSq = imInputSq(1:(2.^h1), 1:(2.^h1), :); 
    end 
end 

figure;
imshow(imInputSq); 

end

