float Random01(float seed)
{
    const float a = 2;
    const float b = 3;
    const float c = 2;
    return frac(sin(seed * a) * b + c);
}

float3 RotateAroundAxis(float3 v, float3 axis, float angle)
{
    axis = normalize(axis);
    float cosAngle = cos(angle);
    float sinAngle = sin(angle);

    return v * cosAngle + cross(axis, v) * sinAngle + axis * dot(axis, v) * (1 - cosAngle);
}