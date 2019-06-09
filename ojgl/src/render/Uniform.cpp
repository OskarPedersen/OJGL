#include "Uniform.hpp"
#include "winapi/gl_loader.h"

namespace ojgl {

void Uniform1f::setUniform(int programID)
{
    glUniform1f(glGetUniformLocation(programID, _location.c_str()), this->_x);
}

void Uniform2f::setUniform(int programID)
{
    glUniform2f(glGetUniformLocation(programID, _location.c_str()), this->_x, this->_y);
}

void Uniform1fv::setUniform(int programID)
{
    glUniform1fv(glGetUniformLocation(programID, this->_location.c_str()), _values.size(), &_values[0]);
}

void Uniform3fv::setUniform(int programID)
{
    glUniform3fv(glGetUniformLocation(programID, this->_location.c_str()), _values.size(), &_values[0]);
}

void UniformMatrix4fv::setUniform(int programID)
{
    glUniformMatrix4fv(glGetUniformLocation(programID, this->_location.c_str()), 1, false, _matrix.data());
}
}
