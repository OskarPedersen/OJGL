#include "GLState.h"
#include "music/Music.h"
#include <iostream>
#include <memory>
#include <string>
#include <vector>

namespace ojgl {
GLState::GLState(const std::string& vertexShader, const std::string& fragmentShader)
{
    _startTime = GetTickCount();
    load_gl_functions();
    setupQuad();
    loadShader(vertexShader, fragmentShader);
}

GLState::~GLState()
{
    glDeleteVertexArrays(1, &_vaoID);
    glDeleteBuffers(1, &_vboID);
    glDeleteProgram(_programID);
}

void GLState::render(Music& music) const
{
    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
    glUseProgram(_programID);
    glUniform1f(glGetUniformLocation(_programID, "iGlobalTime"), (GLfloat)((GetTickCount() - _startTime) / 1000.0));
    for (auto& kv : music.syncChannels) {
        auto sc = kv.second;
        std::vector<GLfloat> values;
        for (int i = 0; i < sc.numNotes; i++)
            values.push_back((GLfloat)(sc.getNoteVelocity(i) / 128.0));
        std::string uniform = "CHANNEL_" + std::to_string(sc.channel) + "_VELOCITY";
        glUniform1fv(glGetUniformLocation(_programID, uniform.c_str()), sc.numNotes, &values[0]);
    }
    glUniform1f(glGetUniformLocation(_programID, "CHANNEL_12_TOTAL"), (GLfloat)music.syncChannels[12].getTotalHitsPerNote(0));
    glUniform1f(glGetUniformLocation(_programID, "CHANNEL_13_TOTAL"), (GLfloat)music.syncChannels[13].getTotalHitsPerNote(0));
    glBindVertexArray(_vaoID);
    glDrawArrays(GL_TRIANGLES, 0, _vertexCount);
    glBindVertexArray(0);
    glFlush();
}

void GLState::setupQuad()
{
    float vertices[] = {
        -1, 1, 0, -1, -1, 0, 1, -1, 0,
        1, -1, 0, 1, 1, 0, -1, 1, 0
    };

    glGenVertexArrays(1, &_vaoID);
    glBindVertexArray(_vaoID);
    glEnableVertexAttribArray(0);
    glGenBuffers(1, &_vboID);
    glBindBuffer(GL_ARRAY_BUFFER, _vboID);
    glBufferData(GL_ARRAY_BUFFER, sizeof(vertices), &vertices, GL_STATIC_DRAW);
    glVertexAttribPointer(0, 3, GL_FLOAT, false, 0, 0);
    glBindBuffer(GL_ARRAY_BUFFER, 0);
    glBindVertexArray(0);
}

void GLState::loadShader(const std::string& vertexShader, const std::string& fragmentShader)
{

    _programID = glCreateProgram();
    int vertID = glCreateShader(GL_VERTEX_SHADER);
    int fragID = glCreateShader(GL_FRAGMENT_SHADER);

    int vertexShaderLength = vertexShader.length();
    auto vertexChar = vertexShader.c_str();
    glShaderSource(vertID, 1, &vertexChar, &vertexShaderLength);

    int fragmentShaderLength = fragmentShader.length();
    auto fragmentChar = fragmentShader.c_str();
    glShaderSource(fragID, 1, &fragmentChar, &fragmentShaderLength);

    glCompileShader(vertID);
    GLint param;
    glGetShaderiv(vertID, GL_COMPILE_STATUS, &param);
    if (param == GL_FALSE) {
        std::cout << "Failed to compile vertex shader!\n";
        int len;
        char log[200];
        glGetShaderInfoLog(fragID, 200, &len, log);
        std::cout << log;
    }

    glCompileShader(fragID);

    glGetShaderiv(fragID, GL_COMPILE_STATUS, &param);
    if (param == GL_FALSE) {
        std::cout << "Failed to compile fragment shader!\n";
        int len;
        char log[200];
        glGetShaderInfoLog(fragID, 200, &len, log);
        std::cout << log;
    }

    glAttachShader(_programID, vertID);
    glAttachShader(_programID, fragID);
    glLinkProgram(_programID);

    glValidateProgram(_programID);
    glGetProgramiv(_programID, GL_VALIDATE_STATUS, &param);
    if (param == GL_FALSE) {
        std::cout << "Shader program is not valid!\n";
        int len;
        char log[200];
        glGetShaderInfoLog(fragID, 200, &len, log);
        std::cout << log;
    }

    //Delete the shaders
    glDetachShader(_programID, vertID);
    glDetachShader(_programID, fragID);
    glDeleteShader(vertID);
    glDeleteShader(fragID);
}

GLuint GLState::getProgramID() const
{
    return this->_programID;
}

GLuint GLState::getVAO() const
{
    return this->_vaoID;
}

GLuint GLState::getVBO() const
{
    return this->_vboID;
}
}