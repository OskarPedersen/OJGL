#pragma once

template <typename T>
struct Vector3 {
    T x = T(0);
    T y = T(0);
    T z = T(0);

    Vector3(const T& x, const T& y, const T& z)
        : x(x)
        , y(y)
        , z(z)
    {
    }

    Vector3 Vector3::operator*(T a)
    {
        return Vector3(x * a, y * a, z * a);
    }

    void Vector3::operator+=(Vector3 v)
    {
        x += v.x;
        y += v.y;
        z += v.z;
    }

    Vector3 Vector3::operator-(Vector3 v)
    {
        return Vec3(x - v.x, y - v.y, z - v.z);
    }

    void Vector3::normalize()
    {
        T len = static_cast<T>(sqrt_asm(static_cast<double>(x * x + y * y + z * z)));
        x /= len;
        y /= len;
        z /= len;
    }

    [[nodiscard]] T Vector3::lenSq()
    {
        return x * x + y * y + z * z;
    }
};

template <typename T>
struct Vector2 {
    Vector2() = default;

    T x = T(0);
    T y = T(0);

    Vector2(const T& x, const T& y)
        : x(x)
        , y(y)
    {
    }
};

using Vector2i = Vector2<int>;
using Vector2d = Vector2<double>;
using Vector3f = Vector3<float>;
