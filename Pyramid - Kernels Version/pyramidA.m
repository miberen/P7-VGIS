function [ out_image ] = pyramidA( in_image, levels, kernel )
% PYRAMIDA performs the analysis process in the generation of a Pyramid.
%
% Parameters:
%   in_image - Input image (dimensions NxM - We assume that N and M
%              are power of 2)
%   levels   - Number of levels of the pyramid (0: input == output)
%   kernel   - Kernel used for filtering the image
%
% Output:
%   out_image - Output image (dimensions [N/(2^levels)]x[M/(2^levels)])
%
% The steps of the analysis process are:
%   1. Apply a filter to the image (to reduce the aliasing);
%   2. Downsample the image obtained;
%   3. Repeat the steps 1 and 2 for 'levels' times.


% Read the dimensions of the input image
in_image_dim = size(in_image);
N = in_image_dim(1)    % # of rows
M = in_image_dim(2)    % # of cols
% In case of an RGB image, then there are 3 dimensions, 1 otherwise
Z = length(size(in_image))
if Z < 3
    Z = 1
end

% Convert the elements from int in the range [0, 255] to double in the
% range [0, 1], to avoid the loss of information in the filtering phase
in_image = double(in_image)/255;

% output matrix
out_image = in_image;
if levels == 0
    return
end

for i=1:levels      % Repeat for the number of levels
    
    % Matrix for temporary results
    temp_image = zeros(N,M,Z);
    
    for j=1:Z       % Go through the channels of the image (1 or 3)
        % Convolve the channel j with the filter
        temp_image(:,:,j) = imfilter(out_image(:,:,j), kernel);
    end
    
    % New dimensions
    N = N/2;
    M = M/2;
    
    % Set the dimensions for the output image
    out_image = zeros(N,M,Z);
    
    for k=1:N
        for l=1:M
            for j=1:Z
                % Perform the downsampling
                out_image(k,l,j) = temp_image(2*k,2*l,j);
            end
        end
    end
    
    % Show the pyramid
     figure;
     imshow(out_image);
    
end

end

