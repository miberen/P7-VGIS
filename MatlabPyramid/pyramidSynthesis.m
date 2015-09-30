function [] = pyramidSynthesis(inputImage, levels)

for L = 1:levels % for every level
    outputImage = zeros(size(inputImage,1)*2, size(inputImage,1)*2, 3); % create an image of half the size from the previous level
    val = zeros(3,4); 
    if(length(inputImage)<4097) % check that it is big enough for the neighborhood
        for i = 2:2:length(inputImage) % get the 2x2 pixel neighborhood (note that, although the neighborhood is 2x2, a 4x4 is involved to compute the mean) 
            for j = 2:2:length(inputImage) 
                
                inputImage = double(inputImage); % convert to double to allow values bigger than 255 (otherwise, the division by 16 cannot be performed)
                
                val(:,1) = (1/16)*(9*inputImage(i-1,j-1,:)+3*inputImage(i-1,j,:)+3*inputImage(i,j-1,:)+inputImage(i,j,:)); % get the weighted mean of the first part of the region
                val(:,2) = (1/16)*(3*inputImage(i-1,j-1,:)+9*inputImage(i-1,j,:)+inputImage(i,j-1,:)+3*inputImage(i,j,:)); % get the weighted mean of the second part of the region
                val(:,3) = (1/16)*(3*inputImage(i-1,j-1,:)+inputImage(i-1,j,:)+9*inputImage(i,j-1,:)+3*inputImage(i,j,:)); % get the weighted mean of the third part of the region
                val(:,4) = (1/16)*(inputImage(i-1,j-1,:)+3*inputImage(i-1,j,:)+3*inputImage(i,j-1,:)+9*inputImage(i,j,:)); % get the weighted mean of the fourth part of the region
                
                o = ones(2,2); % as it is not possible to duplicate vales in a X x X x 3 matrix, this auxiliar matrix is created
                
                % Assign output values (doing the correct duplication)
                outputImage((i*2)-3:(i*2)-2,(j*2)-3:(j*2)-2,1) = val(1,1)*o; 
                outputImage((i*2)-3:(i*2)-2,(j*2)-3:(j*2)-2,2) = val(2,1)*o; 
                outputImage((i*2)-3:(i*2)-2,(j*2)-3:(j*2)-2,3) = val(3,1)*o; 
                
                outputImage((i*2)-3:(i*2)-2,(j*2)-1:j*2,1) = val(1,2)*o; 
                outputImage((i*2)-3:(i*2)-2,(j*2)-1:j*2,2) = val(2,2)*o; 
                outputImage((i*2)-3:(i*2)-2,(j*2)-1:j*2,3) = val(3,2)*o; 
                
                outputImage((i*2)-1:i*2,(j*2)-3:(j*2)-2,1) = val(1,3)*o; 
                outputImage((i*2)-1:i*2,(j*2)-3:(j*2)-2,2) = val(2,3)*o; 
                outputImage((i*2)-1:i*2,(j*2)-3:(j*2)-2,3) = val(3,3)*o;
                
                outputImage((i*2)-1:i*2,(j*2)-1:j*2,1) = val(1,4)*o; 
                outputImage((i*2)-1:i*2,(j*2)-1:j*2,2) = val(2,4)*o; 
                outputImage((i*2)-1:i*2,(j*2)-1:j*2,3) = val(3,4)*o;
                
            end
        end
        
        figure;
        imshow(uint8(outputImage)); % show the output image
        inputImage = outputImage; % variable assignment to continue performing this "downsampling"
    end
end

end

