   * S: Number of samples for MC
   * P: Location of the sampled point
   * N: Normal of the surface the point is on
   * O: The output direction (the inverse of the ray direction)
   * I: The sampled input direction for reflectance
   * LR: Lambertian reflectance (0-1)

The light hitting the camera is the light exiting the surfaces pointing towards the camera.
The light exiting the surface is
    emitted light +
    integral over hemisphere (I) dI
        BSDF(P, N, O, I) * incident(P, I) * dot(N, I)

where incident is the light exiting the surface found in direction I

We use monte carlo for the integral part. So the exited light is
    emitted light +
    1/S * sum
        sample direction I over hemisphere
            BSDF(P, N, O, I) * incident(P, I) * dot(N, I)

If sampling is not uniform, the PDF must cancel with some part of f(x), and divide by that.

As we are not actually handling transmitted light right now for any translucent surfaces, BSDF(P, N, O, I) = BRDF(P, N, O, I).

For a Lambertian material BRDF(P, N, O, I) = LR / pi.

Therefore for a Lambertian material, the exited light is
    emitted light +
    1/S sum
        sample direction I over hemisphere
            LR/pi * incident(P, I) * dot(N, I)

If I is cosine weighted, then the PDF is dot(N, I) / pi, which are both cancelled.

So dividing by PDF gives
    emitted light +
    1/S sum
        sample direction I over cosine weighted hemisphere
            LR * incident(P, I)

Next Event Estimation (NEE) refers to sampling lights directly. This is effectively assuming that direct lighting matters most.
BRDF sampling refers to sampling directions where the material reflects most light. e.g. for a Lambertian material, cosine weighting.

When using Multiple Importance Sampling, the desired result is

sum for each distribution D
   1 / S(D) *
       sum for each sample I
           balance(D, I) * (f(I) / pdf(D, I))

where balance(D, I) = S(D) * pdf(I) / 
    sum for each distribution J
        S(J) * PDF(J, I)
