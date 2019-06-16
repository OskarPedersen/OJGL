#include "Matrix.h"
#include "utility/Log.h"
#include "utility/OJstd.h"
#include <string.h>

using namespace ojgl;

Matrix::Matrix(float data[16])
{
    memcpy(_data, data, sizeof(float) * 16);
}

Matrix Matrix::perspective(float fovy, float aspect, float zNear, float zFar)
{
    float tf = ojstd::tan(fovy / 2);
    float m1 = 1 / (aspect * tf);
    float m6 = 1 / tf;
    float m11 = -(zFar + zNear) / (zFar - zNear);
    float m12 = -1;
    float m15 = -2 * zFar * zNear / (zFar - zNear);
    float data[16] = { m1, 0, 0, 0, 0, m6, 0, 0, 0, 0, m11, m12, 0, 0, m15, 0 };
    return Matrix(data);
}

Matrix Matrix::rotation(float x, float y, float z, float angle)
{
    float n = ojstd::sqrt_asm(x * x + y * y + z * z);
    _ASSERTE(n != 0);
    x /= n;
    y /= n;
    z /= n;

    float c = ojstd::cos(angle);
    float s = ojstd::sin(angle);
    float m1 = x * x * (1 - c) + c;
    float m2 = y * x * (1 - c) + z * s;
    float m3 = z * x * (1 - c) - y * s;
    float m5 = x * y * (1 - c) - z * s;
    float m6 = y * y * (1 - c) + c;
    float m7 = z * y * (1 - c) * x * s;
    float m9 = x * z * (1 - c) + y * s;
    float m10 = y * z * (1 - c) - x * s;
    float m11 = z * z * (1 - c) + c;
    float data[16] = { m1, m2, m3, 0, m5, m6, m7, 0, m9, m10, m11, 0, 0, 0, 0, 1 };
    return Matrix(data);
}

Matrix Matrix::identity()
{
    float data[16] = { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };
    return Matrix(data);
}

Matrix Matrix::scaling(float scaling)
{
    float data[16] = { scaling, 0, 0, 0, 0, scaling, 0, 0, 0, 0, scaling, 0, 0, 0, 0, 1 };
    return Matrix(data);
}

Matrix Matrix::translation(float x, float y, float z)
{
    float data[16] = { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, x, y, z, 1 };
    return Matrix(data);
}

Matrix Matrix::operator*(const Matrix& other)
{
    float result[16];
    auto A = [this](int row, int column) -> const float& { return this->_data[column * 4 + row]; };
    auto B = [&other](int row, int column) -> const float& { return other.data()[column * 4 + row]; };
    auto C = [&result](int row, int column) -> float& { return result[column * 4 + row]; };

    for (int i = 0; i < 4; i++) {
        for (int j = 0; j < 4; j++) {
            float sum = 0.f;
            for (int k = 0; k < 4; k++) {
                sum += A(i, k) * B(k, j);
            }
            C(i, j) = sum;
        }
    }

    return Matrix(result);
}
