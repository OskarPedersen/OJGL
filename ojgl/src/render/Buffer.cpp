#include "Buffer.h"
#include "utility/Log.h"
#include "utility/ShaderReader.h"
#include "winapi/gl_loader.h"

using namespace ojgl;

Buffer::Buffer(unsigned width, unsigned height, const ojstd::string& name, const ojstd::string& vertexPath, const ojstd::string& fragmentPath, const ojstd::vector<BufferPtr>& inputs, BufferFormat format, bool renderOnce)
    : _format(format)
    , _inputs(inputs)
    , _name(name)
    , _width(width)
    , _height(height)
    , _vertexPath(vertexPath)
    , _fragmentPath(fragmentPath)
    , _renderOnce(renderOnce)

{
    loadShader();
    if (_format == BufferFormat::Quad)
        _meshes.push_back({ Mesh::constructQuad(), Matrix::identity() });

    for (int i = 0; i < _inputs.size(); i++) {
        _numInputs += _inputs[i]->numOutTextures();
    }
}

Buffer::~Buffer()
{
    if (_fboID != 0) {
        glDeleteFramebuffers(1, &_fboID);
    }

    for (int i = 0; i < _fboTextureIDs.size(); i++) {
        if (_fboTextureIDs[i] != 0) {
            glDeleteTextures(1, &_fboTextureIDs[i]);
        }
    }

    glDeleteProgram(_programID);
}

ojstd::string Buffer::name() const
{
    return _name;
}

void Buffer::render()
{

    if (ShaderReader::modified(_vertexPath) || ShaderReader::modified(_fragmentPath)) {
        loadShader();
        _hasRendered = false;
    }

    if (_hasRendered && _renderOnce)
        return;

    glBindFramebuffer(GL_FRAMEBUFFER, _fboID);
    glViewport(0, 0, _width, _height);
    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

    glUseProgram(_programID);
    for (unsigned i = 0; i < _numInputs; i++) {
        ojstd::string uniform("inTexture");
        uniform.append(ojstd::to_string(i));
        glUniform1i(glGetUniformLocation(_programID, uniform.c_str()), i);
    }

    int index = 0;
    for (auto[location, texture] : _textures) {
        glUniform1i(glGetUniformLocation(_programID, location.c_str()), _numInputs + index);
        index++;
    }

    for (int i = 0, texture = 0; i < _inputs.size(); i++) {
        for (int j = 0; j < _inputs[i]->_fboTextureIDs.size(); j++, texture++) {
            glActiveTexture(GL_TEXTURE0 + texture);
            glBindTexture(GL_TEXTURE_2D, _inputs[i]->_fboTextureIDs[j]);
        }
    }

    index = 0;
    for (auto[location, texture] : _textures) {
        glActiveTexture(GL_TEXTURE0 + _numInputs + index);
        glBindTexture(GL_TEXTURE_2D, texture->textureID());
        index++;
    }

    for (auto& um : _uniforms) {
        um.second->setUniform(_programID);
    }

    for (const auto& mesh : _meshes) {
        glBindVertexArray(mesh.first->vaoID());
        glUniformMatrix4fv(glGetUniformLocation(_programID, "M"), 1, false, mesh.second.data());
        glDrawArrays(GL_TRIANGLES, 0, mesh.first->verticesCount());
    }

    glBindVertexArray(0);

    glUseProgram(0);
    glBindFramebuffer(GL_FRAMEBUFFER, 0);

    _hasRendered = true;
}

void Buffer::generateFBO()
{
    glGenFramebuffers(1, &_fboID);
    glBindFramebuffer(GL_FRAMEBUFFER, _fboID);

    for (int i = 0; i < numOutTextures(); i++) {
        _fboTextureIDs.emplace_back(0);
        glGenTextures(1, &_fboTextureIDs[i]);
        glBindTexture(GL_TEXTURE_2D, _fboTextureIDs[i]);
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA32F, _width, _height, 0, GL_RGBA, GL_UNSIGNED_BYTE, nullptr);

        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
        glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
        glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);

        glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0 + i, GL_TEXTURE_2D, _fboTextureIDs[i], 0);

        if (glCheckFramebufferStatus(GL_FRAMEBUFFER) != GL_FRAMEBUFFER_COMPLETE) {
            LOG_ERROR("Framebuffer error");
        }
    }

    ojstd::vector<GLenum> drawBuffers;
    for (int i = 0; i < numOutTextures(); i++) {
        drawBuffers.emplace_back(GL_COLOR_ATTACHMENT0 + i);
    }
    glDrawBuffers(numOutTextures(), &drawBuffers[0]);

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

int inline Buffer::numOutTextures()
{
    // Still quite hard-coded, improve when neccessary
    return _renderOnce ? 8 : 1;
}

Buffer& Buffer::operator<<(const Uniform1t& b)
{
    _textures[b.location()] = ojstd::make_shared<Uniform1t>(b);
    return *this;
}

void Buffer::insertMesh(const ojstd::shared_ptr<Mesh>& mesh, const Matrix& modelMatrix)
{
    _ASSERTE(_format == BufferFormat::Meshes);
    _meshes.push_back({ mesh, modelMatrix });
}

void Buffer::clearMeshes()
{
    if (_format == BufferFormat::Meshes)
        _meshes.clear();
}

ojstd::shared_ptr<Buffer> Buffer::construct(unsigned width, unsigned height, const ojstd::string& name, const ojstd::string& vertexPath, const ojstd::string& fragmentPath, const ojstd::vector<BufferPtr>& inputs, BufferFormat format, bool renderOnce)
{
    return ojstd::shared_ptr<Buffer>(new Buffer(width, height, name, vertexPath, fragmentPath, inputs, format, renderOnce));
}
