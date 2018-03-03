#pragma once
#include "winapi/gl_loader.h"
#include <iostream>
#include <string>
#include <vector>

namespace ojgl {

class UniformBase {
public:
    UniformBase(const std::string& location)
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
    Uniform1f(const std::string& location, float x)
        : UniformBase(location)
        , _x(x){};
    void setUniform(int programID) override
    {
        glUniform1f(glGetUniformLocation(programID, _location.c_str()), this->_x);
    }

private:
    const float _x;
};

class Uniform1fv : public UniformBase {
public:
    Uniform1fv(const std::string& location, const std::vector<float>& values)
        : UniformBase(location)
        , _values(values){};
    void setUniform(int programID) override
    {
        glUniform1fv(glGetUniformLocation(programID, this->_location.c_str()), _values.size(), &_values[0]);
    }

private:
    const std::vector<float> _values;
};

class Uniform1t {
public:
    Uniform1t(const std::string& location, const std::shared_ptr<Texture>& texture)
        : _location(location)
        , _texture(texture){};
    std::string location() const { return _location; }
    int textureID() const { return _texture->textureID(); }

private:
    const std::string _location;
    const std::shared_ptr<Texture> _texture;
};
} //namespace ojgl
