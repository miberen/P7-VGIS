function [] = pyramid(inputImage, levels)

for L = 1:levels 
    outputImage = zeros(size(inputImage,1)/2, size(inputImage,1)/2, 3); 
    if(length(inputImage)>2) 
        for i = 1:3
             outputImage(j,j,:) = mean(mean(inputImage(i:i+1, i:i+1,:)));
             j = j+1; 
        end
        figure;
        imshow(outputImage); 
    end
end

end

