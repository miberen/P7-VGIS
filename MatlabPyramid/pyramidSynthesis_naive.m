function [] = pyramidSynthesis_naive(inputImage, levels)

for L = 1:levels % for every level
    outputImage = zeros(size(inputImage,1)*2, size(inputImage,1)*2, 3); % create an image doubling the size from the previous level
    if(length(inputImage)<4097) % check that it is not too big
        for i = 2:2:length(outputImage) % get the 2x2 pixel neighborhood
            for j = 2:2:length(outputImage) 
             vals = inputImage(i/2, j/2,:); % get the value of a pixel
             outputImage(i-1:i,j-1:j,1) = double(vals(1))*ones(2,2); % duplicate 4 times that value in the R channel
             outputImage(i-1:i,j-1:j,2) = double(vals(2))*ones(2,2); % duplicate 4 times that value in the G channel
             outputImage(i-1:i,j-1:j,3) = double(vals(3))*ones(2,2); % duplicate 4 times that value in the B channel
            end
        end
        figure;
        imshow(uint8(outputImage)); % show the output image
        inputImage = outputImage; % variable assignment to continue performing this "upsampling"
    end
end

end

