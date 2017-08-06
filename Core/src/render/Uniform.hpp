#pragma once
#include "GLState.h"
#include "winapi/gl_loader.h"
#include <iostream>
#include <string>
#include <vector>

namespace ojgl {

class UniformBase {
public:
    UniformBase(std::string location)
        : _location(location)
    {
    }
    virtual ~UniformBase() = default;

    virtual void setUniform(int programID) = 0;
    std::string location() const { return _location; }

protected:
    const std::string _location;
};

class Uniform1f : public UniformBase {
public:
    Uniform1f(std::string location, float x)
        : UniformBase(location)
        , _x(x){};
    void setUniform(int programID) override
    {
        glUniform1f(glGetUniformLocation(programID, _location.c_str()), this->_x);
    }

private:
    float _x;
};

class Uniform1fv : public UniformBase {
public:
    Uniform1fv(std::string location, std::vector<float> values)
        : UniformBase(location)
        , _values(values){};
    void setUniform(int programID)
    {
        glUniform1fv(glGetUniformLocation(programID, this->_location.c_str()), _values.size(), &_values[0]);
    }

private:
    std::vector<float> _values;
};
}