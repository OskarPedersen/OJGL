#include "Buffer.h"
#include "GLState.h"
#include "utility\Log.h"
#include "utility\ShaderReader.h"
#include <iostream>
#include <memory>
#include <string>

namespace ojgl {

Buffer::~Buffer()
{
    if (_fboID != 0) {
        glDeleteFramebuffers(1, &_fboID);
    }
    if (_fboTextureID != 0) {
        glDeleteTextures(1, &_fboTextureID);
    }

    glDeleteProgram(_programID);
}

std::string Buffer::name() const
{
    return _name;
}

unsigned Buffer::fboTextureID()
{
    return _fboTextureID;
}

void Buffer::render()
{
    if (ShaderReader::modified(_vertexPath) || ShaderReader::modified(_fragmentPath))
        loadShader();

    glBindFramebuffer(GL_FRAMEBUFFER, _fboID);
    glViewport(0, 0, _width, _height);
    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

    glUseProgram(_programID);
    for (size_t i = 0; i < _inputs.size(); i++) {
        std::string uniform = "inTexture" + std::to_string(i);
        glUniform1i(glGetUniformLocation(_programID, uniform.c_str()), i);
    }

    size_t index = 0;
    for (auto[location, texture] : _textures) {
        glUniform1i(glGetUniformLocation(_programID, location.c_str()), _inputs.size() + index);
        index++;
    }

    for (size_t i = 0; i < _inputs.size(); i++) {
        glActiveTexture(GL_TEXTURE0 + i);
        glBindTexture(GL_TEXTURE_2D, _inputs[i]->fboTextureID());
    }

    index = 0;
    for (auto[location, texture] : _textures) {
        glActiveTexture(GL_TEXTURE0 + _inputs.size() + index);
        glBindTexture(GL_TEXTURE_2D, texture->textureID());
        index++;
    }

    for (auto& um : _uniforms) {
        um.second->setUniform(_programID);
    }

    glDrawArrays(GL_TRIANGLES, 0, Buffer::vertexCount);

    glUseProgram(0);
    glBindFramebuffer(GL_FRAMEBUFFER, 0);
}

void Buffer::generateFBO()
{
    glGenFramebuffers(1, &_fboID);
    glBindFramebuffer(GL_FRAMEBUFFER, _fboID);

    glGenTextures(1, &_fboTextureID);
    glBindTexture(GL_TEXTURE_2D, _fboTextureID);
    glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, _width, _height, 0, GL_RGBA, GL_UNSIGNED_BYTE, nullptr);

    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
    //glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
    //glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
    glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
    glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);

    glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, _fboTextureID, 0);

    if (glCheckFramebufferStatus(GL_FRAMEBUFFER) != GL_FRAMEBUFFER_COMPLETE) {
        LOG_ERROR("Framebuffer error");
    }

    glBindTexture(GL_TEXTURE_2D, 0);
    glBindFramebuffer(GL_FRAMEBUFFER, 0);
}

void Buffer::loadShader()
{
    if (_programID != 0)
        glDeleteProgram(_programID);

    _programID = glCreateProgram();
    int vertID = glCreateShader(GL_VERTEX_SHADER);
    int fragID = glCreateShader(GL_FRAGMENT_SHADER);

    int vertexShaderLength = ShaderReader::get(_vertexPath).length();
    auto vertexChar = ShaderReader::get(_vertexPath).c_str();
    glShaderSource(vertID, 1, &vertexChar, &vertexShaderLength);

    int fragmentShaderLength = ShaderReader::get(_fragmentPath).length();
    auto fragmentChar = ShaderReader::get(_fragmentPath).c_str();
    glShaderSource(fragID, 1, &fragmentChar, &fragmentShaderLength);

    glCompileShader(vertID);
    GLint param;
    glGetShaderiv(vertID, GL_COMPILE_STATUS, &param);
    if (param == GL_FALSE) {
        LOG_ERROR("Failed to compile vertex shader!");
        int len;
        char log[200];
        glGetShaderInfoLog(fragID, 200, &len, log);
        LOG_ERROR(log);
    }

    glCompileShader(fragID);

    glGetShaderiv(fragID, GL_COMPILE_STATUS, &param);
    if (param == GL_FALSE) {
        LOG_ERROR("Failed to compile fragment shader!");
        int len;
        char log[200];
        glGetShaderInfoLog(fragID, 200, &len, log);
        LOG_ERROR(log);
    }

    glAttachShader(_programID, vertID);
    glAttachShader(_programID, fragID);
    glLinkProgram(_programID);

    glValidateProgram(_programID);
    glGetProgramiv(_programID, GL_VALIDATE_STATUS, &param);
    if (param == GL_FALSE) {
        LOG_ERROR("Shader program is not valid!");
        int len;
        char log[200];
        glGetShaderInfoLog(fragID, 200, &len, log);
        LOG_ERROR(log);
    }

    //Delete the shaders
    glDetachShader(_programID, vertID);
    glDetachShader(_programID, fragID);
    glDeleteShader(vertID);
    glDeleteShader(fragID);
}

GLuint Buffer::getProgramID() const
{
    return this->_programID;
}

} //namespace ojgl
