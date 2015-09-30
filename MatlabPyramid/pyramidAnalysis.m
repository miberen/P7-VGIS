function [outputImage] = pyramidAnalysis(inputImage, levels)

for L = 1:levels % for every level
    outputImage = zeros(size(inputImage,1)/2, size(inputImage,1)/2, 3); % create an image of half the size from the previous level
    if(length(inputImage)>2) % check that it is big enough for the neighborhood
        for i = 2:2:length(inputImage) % get the 2x2 pixel neighborhood
            for j = 2:2:length(inputImage) 
             outputImage(i/2,j/2,:) = mean(mean(inputImage(i-1:i, j-1:j,:))); % compute the mean of that neighborhood
            end
        end
        figure;
        imshow(uint8(outputImage)); % show the output image
        inputImage = outputImage; % variable assignment to continue performing this "downsampling"
    end
end

end

