https://research.nvidia.com/sites/default/files/pubs/2017-07_Spatiotemporal-Variance-Guided-Filtering%3A//svgf_preprint.pdf

First, use path tracer to obtain data. This should be:
    * Diffuse colour (texture/vertex colour)
    * Illumination (incoming light)
    * Depth (RayTCurrent())
    * Normal (triangle normal)
    
Second, for Illumination, apply lerp(output[id], input[id], 0.2) from previous frame, if there is one.

Third, calculate the moments https://en.wikipedia.org/wiki/Color_moments (per component) with a 7x7 kernel on Illumination 
    * The first moment is mean
    * The second moment is standard deviation
    * Apply the same lerp again and save the new moments as history for next frame
    * Integrate the moments ???
    * Variance = sqrt(deviation - mean * mean)

Fourth, a-trous:
    * Perform this on variance and illumination simultaneously due to their shared terms
    * After one iteration, store the colour as history for the next frame
    * Gaussian 3x3 kernel = 1/16 * [[1, 2, 1], [2, 4, 2], [1, 2, 1]] https://en.wikipedia.org/wiki/Kernel_(image_processing)
    * Do five iterations

Finally:
    * Multiply in diffuse colour to get the final colour and present that.

https://github.com/jacquespillet/SVGF/blob/Part_13/src/Filter.cuh
https://github.com/alipbcs/ZetaRay