function [ out_image ] = pyramidS( in_image, levels, interpolation )
% PYRAMIDS performs the syntesis process in the generation of a Pyramid.
%
% Parameters:
%   in_image      - Input image (dimensions NxM)
%   levels        - Number of levels of the pyramid (0: input == output)
%   interpolation - Method uses for interpolation
%                   'nearest'  - Nearest-neighbor
%                   'bilinear' - Bilinear
%                   'bicubic'  - Bicubic
%
% Output:
%   out_image - Output image (dimensions [N*(2^levels)]x[M*(2^levels)])

% output matrix
out_image = in_image;
if levels == 0
    return
end

for i=1:levels      % Repeat for the number of levels
    
    out_image = imresize(out_image,2,interpolation);
    
    % Show the pyramid
    figure;
    imshow(out_image);

end

end

