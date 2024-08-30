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


When using Multiple Importance Sampling with the single balance heuristic, the desired result for the MC section is
$$
\sum_{D\,\in\,distributions}
   \frac{1}{|S(D)|}
       \sum_{I\,\in\,S(D)}
           \frac{balance(D, I)\,f(I)}{pdf(D, I)}
$$

where balance(D, I) = 
$$
\frac{|S(D)|\,pdf(D, I)}{\sum_{J\,\in\,distributions} |S(J)|\,pdf(J, I)}
$$
Given that we have only two distributions, NEE and BRDF, then this should expand to

$$
\frac{1}{|S(NEE)|}
    \sum_{I\,\in\,S(NEE)}
        \frac{balance(NEE, I)\,f(I)}{pdf(NEE, I)}
+
\frac{1}{|S(BRDF)|}
    \sum_{I\,\in\,S(BRDF)}
        \frac{balance(BRDF, I)\,f(I)}{pdf(BRDF, I)}
$$

where balance(D, I) =

$$
\frac{|S(D)|\,pdf(D, I)}{|S(NEE)|\,pdf(NEE, I) + |S(BRDF)|\,pdf(BRDF, I)}
$$

which should expand to to
$$
\frac{1}{|S(NEE)|}
    \sum_{I\,\in\,S(NEE)}
        \frac{|S(NEE)|\,pdf(NEE, I)\,f(I)}{pdf(NEE, I)\,(|S(NEE)|\,pdf(NEE, I) + |S(BRDF)|\,pdf(BRDF, I))}
+
\frac{1}{S(BRDF)}
    \sum_{I\,\in\,S(BRDF)}
        \frac{|S(BRDF)|\,pdf(BRDF, I)\,f(I)}{pdf(BRDF, I)\,(|S(NEE)|\,pdf(NEE, I) + |S(BRDF)|\,pdf(BRDF, I))}
$$
which should simplify to
$$
\frac{1}{|S(NEE)|}
    \sum_{I\,\in\,S(NEE)}
        \frac{|S(NEE)|\,f(I)}{|S(NEE)|\,PDF(NEE, I) + |S(BRDF)|\,PDF(BRDF, I)}
+
\frac{1}{S(BRDF)}
    \sum_{I\,\in\,S(BRDF)}
        \frac{|S(BRDF)|\,f(I)}{|S(BRDF)|\,PDF(NEE, I) + |S(BRDF)|\,PDF(BRDF, I)}
$$  
which should simplify to
$$

    \sum_{I\,\in\,S(NEE)}
        \frac{f(I)}{|S(NEE)|\,PDF(NEE, I) + |S(BRDF)|\,PDF(BRDF, I)}
+

    \sum_{I\,\in\,S(BRDF)}
        \frac{f(I)}{|S(BRDF)|\,PDF(NEE, I) + |S(BRDF)|\,PDF(BRDF, I)}
$$  
As all samples now have the same terms, we can simplify with $S = S(BRDF)\,\cup\,S(NEE)$
$$
\sum_{I\,\in\,S}
    \frac{f(I)}{|S(NEE)|\,PDF(NEE, I) + |S(BRDF)|\,PDF(BRDF, I)}
$$
given that $f(I) = incident(P, I)\,\frac{LR}{\pi}\,dot(N, I)$

then this should expand to
$$
\sum_{I\,\in\,S}
     \frac{incident(P, I)\,LR\,dot(N, I)}{\pi\,(|S(NEE)|\,PDF(NEE, I) + |S(BRDF)|\,PDF(BRDF, I))}
$$


When using Multiple Importance Sampling, the desired result with power heuristic for the MC section is
$$
\sum_{D\,\in\,distributions}
   \frac{1}{|S(D)|}
       \sum_{I\,\in\,S(D)}
           \frac{balance(D, I)\,f(I)}{pdf(D, I)}\\

balance(D, I) =  \frac{|S(D)|^2\,pdf(D, I)^2}{\sum_{J\,\in\,distributions} |S(J)|^2\,pdf(J, I)^2}
$$
Given that we have only two distributions, NEE and BRDF, then this should expand to

$$
\frac{1}{|S(NEE)|}
    \sum_{I\,\in\,S(NEE)}
        \frac{balance(NEE, I)\,f(I)}{pdf(NEE, I)}
+
\frac{1}{|S(BRDF)|}
    \sum_{I\,\in\,S(BRDF)}
        \frac{balance(BRDF, I)\,f(I)}{pdf(BRDF, I)}\\

balance(D, I) = \frac{|S(D)|^2\,pdf(D, I)^2}{weight(I)}\\
weight(I) = |S(NEE)|^2\,pdf(NEE, I)^2 + |S(BRDF)|^2\,pdf(BRDF, I)^2
$$

which should expand to to
$$
\frac{1}{|S(NEE)|}
    \sum_{I\,\in\,S(NEE)}
        \frac{|S(NEE)|^2\,pdf(NEE, I)^2\,f(I)}{pdf(NEE, I)\,weight(I)}
+
\frac{1}{S(BRDF)}
    \sum_{I\,\in\,S(BRDF)}
        \frac{|S(BRDF)|^2\,pdf(BRDF, I)^2\,f(I)}{pdf(BRDF, I)\,weight(I)}
$$
which should simplify to
$$
\frac{1}{|S(NEE)|}
    \sum_{I\,\in\,S(NEE)}
        \frac{|S(NEE)|^2\,pdf(NEE, I)\,f(I)}{weight(I)}
+
\frac{1}{S(BRDF)}
    \sum_{I\,\in\,S(BRDF)}
        \frac{|S(BRDF)|^2\,pdf(BRDF, I)\,f(I)}{weight(I)}
$$  
which should simplify to
$$

    \sum_{I\,\in\,S(NEE)}
        \frac{|S(NEE)|\,pdf(NEE, I)\,f(I)}{weight(I)}
+

    \sum_{I\,\in\,S(BRDF)}
        \frac{|S(BRDF)|\,pdf(BRDF, I)\,f(I)}{weight(I)}
$$  
We can now simplify to
$$
\sum_{D\,\in\,distributions}
    \sum_{I\,\in\,S(D)}
        \frac{|S(D)|\,pdf(D, I)\,f(I)}{weight(I)}
$$
given that $f(I) = incident(P, I)\,\frac{LR}{\pi}\,dot(N, I)$

then this should expand to
$$
\sum_{D\,\in\,distributions}
    \sum_{I\,\in\,S(D)}
        \frac{|S(D)|\,pdf(D, I)\,incident(P, I)\,LR\,dot(N, I)}{\pi\,weight(I)}
$$